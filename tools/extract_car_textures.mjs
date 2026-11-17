// Pull a kit car's textures out of its GLB so Unity compresses them (ASTC Android, DXT WebGL) instead of
// using glTFast's uncompressed copies: base color and emissive as JPEG, and the glTF metallic-roughness map
// (B metal, G roughness) rewritten as a URP metallic-smoothness map (R metal, A smoothness) PNG.
// Run from a folder with @gltf-transform/core and sharp: node extract_car_textures.mjs <car.glb> <out dir> <name>
import fs from 'node:fs';
import path from 'node:path';
import { NodeIO } from '@gltf-transform/core';
import sharp from 'sharp';
const [src, dir, name] = process.argv.slice(2);
fs.mkdirSync(dir, { recursive: true });
const m = (await new NodeIO().read(src)).getRoot().listMaterials()[0];
const img = (t) => sharp(Buffer.from(t.getImage())).resize(1024, 1024, { fit: 'inside', withoutEnlargement: true });
if (m.getBaseColorTexture()) await img(m.getBaseColorTexture()).jpeg({ quality: 88 }).toFile(path.join(dir, `${name}_basecolor.jpg`));
if (m.getEmissiveTexture() && Math.max(...m.getEmissiveFactor()) > 0) await img(m.getEmissiveTexture()).jpeg({ quality: 88 }).toFile(path.join(dir, `${name}_emissive.jpg`));
const mr = m.getMetallicRoughnessTexture();
if (mr) {
  const { data, info } = await img(mr).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const out = Buffer.alloc(info.width * info.height * 4);
  const mf = m.getMetallicFactor(), rf = m.getRoughnessFactor();
  for (let i = 0; i < info.width * info.height; i++) {
    out[i * 4] = Math.round(data[i * 3 + 2] * mf);
    out[i * 4 + 1] = out[i * 4 + 2] = 0;
    out[i * 4 + 3] = 255 - Math.round(data[i * 3 + 1] * rf);
  }
  await sharp(out, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toFile(path.join(dir, `${name}_metalsmooth.png`));
}
console.log('done', name, fs.readdirSync(dir).filter((f) => f.startsWith(name)));
