// The finished buildings' window and sign materials (windows_lit, windows_dark, sign_lit) kept the color and glow
// maps embedded in their GLB. Unity puts an embedded map in the build uncompressed: 4 MB a building for the two
// 1024 x 512 window maps (build 916031a: 66 MB of GLB in the APK's assets). This writes them out as files,
// <textures dir>/<name>__<part>_basecolor.jpg and <name>__<part>_emission.jpg, which import GPU compressed like
// every other building map; SceneBuilder.BindTextures binds them. The GLBs are not changed.
// Run from a folder with @gltf-transform/* and sharp. Usage: node extract_window_maps.mjs <models dir> <textures dir> <name>...
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions'; import sharp from 'sharp';
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const [models, tex, ...names] = process.argv.slice(2);
for (const name of names) {
  const d = await io.read(`${models}/${name}.glb`);
  for (const m of d.getRoot().listMaterials()) {
    const part = m.getName(); if (!['windows_lit', 'windows_dark', 'sign_lit'].includes(part)) continue;
    for (const [slot, t] of [['basecolor', m.getBaseColorTexture()], ['emission', m.getEmissiveTexture()]]) {
      if (!t) continue;
      await sharp(Buffer.from(t.getImage())).removeAlpha().jpeg({ quality: 92 }).toFile(`${tex}/${name}__${part}_${slot}.jpg`);
      console.log(name, part, slot, t.getSize().join('x'));
    }
    for (const [slot, t] of [['normal', m.getNormalTexture()], ['metalRough', m.getMetallicRoughnessTexture()], ['occlusion', m.getOcclusionTexture()]]) if (t) console.log('  note:', name, part, 'also has', slot, t.getSize().join('x'));
  }
}
