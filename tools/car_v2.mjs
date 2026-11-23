// Cars and motorcycles v2 (releases meshy-cars-v2-2026-11-22, meshy-bikes-v2-2026-11-22) for Unity.
// - body: an even semi-gloss paint (the game's material converter does not read the metal map, and a metal factor
//   of 1 without it turns the whole car chrome); the normal map is dropped (see below)
// - glass: a touch rougher and off pure black so it does not flash white in the sun
// - the color maps go out as JPEG files (<textures dir>/<kind>_body.jpg, <kind>_wheel.jpg) and the GLBs carry no
//   images, so Unity imports them as ordinary textures (compressed, one copy shared by the near and far car)
// - a far version, same UVs
// Why no normal map and no embedded images (v75t, Nov 22): Android builds use the DXT5nm-style normal encoding
// (ProjectSettings m_BuildTargetNormalMapEncoding 1). A texture embedded in a GLB is not imported as a normal map,
// so the shader read its X from an alpha channel that is not there and every normal leaned hard along its tangent.
// These bodies are unwrapped as a mosaic of small charts, each turned its own way, so each chart lit differently:
// the cars came out in camouflage. The mesh normals are already smooth; renders with and without the map match.
// Run from a folder with @gltf-transform/*, meshoptimizer and sharp.
// Usage: node car_v2.mjs <src dir> <models out dir> <textures out dir> <kind>...
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { weld, simplify, prune, cloneDocument } from '@gltf-transform/functions'; import { MeshoptSimplifier } from 'meshoptimizer';
import sharp from 'sharp'; import fs from 'node:fs';
await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const [src, out, tex, ...kinds] = process.argv.slice(2);
fs.mkdirSync(`${out}/LOD1`, { recursive: true }); fs.mkdirSync(tex, { recursive: true });
const tris = (d) => { let t = 0; for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) t += p.getIndices().getCount() / 3; return t; };
const keep = { keepAttributes: true };   // the UVs stay although the GLB no longer names a texture
// ---- v75u: lettering, glass that reads as glass, and the SUV's black roof --------------------------------------
// Lettering goes on thin panels copied from the body's own side faces and pushed 1 cm out, with their own UVs laid
// flat across the doors, so it follows the body and reads the right way round on both sides. (It cannot be painted
// into the body map: that map is a mosaic of small charts.) The words are a PNG with alpha, <kind>_decal.png.
const LETTERING = {
  // the cruiser's lettering stays on its white doors (z -0.42 to 0.80 m); dark ink would be lost on the black
  police: { small: 'PORT CALDER', big: 'POLICE', side: '', ink: '#0f1a3a', z: [-0.42, 0.80] },
  taxi: { small: 'NIGHTOWL CAB CO.', big: 'TAXI', side: '555-0144', ink: '#141414' },
};
async function letteringPng(file, L, aspect) {
  // the image has the shape of the panel it goes on, so the letters are not stretched
  const H = 256, W = Math.round(H * aspect / 4) * 4, cx = L.side ? W * 0.44 : W * 0.5, room = (L.side ? 0.7 : 0.9) * W;
  const big = Math.min(150, room / (L.big.length * 0.78)), small = Math.min(44, room / (L.small.length * 0.8));
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${W}" height="${H}">
    <style>text{font-family:'DejaVu Sans';font-weight:bold;fill:${L.ink}}</style>
    <text x="${cx}" y="62" font-size="${small}" text-anchor="middle" letter-spacing="${small * 0.12}">${L.small}</text>
    <text x="${cx}" y="${132 + big * 0.5}" font-size="${big}" text-anchor="middle" letter-spacing="${big * 0.06}">${L.big}</text>
    ${L.side ? `<text x="${W * 0.87}" y="200" font-size="40" text-anchor="middle">${L.side}</text>` : ''}</svg>`;
  await sharp(Buffer.from(svg)).png().toFile(file);
}
function bodyPrim(d) {
  let best = null;
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives())
    if (p.getMaterial().getName() === 'body' && (!best || p.getAttribute('POSITION').getCount() > best.p.getAttribute('POSITION').getCount())) best = { m, p };
  return best;
}
function glassBelt(d) {   // the bottom of the side glass: the lettering sits under it
  let ys = [];
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) {
    if (p.getMaterial().getName() !== 'glass') continue;
    const a = p.getAttribute('POSITION'), v = [0, 0, 0]; let mx = 0;
    for (let i = 0; i < a.getCount(); i++) { a.getElement(i, v); mx = Math.max(mx, Math.abs(v[0])); }
    for (let i = 0; i < a.getCount(); i++) { a.getElement(i, v); if (Math.abs(v[0]) > 0.75 * mx) ys.push(v[1]); }
  }
  ys.sort((x, y) => x - y); return ys[Math.floor(ys.length * 0.05)];
}
function wheelZ(d) {
  const z = {}; for (const n of d.getRoot().listNodes()) if (n.getName().startsWith('wheel_')) z[n.getName()] = n.getTranslation()[2];
  return { front: z.wheel_fl, rear: z.wheel_rl };
}
function addLettering(d, rect) {
  const { m, p } = bodyPrim(d); const pos = p.getAttribute('POSITION'), nor = p.getAttribute('NORMAL'), idx = p.getIndices();
  const P = [], N = [], UV = [], I = []; const a = [0, 0, 0], n = [0, 0, 0]; let maxX = 0;
  for (let i = 0; i < pos.getCount(); i++) { pos.getElement(i, a); maxX = Math.max(maxX, Math.abs(a[0])); }
  for (const side of [1, -1]) {
    const map = new Map();
    for (let t = 0; t < idx.getCount(); t += 3) {
      const tri = [idx.getScalar(t), idx.getScalar(t + 1), idx.getScalar(t + 2)];
      let ok = true, z0 = 9, z1 = -9, y0 = 9, y1 = -9;
      for (const vi of tri) {
        pos.getElement(vi, a); nor.getElement(vi, n);
        if (n[0] * side < 0.45 || a[0] * side < 0.6 * maxX) ok = false;
        z0 = Math.min(z0, a[2]); z1 = Math.max(z1, a[2]); y0 = Math.min(y0, a[1]); y1 = Math.max(y1, a[1]);
      }
      if (!ok || z1 < rect.z0 || z0 > rect.z1 || y1 < rect.y0 || y0 > rect.y1) continue;
      for (const vi of tri) {
        if (!map.has(vi)) {
          pos.getElement(vi, a); nor.getElement(vi, n);
          map.set(vi, P.length / 3);
          P.push(a[0] + n[0] * 0.01, a[1] + n[1] * 0.01, a[2] + n[2] * 0.01); N.push(n[0], n[1], n[2]);
          // seen from outside, the car's left side reads nose to tail, the right side tail to nose
          const u = side > 0 ? (rect.z1 - a[2]) / (rect.z1 - rect.z0) : (a[2] - rect.z0) / (rect.z1 - rect.z0);
          UV.push(u, 1 - (a[1] - rect.y0) / (rect.y1 - rect.y0));
        }
        I.push(map.get(vi));
      }
    }
  }
  const buf = d.getRoot().listBuffers()[0];
  const mat = d.createMaterial('decal').setAlphaMode('MASK').setAlphaCutoff(0.5).setRoughnessFactor(0.6).setMetallicFactor(0);
  m.addPrimitive(d.createPrimitive().setMaterial(mat)
    .setAttribute('POSITION', d.createAccessor().setType('VEC3').setArray(new Float32Array(P)).setBuffer(buf))
    .setAttribute('NORMAL', d.createAccessor().setType('VEC3').setArray(new Float32Array(N)).setBuffer(buf))
    .setAttribute('TEXCOORD_0', d.createAccessor().setType('VEC2').setArray(new Float32Array(UV)).setBuffer(buf))
    .setIndices(d.createAccessor().setType('SCALAR').setArray(new Uint32Array(I)).setBuffer(buf)));
  return I.length / 3;
}
// Glass: one small shared map (glass.png, lighter at the top like a reflected sky, two soft streaks) and UVs laid
// by height and length, so a window reads as glass in any light instead of a flat black panel.
async function glassPng(file) {
  const W = 256, H = 256, px = Buffer.alloc(W * H * 3);
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const h = 1 - y / (H - 1);                                    // 1 at the top of the glass
    const band = (c, w) => { let dd = ((x / W + h * 0.55 - c) % 1 + 1) % 1; dd = Math.min(dd, 1 - dd); return Math.max(0, 1 - dd / w); };
    const k = Math.pow(h, 1.6) * 0.75 + 0.22 * band(0.3, 0.07) + 0.12 * band(0.62, 0.035);
    const o = (y * W + x) * 3; px[o] = Math.min(255, 14 + 92 * k); px[o + 1] = Math.min(255, 19 + 112 * k); px[o + 2] = Math.min(255, 26 + 130 * k);
  }
  await sharp(px, { raw: { width: W, height: H, channels: 3 } }).png().toFile(file);
}
function layGlassUVs(d) {
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) {
    if (p.getMaterial().getName() !== 'glass') continue;
    const a = p.getAttribute('POSITION'), v = [0, 0, 0]; let y0 = 9, y1 = -9;
    for (let i = 0; i < a.getCount(); i++) { a.getElement(i, v); y0 = Math.min(y0, v[1]); y1 = Math.max(y1, v[1]); }
    const uv = new Float32Array(a.getCount() * 2);
    for (let i = 0; i < a.getCount(); i++) { a.getElement(i, v); uv[i * 2] = v[2] * 0.3 + v[0] * 0.22; uv[i * 2 + 1] = 1 - (v[1] - y0) / (y1 - y0); }
    p.setAttribute('TEXCOORD_0', d.createAccessor().setType('VEC2').setArray(uv).setBuffer(d.getRoot().listBuffers()[0]));
  }
}
// The SUV's roof is painted near black in its map and reads as a black square from above: the dark texels of the
// upward faces at roof height take the door paint color.
async function paintRoof(d) {
  const { p } = bodyPrim(d); const pos = p.getAttribute('POSITION'), nor = p.getAttribute('NORMAL'), uvA = p.getAttribute('TEXCOORD_0'), idx = p.getIndices();
  const tx = p.getMaterial().getBaseColorTexture(); const { data, info } = await sharp(Buffer.from(tx.getImage())).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const W = info.width, H = info.height, a = [0, 0, 0], n = [0, 0, 0], u = [0, 0]; let yTop = -9, maxX = 0;
  for (let i = 0; i < pos.getCount(); i++) { pos.getElement(i, a); yTop = Math.max(yTop, a[1]); maxX = Math.max(maxX, Math.abs(a[0])); }
  const lum = (o) => 0.3 * data[o] + 0.6 * data[o + 1] + 0.1 * data[o + 2];
  const each = (pick, fn) => {
    for (let t = 0; t < idx.getCount(); t += 3) {
      const T = [0, 1, 2].map(j => { const vi = idx.getScalar(t + j); pos.getElement(vi, a); nor.getElement(vi, n); uvA.getElement(vi, u); return { y: a[1], x: a[0], nx: n[0], ny: n[1], px: u[0] * W, py: u[1] * H }; });
      if (!T.every(pick)) continue;
      const x0 = Math.max(0, Math.floor(Math.min(...T.map(q => q.px)) - 2)), x1 = Math.min(W - 1, Math.ceil(Math.max(...T.map(q => q.px)) + 2));
      const y0 = Math.max(0, Math.floor(Math.min(...T.map(q => q.py)) - 2)), y1 = Math.min(H - 1, Math.ceil(Math.max(...T.map(q => q.py)) + 2));
      const den = (T[1].py - T[2].py) * (T[0].px - T[2].px) + (T[2].px - T[1].px) * (T[0].py - T[2].py); if (Math.abs(den) < 1e-9) continue;
      for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
        const l1 = ((T[1].py - T[2].py) * (x + 0.5 - T[2].px) + (T[2].px - T[1].px) * (y + 0.5 - T[2].py)) / den;
        const l2 = ((T[2].py - T[0].py) * (x + 0.5 - T[2].px) + (T[0].px - T[2].px) * (y + 0.5 - T[2].py)) / den;
        const tol = 2 / Math.sqrt(Math.abs(den));   // about two texels of padding round the chart
        if (l1 >= -tol && l2 >= -tol && 1 - l1 - l2 >= -tol) fn((y * W + x) * 3);
      }
    }
  };
  const samples = [];
  each(q => Math.abs(q.nx) > 0.8 && Math.abs(q.x) > 0.8 * maxX && q.y > yTop * 0.55 && q.y < yTop * 0.62, o => samples.push([data[o], data[o + 1], data[o + 2]]));
  samples.sort((x, y) => (x[0] + x[1] + x[2]) - (y[0] + y[1] + y[2])); const paint = samples[Math.floor(samples.length * 0.6)];
  const pl = 0.3 * paint[0] + 0.6 * paint[1] + 0.1 * paint[2]; let changed = 0;
  each(q => q.ny > 0.7 && q.y > yTop * 0.93, o => { if (lum(o) < pl * 0.8) { data[o] = paint[0]; data[o + 1] = paint[1]; data[o + 2] = paint[2]; changed++; } });
  tx.setImage(new Uint8Array(await sharp(data, { raw: { width: W, height: H, channels: 3 } }).png().toBuffer())).setMimeType('image/png');
  return { paint, changed };
}
await glassPng(`${tex}/glass.png`);

for (const k of kinds) {
  const d = await io.read(`${src}/${k}.glb`);
  if (k === 'suv') console.log('  suv roof', JSON.stringify(await paintRoof(d)));
  for (const m of d.getRoot().listMaterials()) {
    const n = m.getName();
    if (n === 'body') { m.setMetallicFactor(0); m.setRoughnessFactor(0.5); m.setMetallicRoughnessTexture(null); m.setNormalTexture(null); }
    if (n === 'glass') { m.setRoughnessFactor(0.35); m.setBaseColorFactor([0.05, 0.06, 0.07, 1]); }
    const base = m.getBaseColorTexture();
    if (base && (n === 'body' || n === 'wheel')) {
      await sharp(Buffer.from(base.getImage())).jpeg({ quality: 88 }).toFile(`${tex}/${k}_${n}.jpg`);
      m.setBaseColorTexture(null);
    }
    m.setEmissiveTexture(null); m.setOcclusionTexture(null);
  }
  await d.transform(prune(keep));
  layGlassUVs(d);
  const far = cloneDocument(d);
  await far.transform(weld(), simplify({ simplifier: MeshoptSimplifier, ratio: 0.25, error: 0.02 }), prune(keep));
  const full = tris(d);
  if (LETTERING[k]) {
    const wz = wheelZ(d), zs = LETTERING[k].z || [wz.rear + 0.5, wz.front - 0.5];
    const rect = { z0: zs[0], z1: zs[1], y0: 0.5, y1: glassBelt(d) - 0.05 };
    await letteringPng(`${tex}/${k}_decal.png`, LETTERING[k], (rect.z1 - rect.z0) / (rect.y1 - rect.y0));
    console.log('  lettering', k, JSON.stringify(rect), 'near tris', addLettering(d, rect), 'far tris', addLettering(far, rect));
  }
  await io.write(`${out}/${k}.glb`, d);
  await io.write(`${out}/LOD1/${k}.glb`, far);
  console.log(k, full, '->', tris(far), Math.round(fs.statSync(`${out}/${k}.glb`).size / 1024), 'KB', Math.round(fs.statSync(`${out}/LOD1/${k}.glb`).size / 1024), 'KB');
}
