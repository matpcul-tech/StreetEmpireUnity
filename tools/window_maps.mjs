// Window glass and lit rooms for the Meshy kit buildings, whose windows are painted near black into one shattered
// atlas. For every texel of the base color map it finds the surface point it lands on (triangles rasterized in UV
// space), and marks it as window glass when it is dark and grey on a wall (not a roof, not the ground strip).
// v73 mask: thresholded, eroded twice and grown once (windows shrink a pixel, grime and seams drop out), kept only as
// compact pieces; glass gets a low roughness, and about one window in three (pieces of one window decide together)
// gets a dim warm glow, only where the mask is above 0.8.
// Writes the maps into the GLB as glTF emissive and metallic-roughness textures, for a Sovereign Asset pass.
//   node window_maps.mjs <in.glb> <out.glb> [seed]
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import sharp from 'sharp';
const [inp, out, seedArg = '1'] = process.argv.slice(2);
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const doc = await io.read(inp);
const root = doc.getRoot(); const mat = root.listMaterials()[0];
const bcTex = mat.getBaseColorTexture();
const { data: bc, info } = await sharp(Buffer.from(bcTex.getImage())).removeAlpha().raw().toBuffer({ resolveWithObject: true });
const W = info.width, H = info.height;
// geometry in model space (node transforms applied)
const tris = [];
let lo = [1e9, 1e9, 1e9], hi = [-1e9, -1e9, -1e9];
for (const node of root.listNodes()) {
  const mesh = node.getMesh(); if (!mesh) continue;
  const M = node.getWorldMatrix();
  const xf = (p) => [M[0] * p[0] + M[4] * p[1] + M[8] * p[2] + M[12], M[1] * p[0] + M[5] * p[1] + M[9] * p[2] + M[13], M[2] * p[0] + M[6] * p[1] + M[10] * p[2] + M[14]];
  for (const prim of mesh.listPrimitives()) {
    const P = prim.getAttribute('POSITION'), T = prim.getAttribute('TEXCOORD_0'), I = prim.getIndices();
    if (!P || !T) continue;
    const n = I ? I.getCount() : P.getCount();
    const pos = [], uv = [];
    for (let i = 0; i < P.getCount(); i++) { const p = xf(P.getElement(i, [])); pos.push(p); uv.push(T.getElement(i, [])); for (let k = 0; k < 3; k++) { lo[k] = Math.min(lo[k], p[k]); hi[k] = Math.max(hi[k], p[k]); } }
    for (let i = 0; i < n; i += 3) { const a = I ? I.getScalar(i) : i, b = I ? I.getScalar(i + 1) : i + 1, c = I ? I.getScalar(i + 2) : i + 2; tris.push([pos[a], pos[b], pos[c], uv[a], uv[b], uv[c]]); }
  }
}
const size = Math.max(hi[0] - lo[0], hi[2] - lo[2]); const cell = size / 4;   // about 3 m bays on a 13 m lot, one storey tall
const floorY = lo[1] + (hi[1] - lo[1]) * 0.06;
let seed = Number(seedArg);
const hash = (x, y, z) => { let h = (x * 73856093) ^ (y * 19349663) ^ (z * 83492791) ^ (seed * 2654435761); h = Math.imul(h ^ (h >>> 13), 1274126177); return ((h ^ (h >>> 16)) >>> 0) / 4294967296; };
const emi = Buffer.alloc(W * H * 3), mr = Buffer.alloc(W * H * 3);
for (let i = 0; i < W * H; i++) { mr[i * 3] = 0; mr[i * 3 + 1] = 225; mr[i * 3 + 2] = 0; } // roughness 0.88, not metal
const smooth = (a, b, x) => { const t = Math.min(1, Math.max(0, (x - a) / (b - a))); return t * t * (3 - 2 * t); };
// pass 1: how much each texel looks like window glass (dark, grey, on a wall above the ground strip), and where it is
const COV = new Uint8Array(W * H), M = new Float32Array(W * H), PX = new Float32Array(W * H * 3), SIDE = new Uint8Array(W * H);
for (const [A, B, C, ua, ub, uc] of tris) {
  const e1 = [B[0] - A[0], B[1] - A[1], B[2] - A[2]], e2 = [C[0] - A[0], C[1] - A[1], C[2] - A[2]];
  const nx = e1[1] * e2[2] - e1[2] * e2[1], ny = e1[2] * e2[0] - e1[0] * e2[2], nz = e1[0] * e2[1] - e1[1] * e2[0];
  const nl = Math.hypot(nx, ny, nz) || 1; if (Math.abs(ny / nl) > 0.3) continue;   // walls only
  const side = Math.abs(nx) > Math.abs(nz) ? (nx > 0 ? 1 : 2) : (nz > 0 ? 3 : 4);
  const px = [ua[0] * W, ub[0] * W, uc[0] * W], py = [ua[1] * H, ub[1] * H, uc[1] * H];   // glTF uv: v down = image rows
  const x0 = Math.max(0, Math.floor(Math.min(...px))), x1 = Math.min(W - 1, Math.ceil(Math.max(...px)));
  const y0 = Math.max(0, Math.floor(Math.min(...py))), y1 = Math.min(H - 1, Math.ceil(Math.max(...py)));
  const den = (py[1] - py[2]) * (px[0] - px[2]) + (px[2] - px[1]) * (py[0] - py[2]); if (Math.abs(den) < 1e-9) continue;
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    const sx = x + 0.5, sy = y + 0.5;
    const w0 = ((py[1] - py[2]) * (sx - px[2]) + (px[2] - px[1]) * (sy - py[2])) / den, w1 = ((py[2] - py[0]) * (sx - px[2]) + (px[0] - px[2]) * (sy - py[2])) / den, w2 = 1 - w0 - w1;
    if (w0 < 0 || w1 < 0 || w2 < 0) continue;   // inside the triangle only, no bleed over island edges
    const P = [0, 1, 2].map((k) => w0 * A[k] + w1 * B[k] + w2 * C[k]);
    if (P[1] < floorY) continue;
    const i = y * W + x, r = bc[i * 3], g = bc[i * 3 + 1], b = bc[i * 3 + 2];
    const lum = 0.3 * r + 0.59 * g + 0.11 * b, sat = Math.max(r, g, b) - Math.min(r, g, b);
    COV[i] = 1; M[i] = (1 - smooth(26, 50, lum)) * (1 - smooth(24, 46, sat));
    PX[i * 3] = P[0]; PX[i * 3 + 1] = P[1]; PX[i * 3 + 2] = P[2]; SIDE[i] = side;
  }
}
// pass 2: a clean window mask. Threshold, then erode twice and grow back once: thin dark grime and seams drop out and
// every window shrinks by a pixel so the glow never touches the frame or the wall. Texels no triangle covers do not
// count, so the shattered atlas islands do not erode each other along their seams
let mask = new Uint8Array(W * H); for (let i = 0; i < W * H; i++) mask[i] = M[i] > 0.5 ? 1 : 0;
const morph = (src, grow) => { const o = new Uint8Array(W * H); for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { let v = grow ? 0 : 1; for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) { const xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue; const q = yy * W + xx; if (!COV[q]) continue; const s = src[q]; if (grow) v |= s; else v &= s; } o[y * W + x] = v & COV[y * W + x]; } return o; };
mask = morph(morph(morph(mask, false), false), true);
// pass 3: windows are compact pieces: drop specks and big dark areas (shadow, a dark roof edge, a door slab)
const lab = new Int32Array(W * H).fill(-1); const comps = [];
const minPx = Math.max(12, Math.round(40 * W * H / (1024 * 1024))), maxPx = Math.round(0.02 * W * H);
for (let s = 0; s < W * H; s++) {
  if (!mask[s] || lab[s] >= 0) continue;
  const id = comps.length, stack = [s], px = []; lab[s] = id;
  while (stack.length) { const p = stack.pop(); px.push(p); const x = p % W, y = (p / W) | 0;
    for (const q of [x > 0 ? p - 1 : -1, x < W - 1 ? p + 1 : -1, y > 0 ? p - W : -1, y < H - 1 ? p + W : -1]) if (q >= 0 && mask[q] && lab[q] < 0) { lab[q] = id; stack.push(q); } }
  let cx = 0, cy = 0, cz = 0, sides = [0, 0, 0, 0, 0]; for (const p of px) { cx += PX[p * 3]; cy += PX[p * 3 + 1]; cz += PX[p * 3 + 2]; sides[SIDE[p]]++; }
  comps.push({ px, ok: px.length >= minPx && px.length <= maxPx, c: [cx / px.length, cy / px.length, cz / px.length], side: sides.indexOf(Math.max(...sides)) });
}
// one window decides as a whole: the kept glass texels are binned into small cells in model space, per side, and
// cells that touch are one window, however many atlas pieces it was painted over
const vox = size * 0.012, cellOf = new Map(); const good = comps.map((c, i) => i).filter((i) => comps[i].ok);
const keyOf = (p) => SIDE[p] + ':' + Math.floor(PX[p * 3] / vox) + ':' + Math.floor(PX[p * 3 + 1] / vox) + ':' + Math.floor(PX[p * 3 + 2] / vox);
for (const i of good) for (const p of comps[i].px) cellOf.set(keyOf(p), -1);
let nWin = 0;
for (const k of cellOf.keys()) {
  if (cellOf.get(k) >= 0) continue;
  const id = nWin++; const stack = [k]; cellOf.set(k, id);
  while (stack.length) { const [sd, x, y, z] = stack.pop().split(':').map(Number);
    for (let dx = -1; dx <= 1; dx++) for (let dy = -1; dy <= 1; dy++) for (let dz = -1; dz <= 1; dz++) { const q = sd + ':' + (x + dx) + ':' + (y + dy) + ':' + (z + dz); if (cellOf.get(q) === -1) { cellOf.set(q, id); stack.push(q); } } }
}
const find = (i, p) => cellOf.get(keyOf(p));
// the final window mask, softened over a 3 x 3 neighborhood (uncovered texels skipped): 1 inside a window, falling off
// over its last pixel, so emitting only above 0.8 keeps the glow inside the frame
const keep = new Uint8Array(W * H); for (const i of good) for (const p of comps[i].px) keep[p] = 1;
const soft = new Float32Array(W * H);
for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) { const i = y * W + x; if (!keep[i]) continue; let n = 0, s = 0;
  for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) { const xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue; const q = yy * W + xx; if (!COV[q]) continue; n++; s += keep[q]; }
  soft[i] = n ? s / n : 0; }
let glassTexels = 0, litTexels = 0, windows = 0, litWindows = 0; const seen = new Set();
for (const i of good) {
  for (const p of comps[i].px) {
    const root = find(i, p); const lit = hash(root, 17, 3) < 0.35; const k = 0.32 + 0.18 * hash(root, 5, 9), warm = hash(root, 2, 11);
    if (!seen.has(root)) { seen.add(root); windows++; if (lit) litWindows++; }
    glassTexels++;
    mr[p * 3 + 1] = Math.round(225 - Math.min(1, M[p]) * (225 - 30));   // glass roughness about 0.12
    if (lit && soft[p] > 0.8) { litTexels++; emi[p * 3] = Math.round(255 * k); emi[p * 3 + 1] = Math.round((120 + 30 * warm) * k); emi[p * 3 + 2] = Math.round((36 + 24 * warm) * k); }
  }
}
const png = async (buf) => sharp(buf, { raw: { width: W, height: H, channels: 3 } }).png().toBuffer();
const eT = doc.createTexture('windows_emissive').setImage(await png(emi)).setMimeType('image/png');
const mT = doc.createTexture('windows_metal_rough').setImage(await png(mr)).setMimeType('image/png');
mat.setEmissiveTexture(eT).setEmissiveFactor([1, 1, 1]).setMetallicRoughnessTexture(mT).setMetallicFactor(1).setRoughnessFactor(1);
await io.write(out, doc);
console.log(`WINDOWS ${inp.split('/').pop()} ${windows} windows, ${litWindows} lit; glass ${(100 * glassTexels / (W * H)).toFixed(2)}% of the map, emitting ${(100 * litTexels / (W * H)).toFixed(2)}%`);
