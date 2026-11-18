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
  // window glass and lit rooms (tools/window_maps.mjs): emissive as JPEG, glTF metallic-roughness rewritten as a
  // URP metallic-smoothness map (R metal from B, A smoothness from 1 - G)
  const em = m.getEmissiveTexture(), mr = m.getMetallicRoughnessTexture();
  if (em) { await sharp(Buffer.from(em.getImage())).removeAlpha().resize(1024, 1024, { fit: 'inside', withoutEnlargement: true }).jpeg({ quality: 90 }).toFile(path.join(texDir, `${name}_emission.jpg`)); console.log(name, 'emission'); }
  if (mr) {
    const { data, info } = await sharp(Buffer.from(mr.getImage())).removeAlpha().raw().toBuffer({ resolveWithObject: true });
    const o = Buffer.alloc(info.width * info.height * 4);
    for (let i = 0; i < info.width * info.height; i++) { o[i * 4] = data[i * 3 + 2]; o[i * 4 + 1] = 0; o[i * 4 + 2] = 0; o[i * 4 + 3] = 255 - data[i * 3 + 1]; }
    await sharp(o, { raw: { width: info.width, height: info.height, channels: 4 } }).resize(1024, 1024, { fit: 'inside', withoutEnlargement: true }).png().toFile(path.join(texDir, `${name}_metalsmooth.png`));
    console.log(name, 'metalsmooth');
  }
}
