// Run from a folder with @gltf-transform/core, extensions, functions, meshoptimizer and sharp installed.
// GLB for Unity glTFast: decode meshopt, dequantize, turn WebP textures into PNG. Usage: node convert.mjs in.glb out.glb
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import { dequantize, textureCompress, prune } from '@gltf-transform/functions';
import { MeshoptDecoder } from 'meshoptimizer';
import sharp from 'sharp';
await MeshoptDecoder.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({ 'meshopt.decoder': MeshoptDecoder });
const [inp, out] = process.argv.slice(2);
const doc = await io.read(inp);
await doc.transform(dequantize(), textureCompress({ encoder: sharp, targetFormat: 'png' }), prune());
// opaque textures to JPEG to keep the repo small; keep PNG where there is real alpha
for (const t of doc.getRoot().listTextures()) {
  const img = sharp(Buffer.from(t.getImage())); const st = await img.stats();
  if (st.isOpaque) { t.setImage(new Uint8Array(await sharp(Buffer.from(t.getImage())).jpeg({ quality: 88 }).toBuffer())); t.setMimeType('image/jpeg'); if (t.getURI()) t.setURI(t.getURI().replace(/\.png$/, '.jpg')); }
}
for (const e of doc.getRoot().listExtensionsUsed()) if (['EXT_meshopt_compression', 'EXT_texture_webp', 'KHR_mesh_quantization'].includes(e.extensionName)) e.dispose();
await io.write(out, doc);
const j = (await io.readAsJSON(out)).json;
console.log(out, 'ext', j.extensionsUsed || [], 'img', (j.images || []).map((i) => i.mimeType).join(','));
