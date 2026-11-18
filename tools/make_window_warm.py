# v75f: the lit window panes' emission map (Matthew: the white house and pawn shop panes were flat, near white
# squares). Four 256 px variants in a 2x2 atlas, each a warm room seen through the glass: A blinds (slats, a little
# darker at the top), B curtains (darker folds at both sides, warm gap), C a lamp low in one corner (soft falloff),
# D a blind half down. Near neutral warm values (DayCycle's emission color adds the amber) at about half brightness.
#   python3 make_window_warm.py <out.png>
import sys, math
from PIL import Image, ImageFilter
import random
random.seed(7)
S = 256
def warm(t):                     # 0..1 brightness to a warm near neutral
    t = max(0.0, min(1.0, t * 1.3)); return (int(255 * t), int(236 * t ** 1.05), int(205 * t ** 1.12))
def tile(f):
    im = Image.new('RGB', (S, S))
    px = im.load()
    for y in range(S):
        for x in range(S):
            px[x, y] = warm(f(x / (S - 1), y / (S - 1)))   # y 0 is the top of the texture
    return im.filter(ImageFilter.GaussianBlur(1.2))
def room(u, v, cx=0.35, cy=0.75, r=0.9):
    d = math.hypot(u - cx, (v - cy) * 1.2); return 0.30 + 0.38 * max(0.0, 1 - d / r)
def A(u, v):   # blinds
    slat = 0.78 + 0.22 * (0.5 + 0.5 * math.cos(v * 2 * math.pi * 14))
    return room(u, v) * slat * (0.8 + 0.2 * v)
def B(u, v):   # curtains
    edge = min(u, 1 - u); fold = 0.85 + 0.15 * math.cos(u * 2 * math.pi * 9)
    cur = 0.42 * fold if edge < 0.24 else 1.0
    return room(u, v, 0.5, 0.6, 1.0) * (cur if edge < 0.24 else 1.0) * (0.85 if edge < 0.24 else 1)
def C(u, v):   # lamp low in a corner
    d = math.hypot(u - 0.78, v - 0.82); return 0.22 + 0.55 * max(0.0, 1 - d / 0.85) ** 1.6
def D(u, v):   # blind half down
    if v < 0.45: return 0.26 * (0.8 + 0.2 * (0.5 + 0.5 * math.cos(v * 2 * math.pi * 12)))
    return room(u, v, 0.4, 0.85, 0.8) * (0.75 + 0.25 * (v - 0.45) / 0.55)
out = Image.new('RGB', (S * 2, S * 2))
for i, f in enumerate((A, B, C, D)): out.paste(tile(f), ((i % 2) * S, (i // 2) * S))
out.save(sys.argv[1]); print('saved', sys.argv[1])
