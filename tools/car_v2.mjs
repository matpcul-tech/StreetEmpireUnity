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
import { weld, simplify, prune } from '@gltf-transform/functions'; import { MeshoptSimplifier } from 'meshoptimizer';
import sharp from 'sharp'; import fs from 'node:fs';
await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const [src, out, tex, ...kinds] = process.argv.slice(2);
fs.mkdirSync(`${out}/LOD1`, { recursive: true }); fs.mkdirSync(tex, { recursive: true });
const tris = (d) => { let t = 0; for (const m of d.getRoot().listMeshes()) for (const p of m.listPrimitives()) t += p.getIndices().getCount() / 3; return t; };
const keep = { keepAttributes: true };   // the UVs stay although the GLB no longer names a texture
for (const k of kinds) {
  const d = await io.read(`${src}/${k}.glb`);
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
  await io.write(`${out}/${k}.glb`, d);
  const full = tris(d);
  await d.transform(weld(), simplify({ simplifier: MeshoptSimplifier, ratio: 0.25, error: 0.02 }), prune(keep));
  await io.write(`${out}/LOD1/${k}.glb`, d);
  console.log(k, full, '->', tris(d), Math.round(fs.statSync(`${out}/${k}.glb`).size / 1024), 'KB', Math.round(fs.statSync(`${out}/LOD1/${k}.glb`).size / 1024), 'KB', d.getRoot().listTextures().length, 'images left');
}
