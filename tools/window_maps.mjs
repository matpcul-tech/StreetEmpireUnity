// Window glass and lit rooms for the Meshy kit buildings, whose windows are painted near black into one shattered
// atlas. For every texel of the base color map it finds the surface point it lands on (triangles rasterized in UV
// space), and marks it as window glass when it is dark and grey on a wall (not a roof, not the ground strip).
// Glass gets a low roughness (it reflects the sunset sky); about two in five window bays, picked per
// bay, storey and side of the building in model space so a whole window lights together, get a dim warm emissive glow.
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
let glassTexels = 0, litTexels = 0;
for (const [A, B, C, ua, ub, uc] of tris) {
  const e1 = [B[0] - A[0], B[1] - A[1], B[2] - A[2]], e2 = [C[0] - A[0], C[1] - A[1], C[2] - A[2]];
  const nx = e1[1] * e2[2] - e1[2] * e2[1], ny = e1[2] * e2[0] - e1[0] * e2[2], nz = e1[0] * e2[1] - e1[1] * e2[0];
  const nl = Math.hypot(nx, ny, nz) || 1; if (Math.abs(ny / nl) > 0.35) continue;   // walls only
  const px = [ua[0] * W, ub[0] * W, uc[0] * W], py = [ua[1] * H, ub[1] * H, uc[1] * H];   // glTF uv: v down = image rows
  const x0 = Math.max(0, Math.floor(Math.min(...px))), x1 = Math.min(W - 1, Math.ceil(Math.max(...px)));
  const y0 = Math.max(0, Math.floor(Math.min(...py))), y1 = Math.min(H - 1, Math.ceil(Math.max(...py)));
  const den = (py[1] - py[2]) * (px[0] - px[2]) + (px[2] - px[1]) * (py[0] - py[2]); if (Math.abs(den) < 1e-9) continue;
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    const sx = x + 0.5, sy = y + 0.5;
    const w0 = ((py[1] - py[2]) * (sx - px[2]) + (px[2] - px[1]) * (sy - py[2])) / den, w1 = ((py[2] - py[0]) * (sx - px[2]) + (px[0] - px[2]) * (sy - py[2])) / den, w2 = 1 - w0 - w1;
    if (w0 < -0.02 || w1 < -0.02 || w2 < -0.02) continue;
    const P = [0, 1, 2].map((k) => w0 * A[k] + w1 * B[k] + w2 * C[k]);
    if (P[1] < floorY) continue;
    const i = y * W + x, r = bc[i * 3], g = bc[i * 3 + 1], b = bc[i * 3 + 2];
    const lum = 0.3 * r + 0.59 * g + 0.11 * b, sat = Math.max(r, g, b) - Math.min(r, g, b);
    const m = (1 - smooth(28, 52, lum)) * (1 - smooth(26, 48, sat));
    if (m <= 0.02) continue;
    glassTexels++;
    mr[i * 3 + 1] = Math.round(225 - m * (225 - 30));   // glass roughness about 0.12
    // one bay of one storey on one side of the building lights together
    const side = Math.abs(nx) > Math.abs(nz) ? (nx > 0 ? 1 : 2) : (nz > 0 ? 3 : 4);
    const cx = Math.floor((P[0] - lo[0]) / cell) * 10 + side, cy = Math.floor((P[1] - lo[1]) / (cell * 0.85)), cz = Math.floor((P[2] - lo[2]) / cell);
    const h = hash(cx, cy, cz);
    if (h < 0.4) {
      litTexels++;
      const k = m * (0.55 + 0.45 * hash(cx + 7, cy + 3, cz + 1)), warm = hash(cx + 2, cy + 9, cz + 5);
      emi[i * 3] = Math.round(255 * k); emi[i * 3 + 1] = Math.round((128 + 36 * warm) * k); emi[i * 3 + 2] = Math.round((40 + 30 * warm) * k);
    }
  }
}
const png = async (buf) => sharp(buf, { raw: { width: W, height: H, channels: 3 } }).png().toBuffer();
const eT = doc.createTexture('windows_emissive').setImage(await png(emi)).setMimeType('image/png');
const mT = doc.createTexture('windows_metal_rough').setImage(await png(mr)).setMimeType('image/png');
mat.setEmissiveTexture(eT).setEmissiveFactor([1, 1, 1]).setMetallicRoughnessTexture(mT).setMetallicFactor(1).setRoughnessFactor(1);
await io.write(out, doc);
console.log(`WINDOWS ${inp.split('/').pop()} glass ${(100 * glassTexels / (W * H)).toFixed(1)}% of the map, lit ${(100 * litTexels / Math.max(1, glassTexels)).toFixed(0)}% of the glass, ${tris.length} triangles, cell ${cell.toFixed(3)}`);
