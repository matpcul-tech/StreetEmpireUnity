import numpy as np
from PIL import Image, ImageDraw
from scipy import ndimage
S = 1024
def load(n):
    d = np.load(f"uv_{n}.npz", allow_pickle=True); return d["tris"], d["bone"], [str(x) for x in d["names"]], d["pos"]
def raster(tris, sel=None, size=S, outline=True):
    im = Image.new("L", (size, size), 0); dr = ImageDraw.Draw(im)
    for i, t in enumerate(tris):
        if sel is not None and not sel[i]: continue
        p = [(float(u) * size, (1 - float(v)) * size) for u, v in t]
        dr.polygon(p, fill=255, outline=255 if outline else None)
    return np.asarray(im) > 0
def dilate_fill(img, cover, px=12):
    # texels outside the islands take the color of the nearest texel inside one (edge padding)
    inner = ndimage.binary_erosion(cover, iterations=1)   # the outermost ring of an island often already holds the dark gutter
    dist, idx = ndimage.distance_transform_edt(~inner, return_indices=True)
    out = img.copy(); m = (~inner) & (dist <= px)
    out[m] = img[idx[0][m], idx[1][m]]
    return out
def rgb2hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]; mx = a.max(-1); mn = a.min(-1); d = mx - mn
    h = np.zeros_like(mx); m = d > 1e-6
    rc = (mx == r) & m; gc = (mx == g) & m & ~rc; bc = m & ~rc & ~gc
    h[rc] = ((g - b)[rc] / d[rc]) % 6; h[gc] = (b - r)[gc] / d[gc] + 2; h[bc] = (r - g)[bc] / d[bc] + 4
    return h * 60, np.where(mx > 0, d / np.maximum(mx, 1e-6), 0), mx
def hsv2rgb(h, s, v):
    h = (h % 360) / 60; i = np.floor(h).astype(int) % 6; f = h - np.floor(h)
    p = v * (1 - s); q = v * (1 - f * s); t = v * (1 - (1 - f) * s)
    out = np.zeros(h.shape + (3,), dtype=np.float32)
    for k, (r, g, b) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k; out[m, 0] = r[m]; out[m, 1] = g[m]; out[m, 2] = b[m]
    return out

SKIN_BONES_HEAD = ("Head", "Neck")
def bone_sel(bone, names, keys):
    ok = np.array([any(k in nm for k in keys) for nm in names]); return ok[bone]
def seam_fix(img, tris, bone, names, ring=2, inner=4, thresh=0.90):
    """On head and neck islands: an edge texel clearly darker than the skin a few texels inside the island takes that inside color."""
    sel = bone_sel(bone, names, SKIN_BONES_HEAD)
    cover = raster(tris, sel)                      # head and neck islands
    allc = raster(tris)
    a = img.astype(np.float32) / 255; lum = a @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    core = ndimage.binary_erosion(cover, iterations=inner)
    dist, idx = ndimage.distance_transform_edt(~core, return_indices=True)
    edge = cover & ~ndimage.binary_erosion(cover, iterations=ring)
    src = a[idx[0], idx[1]]; slum = lum[idx[0], idx[1]]
    h, s, v = rgb2hsv(src)
    skin_src = ((h < 35) | (h > 345)) & (s > 0.08) & (s < 0.6) & (v > 0.45)      # the inside texel is skin, not hair or cloth
    bad = edge & skin_src & (dist <= inner + ring + 1) & (lum < thresh * slum)
    out = a.copy(); out[bad] = src[bad]
    return (out * 255 + 0.5).astype(np.uint8), bad

def retouch_box(img, box, pad=10):
    """Paint a mark out of plain skin: inside the box, texels that stray from the skin's own slow gradient go back to it."""
    y0, y1, x0, x1 = box; a0, a1, b0, b1 = max(0, y0 - pad), min(S, y1 + pad), max(0, x0 - pad), min(S, x1 + pad)
    a = img[a0:a1, b0:b1].astype(np.float32) / 255
    h, s, v = rgb2hsv(a); skin = ((h < 35) | (h > 345)) & (s > 0.05) & (s < 0.6) & (v > 0.5)
    # slow gradient of the skin: a wide median, taken over skin texels only
    fill = a.copy(); med = np.median(a[skin], axis=0); fill[~skin] = med
    base = np.stack([ndimage.median_filter(fill[..., c], size=21, mode="nearest") for c in range(3)], -1)
    lum = a @ np.array([0.299, 0.587, 0.114], dtype=np.float32); bl = base @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    dev = np.abs(a - base).max(-1)
    mark = skin & ((lum < bl - 0.012) | (dev > 0.03))
    inbox = np.zeros_like(mark); inbox[y0 - a0:y1 - a0, x0 - b0:x1 - b0] = True
    w = ndimage.gaussian_filter((ndimage.binary_dilation(mark & inbox, iterations=2) & skin).astype(np.float32), 1.2)[..., None]
    w = np.clip(w * 1.6, 0, 1)
    out = img.copy(); out[a0:a1, b0:b1] = ((a * (1 - w) + base * w) * 255 + 0.5).astype(np.uint8)
    return out, int((w[..., 0] > 0.5).sum())

def smooth_normal_box(nrm, box, size=13):
    """Flatten a ridge in the normal map inside a box: texels go to the wide median of their neighborhood, feathered at the box edge."""
    y0, y1, x0, x1 = box; pad = size
    a0, a1, b0, b1 = max(0, y0 - pad), min(S, y1 + pad), max(0, x0 - pad), min(S, x1 + pad)
    a = nrm[a0:a1, b0:b1].astype(np.float32)
    base = np.stack([ndimage.median_filter(a[..., c], size=size, mode="nearest") for c in range(3)], -1)
    base = ndimage.gaussian_filter(base, (1.5, 1.5, 0))
    w = np.zeros(a.shape[:2], np.float32); w[y0 - a0:y1 - a0, x0 - b0:x1 - b0] = 1; w = ndimage.gaussian_filter(w, 2.5)[..., None]
    out = nrm.copy(); out[a0:a1, b0:b1] = (a * (1 - w) + base * w + 0.5).astype(np.uint8)
    return out

SKIN_BONES = ("Head", "Neck", "Hand", "ForeArm", "Arm", "Shoulder")
def skin_mask(img, tris, bone, names):
    """Skin texels: on triangles that follow the head, neck, arms or hands, and close in color to the face's own skin."""
    a = img.astype(np.float32) / 255; h, s, v = rgb2hsv(a); hh = np.where(h > 180, h - 360, h)
    head = raster(tris, bone_sel(bone, names, ("Head", "Neck")), outline=False)
    body = raster(tris, bone_sel(bone, names, SKIN_BONES))
    rough = (hh > -15) & (hh < 40) & (s > 0.08) & (s < 0.62) & (v > 0.40)
    f = head & rough
    mh, ms, mv = np.median(hh[f]), np.median(s[f]), np.median(v[f])
    near = (np.abs(hh - mh) < 9) & (np.abs(s - ms) < 0.13) & (v > mv - 0.30) & (v < mv + 0.2)
    # shadowed skin (under the chin, between fingers) is darker and a little more saturated: allow it next to skin already found
    m = body & near
    grow = body & (np.abs(hh - mh) < 12) & (s > ms - 0.1) & (s < ms + 0.22) & (v > mv - 0.45)
    m = ndimage.binary_propagation(m, mask=m | grow)
    m = ndimage.binary_opening(m, iterations=1) | (m & ndimage.binary_dilation(ndimage.binary_opening(m, iterations=1), iterations=2))
    return m, (float(mh), float(ms), float(mv))
def grade_skin(img, mask, cur, target):
    """Move the skin from its median (hue, sat, val) to the target one; every texel keeps its own offset from the median."""
    a = img.astype(np.float32) / 255; h, s, v = rgb2hsv(a); hh = np.where(h > 180, h - 360, h)
    dh = target[0] - cur[0]; ks = target[1] / max(cur[1], 1e-3); kv = target[2] / max(cur[2], 1e-3)
    g = hsv2rgb(hh + dh, np.clip(s * ks, 0, 1), np.clip(v * kv, 0, 1))
    w = ndimage.gaussian_filter(mask.astype(np.float32), 0.8)[..., None]
    return ((a * (1 - w) + g * w) * 255 + 0.5).astype(np.uint8)
