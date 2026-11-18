// Cutout textures (alpha MASK or BLEND): the color under fully transparent texels is pushed out from the nearest
// opaque texels instead of staying black. Nothing visible changes at full size, but the mips no longer darken the
// leaf and blade edges, and Sovereign Asset's Color standard check no longer counts the hidden black as color.
// Fully transparent texels also get a small alpha floor (16 of 255 on cutouts, under the 0.5 cut; 6 on blended decals,
// about 2 percent): Sovereign Asset re-encodes through a premultiplied canvas, which wipes the color of texels whose
// alpha is exactly 0, and its own Color standard check then failed on its own output.
//   node alpha_bleed.mjs <in.glb> <out.glb>
import { NodeIO } from '@gltf-transform/core';
import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
import sharp from 'sharp';
const [inp, out] = process.argv.slice(2);
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
const doc = await io.read(inp); let n = 0;
for (const m of doc.getRoot().listMaterials()) {
  const t = m.getBaseColorTexture(); if (!t || m.getAlphaMode() === 'OPAQUE') continue;
  const { data, info } = await sharp(Buffer.from(t.getImage())).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
  const W = info.width, H = info.height; let known = new Uint8Array(W * H);
  for (let i = 0; i < W * H; i++) known[i] = data[i * 4 + 3] >= 8 ? 1 : 0;
  for (let pass = 0; pass < 64; pass++) {
    const next = known.slice(); let grew = 0;
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      const i = y * W + x; if (known[i]) continue;
      let r = 0, g = 0, b = 0, c = 0;
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1], [1, 1], [-1, -1], [1, -1], [-1, 1]]) {
        const xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= W || yy >= H) continue; const q = yy * W + xx; if (!known[q]) continue;
        r += data[q * 4]; g += data[q * 4 + 1]; b += data[q * 4 + 2]; c++;
      }
      if (c) { data[i * 4] = r / c; data[i * 4 + 1] = g / c; data[i * 4 + 2] = b / c; next[i] = 1; grew++; }
    }
    known = next; if (!grew) break;
  }
  const floor = m.getAlphaMode() === 'MASK' ? 16 : 6;
  for (let i = 0; i < W * H; i++) if (data[i * 4 + 3] < floor) data[i * 4 + 3] = floor;
  t.setImage(await sharp(data, { raw: { width: W, height: H, channels: 4 } }).png().toBuffer()).setMimeType('image/png'); n++;
}
await io.write(out, doc); console.log(`BLEED ${inp.split('/').pop()} ${n} cutout textures`);
