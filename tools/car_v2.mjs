// Cars v2 (release meshy-cars-v2-2026-11-22) for Unity: body paint to a plain semi-gloss (the game's material
// converter does not read the metal map, and a metal factor of 1 without it turns the whole car chrome), glass a
// touch rougher and off pure black so it does not flash white in the sun, PNG maps to JPEG, and a far version
// (about a quarter of the triangles, same maps and UVs). Run from a folder with @gltf-transform/* , meshoptimizer, sharp.
// Usage: node carv2.mjs <src dir> <out dir> <kind>...
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { weld, simplify, prune } from '@gltf-transform/functions'; import { MeshoptSimplifier } from 'meshoptimizer';
import sharp from 'sharp'; import fs from 'node:fs';
await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const [src, out, ...kinds] = process.argv.slice(2);
fs.mkdirSync(`${out}/LOD1`, { recursive: true });
const tris = (d) => { let t = 0; for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) t += p.getIndices().getCount() / 3; return t; };
for (const k of kinds) {
  const d = await io.read(`${src}/${k}.glb`);
  for (const m of d.getRoot().listMaterials()) {
    if (m.getName() === 'body') { m.setMetallicFactor(0); m.setRoughnessFactor(0.5); m.setMetallicRoughnessTexture(null); }
    if (m.getName() === 'glass') { m.setRoughnessFactor(0.35); m.setBaseColorFactor([0.05, 0.06, 0.07, 1]); }
  }
  await d.transform(prune());
  for (const t of d.getRoot().listTextures()) {
    t.setImage(new Uint8Array(await sharp(Buffer.from(t.getImage())).jpeg({ quality: 86 }).toBuffer())); t.setMimeType('image/jpeg');
  }
  await io.write(`${out}/${k}.glb`, d);
  const full = tris(d);
  await d.transform(weld(), simplify({ simplifier: MeshoptSimplifier, ratio: 0.25, error: 0.02 }), prune());
  await io.write(`${out}/LOD1/${k}.glb`, d);
  console.log(k, full, '->', tris(d), Math.round(fs.statSync(`${out}/${k}.glb`).size / 1024), 'KB', Math.round(fs.statSync(`${out}/LOD1/${k}.glb`).size / 1024), 'KB', d.getRoot().listTextures().length, 'maps');
}
