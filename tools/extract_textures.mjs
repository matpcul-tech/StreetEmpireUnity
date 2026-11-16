// Pull base color and normal textures out of a character GLB as 1024 px JPEGs (for the Unity materials).
// Usage: node extract_textures.mjs <in.glb> <out prefix>  -> <prefix>_basecolor.jpg, <prefix>_normal.jpg
import { NodeIO } from '@gltf-transform/core';
import sharp from 'sharp';
const [src, out] = process.argv.slice(2);
const doc = await new NodeIO().read(src);
const m = doc.getRoot().listMaterials()[0];
for (const [tex, name] of [[m.getBaseColorTexture(), 'basecolor'], [m.getNormalTexture(), 'normal']]) {
  if (!tex) { console.log('no', name); continue; }
  await sharp(Buffer.from(tex.getImage())).resize(1024, 1024, { fit: 'inside' }).jpeg({ quality: 88 }).toFile(`${out}_${name}.jpg`);
  console.log('wrote', `${out}_${name}.jpg`);
}
