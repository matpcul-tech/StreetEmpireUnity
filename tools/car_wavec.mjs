// Vehicles wave C (release meshy-vehicles-wave-c-2026-11-23) for Unity. They arrive in real meters, nose along +Z,
// with named materials (paint, trim, marking, lights share one color map; well, glass, wheel_rim and wheel_tire are
// flat colors) and wheel nodes at the hubs. This step only prepares them the way the v2 cars are prepared:
// - the color map goes out as a JPEG file, <textures dir>/<kind>_body.jpg, and the GLB carries no image, so Unity
//   imports it as an ordinary compressed texture shared by the near and far car
// - the glass gets the shared glass UVs (by height and length, see car_v2.mjs) for Textures/Cars/V2/glass.png
// - KHR_materials_specular is dropped (the game's material converter does not read it)
// - a far version, same UVs
// Mesh, size, wheel pivots and material names are as delivered.
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
  for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) {
    if (p.getMaterial().getName() !== 'glass') continue;
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
