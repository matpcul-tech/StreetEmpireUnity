// A triangle whose UVs are stretched across the atlas (one corner far from the other two) draws a thin line of
// whatever lies between. Give that corner a vertex of its own, with the UV its shape on the surface calls for.
// node stretch.mjs <in.glb> <out.glb|->
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions'; import sharp from 'sharp';
const [src, dst] = process.argv.slice(2);
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS); const doc = await io.read(src);
const prim = doc.getRoot().listMeshes()[0].listPrimitives()[0]; const mat = prim.getMaterial();
const sem = prim.listSemantics(); const acc = Object.fromEntries(sem.map(s => [s, prim.getAttribute(s)]));
const pos = acc.POSITION.getArray(), uv0 = acc.TEXCOORD_0.getArray(), idx = Array.from(prim.getIndices().getArray()); const nv = acc.POSITION.getCount(), nt = idx.length / 3;
const { data, info } = await sharp(Buffer.from(mat.getBaseColorTexture().getImage())).removeAlpha().raw().toBuffer({ resolveWithObject: true });
const W = info.width, H = info.height;
const tex = (u, v) => { const x = Math.min(W - 1, Math.max(0, Math.floor(u * W))), y = Math.min(H - 1, Math.max(0, Math.floor(v * H))); const o = (y * W + x) * 3; return [data[o], data[o + 1], data[o + 2]]; };
const p3 = (i) => [pos[3 * i], pos[3 * i + 1], pos[3 * i + 2]], p2 = (i) => [uv0[2 * i], uv0[2 * i + 1]];
const d3 = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1], a[2] - b[2]), d2 = (a, b) => Math.hypot(a[0] - b[0], a[1] - b[1]);
const ratios = []; for (let t = 0; t < nt; t++) { const v = [idx[3 * t], idx[3 * t + 1], idx[3 * t + 2]]; let m3 = 0, m2 = 0; for (let k = 0; k < 3; k++) { m3 = Math.max(m3, d3(p3(v[k]), p3(v[(k + 1) % 3]))); m2 = Math.max(m2, d2(p2(v[k]), p2(v[(k + 1) % 3]))); } ratios.push(m3 > 1e-7 ? m2 / m3 : 0); }
const med = [...ratios].sort((a, b) => a - b)[nt >> 1];
const add = []; const rep = [];
for (let t = 0; t < nt; t++) {
  if (ratios[t] < 20 * med) continue;
  const v = [idx[3 * t], idx[3 * t + 1], idx[3 * t + 2]];
  // the edge whose UV length fits its length on the surface best is kept; the corner across from it moves
  let bk = -1, br = 1e9; for (let k = 0; k < 3; k++) { const a = v[k], b = v[(k + 1) % 3]; const l3 = d3(p3(a), p3(b)); if (l3 < 1e-7) continue; const r = d2(p2(a), p2(b)) / l3; if (r < br) { br = r; bk = k; } }
  if (bk < 0 || br > 3 * med) { rep.push({ t, skipped: 'no sound edge', y: +p3(v[0])[1].toFixed(3) }); continue; }
  const a = v[bk], b = v[(bk + 1) % 3], c = v[(bk + 2) % 3]; const A = p3(a), B = p3(b), C = p3(c), ua = p2(a), ub = p2(b);
  const ab = [B[0] - A[0], B[1] - A[1], B[2] - A[2]], ac = [C[0] - A[0], C[1] - A[1], C[2] - A[2]]; const L = Math.hypot(...ab);
  const tpar = (ab[0] * ac[0] + ab[1] * ac[1] + ab[2] * ac[2]) / (L * L); const hgt = Math.sqrt(Math.max(0, ac[0] ** 2 + ac[1] ** 2 + ac[2] ** 2 - (tpar * L) ** 2));
  const e = [ub[0] - ua[0], ub[1] - ua[1]]; const el = Math.hypot(...e) || 1e-9; const n = [-e[1] / el, e[0] / el]; const s = el / L;
  const base = [ua[0] + e[0] * tpar, ua[1] + e[1] * tpar]; const mid = tex((ua[0] + ub[0]) / 2, (ua[1] + ub[1]) / 2);
  let best = null; for (const sg of [1, -1]) { const q = [base[0] + n[0] * hgt * s * sg, base[1] + n[1] * hgt * s * sg]; const cc = tex((ua[0] + ub[0] + q[0]) / 3, (ua[1] + ub[1] + q[1]) / 3); const dd = Math.hypot(cc[0] - mid[0], cc[1] - mid[1], cc[2] - mid[2]); if (!best || dd < best[1]) best = [q, dd]; }
  add.push({ t, corner: (bk + 2) % 3, from: c, uv: best[0] }); rep.push({ y: +C[1].toFixed(3), x: +C[0].toFixed(3), ratio: +(ratios[t] / med).toFixed(0) });
}
console.log(src.split('/').pop(), 'tris', nt, 'stretched', rep.length, 'fixed', add.length, JSON.stringify(rep.sort((a, b) => b.y - a.y).slice(0, 14)));
if (dst && dst !== '-' ) {
  if (add.length) {
    for (const s of sem) { const A = acc[s]; const es = A.getElementSize(); const old = A.getArray(); const arr = new old.constructor((nv + add.length) * es); arr.set(old);
      add.forEach((x, k) => { for (let j = 0; j < es; j++) arr[(nv + k) * es + j] = old[x.from * es + j]; if (s === 'TEXCOORD_0') { arr[(nv + k) * es] = x.uv[0]; arr[(nv + k) * es + 1] = x.uv[1]; } }); A.setArray(arr); }
    add.forEach((x, k) => { idx[3 * x.t + x.corner] = nv + k; });
    const I = prim.getIndices(); I.setArray(new (nv + add.length > 65535 ? Uint32Array : I.getArray().constructor)(idx));
  }
  await io.write(dst, doc);
}
