// v75c: lower the contrast of a far skyline model's base color toward the day haze (Matthew: flat gray boxes)
//   node haze_tex.mjs <in.glb> <out.glb> <amount 0..1> <r,g,b haze sRGB>
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions'; import sharp from 'sharp';
const [src, out, amt, hz] = process.argv.slice(2); const k = +amt; const H = hz.split(',').map(Number);
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS); const doc = await io.read(src);
for (const m of doc.getRoot().listMaterials()) {
  const t = m.getBaseColorTexture(); if (!t) continue;
  const { data, info } = await sharp(Buffer.from(t.getImage())).raw().toBuffer({ resolveWithObject: true });
  for (let i = 0; i < data.length; i += info.channels) for (let c = 0; c < 3; c++) data[i + c] = Math.round(data[i + c] * (1 - k) + H[c] * k);
  const img = await sharp(data, { raw: info }).jpeg({ quality: 92 }).toBuffer();
  t.setImage(new Uint8Array(img)).setMimeType('image/jpeg'); console.log('hazed', m.getName(), info.width, info.height);
}
await io.write(out, doc);
