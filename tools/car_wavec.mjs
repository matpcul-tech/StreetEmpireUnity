// Vehicles wave C (release meshy-vehicles-wave-c-2026-11-23) for Unity. They arrive in real meters, nose along +Z,
// with named materials (paint, trim, marking, lights share one color map; well, glass, wheel_rim and wheel_tire are
// flat colors) and wheel nodes at the hubs. This step only prepares them the way the v2 cars are prepared:
// - the color map goes out as a JPEG file, <textures dir>/<kind>_body.jpg, and the GLB carries no image, so Unity
//   imports it as an ordinary compressed texture shared by the near and far car
// - the glass gets the shared glass UVs (by height and length, see car_v2.mjs) for Textures/Cars/V2/glass.png
// - KHR_materials_specular is dropped (the game's material converter does not read it)
// - a far version, same UVs
// - the buses: the painted skin that lies under the glass pokes through it as light shards (phone and CI frames,
//   build 7d05e48), so body triangles hugging the glass are drawn with the glass material instead
// - the armored truck: its map's small islands were baked in mismatched grays and its plates are shaded facet by
//   facet, which reads as crumpled panels; the islands level to the panel gray and normals are shared across a
//   plate (faces within 40 degrees)
// Size, wheel pivots and material names are as delivered.
// Run from a folder with @gltf-transform/*, meshoptimizer and sharp.
// Usage: node car_wavec.mjs <src dir> <models out dir> <textures out dir> <kind>...
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { weld, simplify, prune, cloneDocument } from '@gltf-transform/functions'; import { MeshoptSimplifier } from 'meshoptimizer';
import sharp from 'sharp'; import fs from 'node:fs';
await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const [src, out, tex, ...kinds] = process.argv.slice(2);
fs.mkdirSync(`${out}/LOD1`, { recursive: true }); fs.mkdirSync(tex, { recursive: true });
const tris = (d) => { let t = 0; for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) t += p.getIndices().getCount() / 3; return t; };
const keep = { keepAttributes: true };
const SKIN = ['school-bus', 'city-bus'];
// glass triangles wound facing into the bus are not drawn from outside, so the gray inside shows through the pane
// (the city bus's zigzag): they are rewound to face out from the middle of the bus
function faceGlassOut(d) {
  let turned = 0; const a = [0, 0, 0], b = [0, 0, 0], c = [0, 0, 0];
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) {
    if (p.getMaterial().getName() !== 'glass') continue;
    const pos = p.getAttribute('POSITION'), idx = p.getIndices(), arr = Uint32Array.from(idx.getArray());
    let zc = 0; for (let i = 0; i < pos.getCount(); i++) { pos.getElement(i, a); zc += a[2]; } zc /= pos.getCount();
    for (let i = 0; i < arr.length; i += 3) {
      pos.getElement(arr[i], a); pos.getElement(arr[i + 1], b); pos.getElement(arr[i + 2], c);
      const u = [b[0] - a[0], b[1] - a[1], b[2] - a[2]], w = [c[0] - a[0], c[1] - a[1], c[2] - a[2]];
      const fn = [u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0]];
      const mx = (a[0] + b[0] + c[0]) / 3, mz = (a[2] + b[2] + c[2]) / 3 - zc;
      // out is sideways on the flanks and along the bus at its ends
      const out = Math.abs(fn[0]) >= Math.abs(fn[2]) ? fn[0] * mx : fn[2] * mz;
      if (out < 0 && Math.abs(fn[1]) < Math.max(Math.abs(fn[0]), Math.abs(fn[2]))) { const t = arr[i + 1]; arr[i + 1] = arr[i + 2]; arr[i + 2] = t; turned++; }
    }
    p.setIndices(d.createAccessor().setType('SCALAR').setArray(arr).setBuffer(d.getRoot().listBuffers()[0]));
  }
  return turned;
}
// body triangles within 9 cm of the glass surface (all three corners and the middle) move to the glass material
function skinUnderGlass(d) {
  const root = d.getRoot(), buf = root.listBuffers()[0]; const glassMat = root.listMaterials().find((m) => m.getName() === 'glass');
  const cell = 0.09, grid = new Map(), side = new Map(), key = (x, y, z) => `${Math.floor(x / cell)},${Math.floor(y / cell)},${Math.floor(z / cell)}`;
  let y0 = 9, y1 = -9; const a = [0, 0, 0], b = [0, 0, 0], c = [0, 0, 0];
  for (const m of root.listMeshes()) for (const p of m.listPrimitives()) {
    if (p.getMaterial() !== glassMat) continue;
    const pos = p.getAttribute('POSITION'), idx = p.getIndices();
    for (let i = 0; i < idx.getCount(); i += 3) {
      pos.getElement(idx.getScalar(i), a); pos.getElement(idx.getScalar(i + 1), b); pos.getElement(idx.getScalar(i + 2), c);
      y0 = Math.min(y0, a[1], b[1], c[1]); y1 = Math.max(y1, a[1], b[1], c[1]);
      const n = Math.max(1, Math.ceil(Math.max(Math.hypot(b[0] - a[0], b[1] - a[1], b[2] - a[2]), Math.hypot(c[0] - a[0], c[1] - a[1], c[2] - a[2])) / 0.02));
      for (let u = 0; u <= n; u++) for (let v = 0; v <= n - u; v++) {
        const s = u / n, t = v / n, x = a[0] + (b[0] - a[0]) * s + (c[0] - a[0]) * t, y = a[1] + (b[1] - a[1]) * s + (c[1] - a[1]) * t, z = a[2] + (b[2] - a[2]) * s + (c[2] - a[2]) * t;
        const k = key(x, y, z); let g = grid.get(k); if (!g) grid.set(k, g = []); if (g.length < 6) g.push(x, y, z);
        if (Math.abs(x) > 0.9) { const k2 = `${x > 0 ? 1 : 0},${Math.floor(y / 0.04)},${Math.floor(z / 0.04)}`; const ax = Math.abs(x); const e = side.get(k2); if (e === undefined || ax > e) side.set(k2, ax); }
      }
    }
  }
  const near = (q) => {
    const cx = Math.floor(q[0] / cell), cy = Math.floor(q[1] / cell), cz = Math.floor(q[2] / cell);
    for (let i = -1; i <= 1; i++) for (let j = -1; j <= 1; j++) for (let l = -1; l <= 1; l++) {
      const g = grid.get(`${cx + i},${cy + j},${cz + l}`); if (!g) continue;
      for (let o = 0; o < g.length; o += 3) if (Math.hypot(g[o] - q[0], g[o + 1] - q[1], g[o + 2] - q[2]) < cell) return true;
    }
    return false;
  };
  // or the glass lies straight behind it on a side (the city bus's panes sit up to 17 cm inside its skin)
  const covers = (q) => {
    if (Math.abs(q[0]) < 0.9) return false; const sx = q[0] > 0 ? 1 : 0, cy = Math.floor(q[1] / 0.04), cz = Math.floor(q[2] / 0.04);
    for (let j = -1; j <= 1; j++) for (let l = -1; l <= 1; l++) { const e = side.get(`${sx},${cy + j},${cz + l}`); if (e !== undefined && Math.abs(q[0]) - e > -0.09 && Math.abs(q[0]) - e < 0.3) return true; }
    return false;
  };
  const hit = (q) => near(q) || covers(q);
  let moved = 0;
  for (const m of root.listMeshes()) for (const p of m.listPrimitives()) {
    if (!['paint', 'trim', 'marking'].includes(p.getMaterial().getName())) continue;
    const pos = p.getAttribute('POSITION'), nor = p.getAttribute('NORMAL'), idx = p.getIndices(); const stay = [], go = [];
    for (let i = 0; i < idx.getCount(); i += 3) {
      const t = [idx.getScalar(i), idx.getScalar(i + 1), idx.getScalar(i + 2)];
      pos.getElement(t[0], a); pos.getElement(t[1], b); pos.getElement(t[2], c);
      const mid = [(a[0] + b[0] + c[0]) / 3, (a[1] + b[1] + c[1]) / 3, (a[2] + b[2] + c[2]) / 3];
      (hit(a) && hit(b) && hit(c) && hit(mid) ? go : stay).push(...t);
    }
    if (!go.length) continue;
    const P = new Float32Array(go.length * 3), N = new Float32Array(go.length * 3), U = new Float32Array(go.length * 2), I = new Uint32Array(go.length);
    go.forEach((vi, o) => { pos.getElement(vi, a); nor.getElement(vi, b); P.set(a, o * 3); N.set(b, o * 3); U[o * 2] = a[2] * 0.3 + a[0] * 0.22; U[o * 2 + 1] = 1 - Math.min(1, Math.max(0, (a[1] - y0) / (y1 - y0))); I[o] = o; });
    const acc = (type, arr) => d.createAccessor().setType(type).setArray(arr).setBuffer(buf);
    const skin = d.createPrimitive().setMaterial(glassMat).setAttribute('POSITION', acc('VEC3', P)).setAttribute('NORMAL', acc('VEC3', N)).setAttribute('TEXCOORD_0', acc('VEC2', U)).setIndices(acc('SCALAR', I));
    skin.setExtras({ skin: true }); m.addPrimitive(skin);
    p.setIndices(acc('SCALAR', new Uint32Array(stay))); moved += go.length / 3;
  }
  return moved;
}
// one normal per plate: every vertex takes the faces that meet at its position and lie within 40 degrees of its own
function smoothNormals(d) {
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) {
    if (!['paint', 'trim', 'marking'].includes(p.getMaterial().getName())) continue;
    const pos = p.getAttribute('POSITION'), idx = p.getIndices(), n = pos.getCount(); const a = [0, 0, 0], b = [0, 0, 0], c = [0, 0, 0];
    const own = new Float32Array(n * 3), at = new Map(), faces = [];
    const K = (v) => `${Math.round(v[0] * 500)},${Math.round(v[1] * 500)},${Math.round(v[2] * 500)}`;
    for (let i = 0; i < idx.getCount(); i += 3) {
      const t = [idx.getScalar(i), idx.getScalar(i + 1), idx.getScalar(i + 2)];
      pos.getElement(t[0], a); pos.getElement(t[1], b); pos.getElement(t[2], c);
      const u = [b[0] - a[0], b[1] - a[1], b[2] - a[2]], w = [c[0] - a[0], c[1] - a[1], c[2] - a[2]];
      const fn = [u[1] * w[2] - u[2] * w[1], u[2] * w[0] - u[0] * w[2], u[0] * w[1] - u[1] * w[0]];   // length = twice the area
      const f = faces.length; faces.push(fn);
      [a, b, c].forEach((v, j) => { own[t[j] * 3] += fn[0]; own[t[j] * 3 + 1] += fn[1]; own[t[j] * 3 + 2] += fn[2]; const k = K(v); let g = at.get(k); if (!g) at.set(k, g = []); g.push(f); });
    }
    const out = new Float32Array(n * 3);
    for (let v = 0; v < n; v++) {
      pos.getElement(v, a); const o = [own[v * 3], own[v * 3 + 1], own[v * 3 + 2]], ol = Math.hypot(...o) || 1; const s = [0, 0, 0];
      for (const f of at.get(K(a)) || []) { const fn = faces[f], fl = Math.hypot(...fn) || 1; if ((fn[0] * o[0] + fn[1] * o[1] + fn[2] * o[2]) / (fl * ol) > 0.766) { s[0] += fn[0]; s[1] += fn[1]; s[2] += fn[2]; } }
      const l = Math.hypot(...s) || 1; out[v * 3] = s[0] / l; out[v * 3 + 1] = s[1] / l; out[v * 3 + 2] = s[2] / l;
    }
    p.setAttribute('NORMAL', d.createAccessor().setType('VEC3').setArray(out).setBuffer(d.getRoot().listBuffers()[0]));
  }
}
// the armored truck's small map islands (the left half of the atlas) level to the panel gray; dark parts stay dark
async function levelArmored(file) {
  const { data, info } = await sharp(file).raw().toBuffer({ resolveWithObject: true }); const W = info.width, H = info.height, ch = info.channels; let n = 0;
  for (let y = 0; y < H; y++) for (let x = 0; x < W * 0.5; x++) {
    const o = (y * W + x) * ch, l = 0.3 * data[o] + 0.6 * data[o + 1] + 0.1 * data[o + 2];
    if (l > 70 && Math.max(data[o], data[o + 1], data[o + 2]) - Math.min(data[o], data[o + 1], data[o + 2]) < 30) { data[o] = 206; data[o + 1] = 204; data[o + 2] = 198; n++; }
  }
  await sharp(data, { raw: { width: W, height: H, channels: ch } }).jpeg({ quality: 88 }).toFile(file + '.tmp'); fs.renameSync(file + '.tmp', file);
  return n;
}
for (const k of kinds) {
  const d = await io.read(`${src}/${k}.glb`);
  let wrote = 0;
  for (const m of d.getRoot().listMaterials()) {
    const base = m.getBaseColorTexture();
    if (base) {
      if (!wrote) { const im = sharp(Buffer.from(base.getImage())); const meta = await im.metadata(); await im.removeAlpha().jpeg({ quality: 88 }).toFile(`${tex}/${k}_body.jpg`); wrote = meta.width; }
      m.setBaseColorTexture(null);
    }
    m.setEmissiveTexture(null); m.setNormalTexture(null); m.setOcclusionTexture(null); m.setMetallicRoughnessTexture(null);
    m.setExtension('KHR_materials_specular', null);
  }
  for (const e of d.getRoot().listExtensionsUsed()) e.dispose();
  if (d.getRoot().listTextures().length > 1) console.log('  note:', k, 'had', d.getRoot().listTextures().length, 'images; only the first was written');
  await d.transform(prune(keep));
  // the city bus's 14 "marking" triangles are a gray zigzag lying across its window band: dropped
  if (k === 'city-bus') for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) if (p.getMaterial().getName() === 'marking') { m.removePrimitive(p); p.dispose(); }
  if (SKIN.includes(k)) console.log('  glass turned outward', k, faceGlassOut(d), 'triangles');
  if (SKIN.includes(k)) console.log('  skin under glass', k, skinUnderGlass(d), 'triangles');
  if (k === 'armored-truck') smoothNormals(d);
  if (k === 'armored-truck') console.log('  leveled', await levelArmored(`${tex}/${k}_body.jpg`), 'texels');
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) {
    if (p.getMaterial().getName() !== 'glass' || p.getExtras().skin) continue;
    const a = p.getAttribute('POSITION'), v = [0, 0, 0]; let y0 = 9, y1 = -9;
    for (let i = 0; i < a.getCount(); i++) { a.getElement(i, v); y0 = Math.min(y0, v[1]); y1 = Math.max(y1, v[1]); }
    const uv = new Float32Array(a.getCount() * 2);
    for (let i = 0; i < a.getCount(); i++) { a.getElement(i, v); uv[i * 2] = v[2] * 0.3 + v[0] * 0.22; uv[i * 2 + 1] = 1 - (v[1] - y0) / (y1 - y0); }
    p.setAttribute('TEXCOORD_0', d.createAccessor().setType('VEC2').setArray(uv).setBuffer(d.getRoot().listBuffers()[0]));
  }
  const far = cloneDocument(d);
  await far.transform(weld(), simplify({ simplifier: MeshoptSimplifier, ratio: 0.3, error: 0.02 }), prune(keep));
  await io.write(`${out}/${k}.glb`, d); await io.write(`${out}/LOD1/${k}.glb`, far);
  console.log(k.padEnd(16), tris(d), '->', tris(far), 'map', wrote, 'px', Math.round(fs.statSync(`${out}/${k}.glb`).size / 1024), 'KB', Math.round(fs.statSync(`${out}/LOD1/${k}.glb`).size / 1024), 'KB');
}
