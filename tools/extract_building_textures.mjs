// Pull each kit building's base color and normal textures out of its GLB as 1024 px JPEG files, so Unity
// imports them with its own texture compression (ASTC on Android, DXT on WebGL; see ModelImportRules)
// instead of glTFast's uncompressed copies. SceneBuilder points the building materials at these files.
// Run from a folder with @gltf-transform/core and sharp installed:
//   node extract_building_textures.mjs <repo>/Assets/Art/Models/Buildings <repo>/Assets/Art/Textures/Buildings
import fs from 'node:fs';
import path from 'node:path';
import { NodeIO } from '@gltf-transform/core';
import sharp from 'sharp';
const [dir, texDir] = process.argv.slice(2);
fs.mkdirSync(texDir, { recursive: true });
for (const f of fs.readdirSync(dir).filter((f) => f.endsWith('.glb')).sort()) {
  const name = f.replace(/\.glb$/, '');
  const m = (await new NodeIO().read(path.join(dir, f))).getRoot().listMaterials()[0];
  for (const [slot, tex] of [['basecolor', m.getBaseColorTexture()], ['normal', m.getNormalTexture()]]) {
    if (!tex) continue;
    await sharp(Buffer.from(tex.getImage())).resize(1024, 1024, { fit: 'inside', withoutEnlargement: true }).jpeg({ quality: 85 }).toFile(path.join(texDir, `${name}_${slot}.jpg`));
    console.log(name, slot);
  }
}
