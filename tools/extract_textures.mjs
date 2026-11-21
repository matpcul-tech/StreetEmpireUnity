// Pull base color, normal, and metallic-roughness textures out of a character GLB.
// Base color and normal are 1024 px JPEGs. The glTF metallic-roughness map
// (B metal, G roughness) is rewritten as a URP metallic-smoothness PNG
// (R metal, A smoothness). JPEG cannot hold smoothness in alpha.
// Usage: node extract_textures.mjs <in.glb> <out prefix>
//   -> <prefix>_basecolor.jpg, <prefix>_normal.jpg, <prefix>_metalsmooth.png
import { NodeIO } from '@gltf-transform/core';
import sharp from 'sharp';
const [src, out] = process.argv.slice(2);
const doc = await new NodeIO().read(src);
const m = doc.getRoot().listMaterials()[0];
const img = (t) => sharp(Buffer.from(t.getImage())).resize(1024, 1024, { fit: 'inside', withoutEnlargement: true });
if (m.getBaseColorTexture()) {
  await img(m.getBaseColorTexture()).jpeg({ quality: 88 }).toFile(`${out}_basecolor.jpg`);
  console.log('wrote', `${out}_basecolor.jpg`);
} else console.log('no', 'basecolor');
if (m.getNormalTexture()) {
  await img(m.getNormalTexture()).jpeg({ quality: 88 }).toFile(`${out}_normal.jpg`);
  console.log('wrote', `${out}_normal.jpg`);
} else console.log('no', 'normal');
const mr = m.getMetallicRoughnessTexture();
if (mr) {
  const { data, info } = await img(mr).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const packed = Buffer.alloc(info.width * info.height * 4);
  const mf = m.getMetallicFactor(), rf = m.getRoughnessFactor();
  for (let i = 0; i < info.width * info.height; i++) {
    packed[i * 4] = Math.round(data[i * 3 + 2] * mf);
    packed[i * 4 + 1] = packed[i * 4 + 2] = 0;
    packed[i * 4 + 3] = 255 - Math.round(data[i * 3 + 1] * rf);
  }
  await sharp(packed, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toFile(`${out}_metalsmooth.png`);
  console.log('wrote', `${out}_metalsmooth.png`);
} else console.log('no', 'metalsmooth');
