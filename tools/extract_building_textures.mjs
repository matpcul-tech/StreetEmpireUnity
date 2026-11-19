// v74: pull each building material's textures out of its finished GLB (Sovereign Asset "Unity building", four
// materials kept apart) so Unity compresses them (ASTC on Android, DXT on WebGL; see ModelImportRules) instead of
// glTFast's uncompressed copies. Per material part (Material_0, walls; the window glass has no textures):
//   <name>__<part>_basecolor.jpg, <name>__<part>_normal.jpg, and <name>__<part>_metalsmooth.png (the glTF
//   metallic-roughness or roughness map rewritten for URP: R metal 0, A smoothness = 1 - roughness).
// Old single atlas files (<name>_basecolor.jpg, _normal, _emission, _metalsmooth) are deleted: the emission mask
// system is gone. Run from a folder with @gltf-transform/core, extensions and sharp:
//   node extract_building_textures.mjs <repo>/Assets/Art/Models/Buildings <repo>/Assets/Art/Textures/Buildings
import fs from 'node:fs';
import path from 'node:path';
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import sharp from 'sharp';
const [dir, texDir, only] = process.argv.slice(2);   // v75g: optional third argument, one model name
fs.mkdirSync(texDir, { recursive: true });
if (!only) for (const f of fs.readdirSync(texDir)) if (f !== 'window-warm_emission.png' && /^[a-z0-9-]+_(basecolor|normal|emission|metalsmooth)\.(jpg|png)$/.test(f)) fs.rmSync(path.join(texDir, f));
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const part = (m) => { const n = m.getName(); return n.includes('windows_lit') ? 'windows_lit' : n.includes('windows_dark') ? 'windows_dark' : n.includes('walls') ? 'walls' : (n.includes('Material_0') || !n) ? 'Material_0' : n; };
for (const f of fs.readdirSync(dir).filter((f) => f.endsWith('.glb')).sort()) {
  const name = f.replace(/\.glb$/, ''); if (only && name !== only) continue;
  const doc = await io.read(path.join(dir, f));
  for (const m of doc.getRoot().listMaterials()) {
    const p = part(m); if (p.startsWith('windows') || p === 'roof') continue;   // the roof shares the walls maps
    // the walls carry the clean wall color at 2048; normals, roughness, trim, roof, frames and doors at 1024
    // v75g: the white house's walls keep their normal and roughness at 2048 too (the stucco is in its normal map)
    const max = p === 'walls' ? 2048 : 1024, nmax = p === 'walls' && name === 'house-01-bungalow' ? 2048 : 1024, base = path.join(texDir, `${name}__${p}`);
    const img = (t, sz) => sharp(Buffer.from(t.getImage())).resize(sz, sz, { fit: 'inside', withoutEnlargement: true });
    const bc = m.getBaseColorTexture(); if (bc) { await img(bc, max).jpeg({ quality: 90 }).toFile(`${base}_basecolor.jpg`); console.log(name, p, 'basecolor'); }
    const nm = m.getNormalTexture(); if (nm) { await img(nm, nmax).jpeg({ quality: 92 }).toFile(`${base}_normal.jpg`); console.log(name, p, 'normal'); }
    const mr = m.getMetallicRoughnessTexture();
    if (mr) {
      const { data, info } = await img(mr, nmax).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
      const out = Buffer.alloc(info.width * info.height * 4);
      for (let i = 0; i < info.width * info.height; i++) { out[i * 4] = 0; out[i * 4 + 1] = 0; out[i * 4 + 2] = 0; out[i * 4 + 3] = 255 - data[i * 4 + 1]; }
      await sharp(out, { raw: { width: info.width, height: info.height, channels: 4 } }).png().toFile(`${base}_metalsmooth.png`); console.log(name, p, 'metalsmooth');
    }
  }
}
