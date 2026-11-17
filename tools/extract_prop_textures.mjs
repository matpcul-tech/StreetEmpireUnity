// Pull every prop material's textures out of the Sovereign Asset finished GLBs so Unity compresses them
// (ASTC Android, DXT WebGL; see ModelImportRules) instead of glTFast's uncompressed copies, and write a
// manifest PropBuilder uses to make the URP Lit materials: base color (JPEG, PNG where alpha is used),
// normal (JPEG), and the glTF metallic-roughness map (B metal, G roughness) rewritten as a URP
// metallic-smoothness map (R metal, A smoothness). Hero props seen up close keep 2048 px ("_2k" in the name).
// Run from a folder with @gltf-transform/core, extensions and sharp:
//   node extract_prop_textures.mjs <repo>/Assets/Art/Models/Props <repo>/Assets/Art/Textures/Props
import fs from 'node:fs';
import path from 'node:path';
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import sharp from 'sharp';
const HERO = new Set(['hydrant', 'hydrant-aged', 'bench', 'trash-can', 'street-lamp-1']);
const [dir, texDir] = process.argv.slice(2);
fs.mkdirSync(texDir, { recursive: true });
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const safe = (s) => s.replace(/[^A-Za-z0-9_-]/g, '_');
const manifest = [];
for (const f of fs.readdirSync(dir).filter((f) => f.endsWith('.glb')).sort()) {
  const prop = f.replace(/\.glb$/, '');
  const size = HERO.has(prop) ? 2048 : 1024;
  const tag = HERO.has(prop) ? '_2k' : '';
  for (const m of (await io.read(path.join(dir, f))).getRoot().listMaterials()) {
    const base = `${prop}__${safe(m.getName())}${tag}`;
    const img = (t) => sharp(Buffer.from(t.getImage())).resize(size, size, { fit: 'inside', withoutEnlargement: true });
    const e = { prop, material: m.getName(), alpha: m.getAlphaMode(), cutoff: m.getAlphaCutoff(), color: m.getBaseColorFactor(),
      metallic: m.getMetallicFactor(), roughness: m.getRoughnessFactor(), tiling: [1, 1] };
    const tt = m.getBaseColorTextureInfo()?.getExtension('KHR_texture_transform');
    if (tt) e.tiling = tt.getScale();
    const bc = m.getBaseColorTexture();
    if (bc) {
      const alpha = m.getAlphaMode() !== 'OPAQUE';
      e.basecolor = `${base}_basecolor.${alpha ? 'png' : 'jpg'}`;
      const p = img(bc); await (alpha ? p.png() : p.jpeg({ quality: 88 })).toFile(path.join(texDir, e.basecolor));
    }
    if (m.getNormalTexture()) { e.normal = `${base}_normal.jpg`; await img(m.getNormalTexture()).jpeg({ quality: 90 }).toFile(path.join(texDir, e.normal)); }
    const mr = m.getMetallicRoughnessTexture();
    if (mr) {
      const { data, info } = await img(mr).removeAlpha().raw().toBuffer({ resolveWithObject: true });
      const out = Buffer.alloc(info.width * info.height * 4);
      for (let i = 0; i < info.width * info.height; i++) {
        out[i * 4] = Math.round(data[i * 3 + 2] * e.metallic);
        out[i * 4 + 3] = 255 - Math.min(255, Math.round(data[i * 3 + 1] * e.roughness));
      }
      e.metalsmooth = `${base}_metalsmooth.png`;
      await sharp(out, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toFile(path.join(texDir, e.metalsmooth));
    }
    manifest.push(e);
  }
  console.log(prop);
}
fs.writeFileSync(path.join(texDir, 'manifest.json'), JSON.stringify({ materials: manifest }, null, 1) + '\n');
