// Bake a kit car's paint into its atlas, the way finishMaterial() in StreetEmpire src/kit.js repaints it on the
// GPU: the body hue is found from the mask's green (body) area, then body pixels near that hue are moved to the
// paint color keeping their shading, and the mask's red (glass) area is darkened. Unity has no shader for this
// yet, so the painted atlas is a texture of its own.
// Teal left in the atlas after painting sits outside the mask's body area, which kit.js leaves alone too.
// Usage: node paint_car.mjs <car.glb> <0xRRGGBB paint> <out.jpg>
import { NodeIO } from '@gltf-transform/core';
import sharp from 'sharp';
const [src, hex, out] = process.argv.slice(2);
const doc = await new NodeIO().read(src);
const m = doc.getRoot().listMaterials()[0];
const base = await sharp(Buffer.from(m.getBaseColorTexture().getImage())).removeAlpha().raw().toBuffer({ resolveWithObject: true });
const W = base.info.width, H = base.info.height;
const maskTex = m.getEmissiveTexture();
const mask = maskTex ? await sharp(Buffer.from(maskTex.getImage())).resize(W, H).removeAlpha().raw().toBuffer() : null;
const lin = (c) => (c <= 0.04045 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4);
const srgb = (c) => (c <= 0.0031308 ? c * 12.92 : 1.055 * c ** (1 / 2.4) - 0.055);
const hsv = (r, g, b) => { const mx = Math.max(r, g, b), mn = Math.min(r, g, b), d = mx - mn; let h = 0; if (d > 1e-6) h = mx === r ? ((g - b) / d) % 6 : mx === g ? (b - r) / d + 2 : (r - g) / d + 4; return [((h / 6) + 1) % 1, mx > 0 ? d / mx : 0, mx]; };
const rgb = (h, s, v) => { const f = (n) => { const k = (n + h * 6) % 6; return v - v * s * Math.max(0, Math.min(k, 4 - k, 1)); }; return [f(5), f(3), f(1)]; };
const ss = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };

// paintKey(): dominant hue of the lit, saturated body pixels on a 128 px copy (sRGB values, as the canvas sees them)
const N = 128;
const small = await sharp(Buffer.from(m.getBaseColorTexture().getImage())).resize(N, N, { fit: 'fill' }).removeAlpha().raw().toBuffer();
const smallMask = maskTex ? await sharp(Buffer.from(maskTex.getImage())).resize(N, N, { fit: 'fill' }).removeAlpha().raw().toBuffer() : null;
const bins = new Array(36).fill(0), sum = []; let lit = 0, sat = 0;
for (let i = 0; i < N * N; i++) {
  if (smallMask && smallMask[i * 3 + 1] < 128) continue;
  const [h, s, v] = hsv(small[i * 3] / 255, small[i * 3 + 1] / 255, small[i * 3 + 2] / 255);
  if (v < 0.12) continue; lit++; if (s < 0.25) continue; sat++;
  const k = Math.floor(h * 36) % 36; bins[k]++; (sum[k] ||= []).push([small[i * 3], small[i * 3 + 1], small[i * 3 + 2]]);
}
let key = null;
if (lit && sat / lit >= 0.05) {
  let best = 0; const tot = (k) => bins[k] + bins[(k + 1) % 36] + bins[(k + 35) % 36];
  for (let k = 0; k < 36; k++) if (tot(k) > tot(best)) best = k;
  const pick = [].concat(sum[best] || [], sum[(best + 1) % 36] || [], sum[(best + 35) % 36] || []);
  const avg = pick.reduce((a, p) => [a[0] + p[0], a[1] + p[1], a[2] + p[2]], [0, 0, 0]).map((v) => lin(v / pick.length / 255));
  key = hsv(...avg);
}
const P = parseInt(hex, 16), paint = hsv(lin(((P >> 16) & 255) / 255), lin(((P >> 8) & 255) / 255), lin((P & 255) / 255));
const neutral = !!mask && !key;
console.log('key', key && key.map((v) => v.toFixed(3)), 'neutral', neutral, 'size', W, H);
const px = Buffer.alloc(W * H * 3);
for (let i = 0; i < W * H; i++) {
  let c = [lin(base.data[i * 3] / 255), lin(base.data[i * 3 + 1] / 255), lin(base.data[i * 3 + 2] / 255)];
  const hv = hsv(...c);
  let dh = Math.abs(hv[0] - (key ? key[0] : 0)); dh = Math.min(dh, 1 - dh);
  let body = (key ? 1 : 0) * (1 - ss(0.035, 0.065, dh)) * ss(0.10, 0.22, hv[1]);
  let glass = (1 - ss(0.25, 0.4, hv[1])) * ss(0.006, 0.014, hv[2]) * (1 - ss(0.07, 0.11, hv[2]));
  if (mask) { const mr = mask[i * 3] / 255, mg = mask[i * 3 + 1] / 255; glass = mr; body *= mg; if (neutral) body = mg * (1 - ss(0.38, 0.55, hv[1])) * ss(0.08, 0.22, hv[2]); }
  const nh = neutral ? [paint[0], paint[1], Math.min(1, hv[2] * paint[2] * 1.1)]
    : [paint[0], Math.min(1, hv[1] * paint[1] / Math.max(key[1], 1e-3)), Math.min(1, hv[2] * paint[2] / Math.max(key[2], 1e-3))];
  const p = rgb(...nh);
  c = c.map((v, k) => (v + (p[k] - v) * body) * (1 - 0.45 * glass));
  for (let k = 0; k < 3; k++) px[i * 3 + k] = Math.round(Math.min(1, Math.max(0, srgb(c[k]))) * 255);
}
await sharp(px, { raw: { width: W, height: H, channels: 3 } }).jpeg({ quality: 88 }).toFile(out);
console.log('wrote', out);
