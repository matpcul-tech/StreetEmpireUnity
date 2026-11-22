// The fixes applied to a v3 NPC before it is converted for the game:
//   base color: the graded skin texture, when there is one for this character (fix/<name>_basecolor.png)
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions'; import sharp from 'sharp'; import fs from 'node:fs';
const [n] = process.argv.slice(2);
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS); const doc = await io.read(`npc/stage/${n}.glb`);
const prim = doc.getRoot().listMeshes()[0].listPrimitives()[0]; const mat = prim.getMaterial(); const notes = [];
const fx = `npc/fix/${n}_basecolor.png`;
if (fs.existsSync(fx)) { mat.getBaseColorTexture().setImage(fs.readFileSync(fx)).setMimeType('image/png'); notes.push('skin graded'); }
if (false) {   // tried and dropped: with the hair on Spine2 the shirt pokes through it when the shoulders lift; the delivered weights are better
  const skin = doc.getRoot().listSkins()[0]; const joints = skin.listJoints().map(j => j.getName());
  const sp = joints.indexOf('mixamorig:Spine2'), sh = [joints.indexOf('mixamorig:LeftShoulder'), joints.indexOf('mixamorig:RightShoulder')];
  const { data, info } = await sharp(Buffer.from(mat.getBaseColorTexture().getImage())).removeAlpha().raw().toBuffer({ resolveWithObject: true });
  const P = prim.getAttribute('POSITION').getArray(), UV = prim.getAttribute('TEXCOORD_0').getArray(), JA = prim.getAttribute('JOINTS_0'), WA = prim.getAttribute('WEIGHTS_0');
  const J = JA.getArray().slice(), Wt = WA.getArray().slice(); const nv = JA.getCount(); let moved = 0;
  const wmax = WA.getNormalized() ? (Wt instanceof Uint8Array ? 255 : 65535) : 1;
  for (let i = 0; i < nv; i++) {
    if (P[3 * i + 1] < 1.2) continue;
    const x = Math.min(info.width - 1, Math.floor(UV[2 * i] * info.width)), y = Math.min(info.height - 1, Math.floor(UV[2 * i + 1] * info.height)); const o = (y * info.width + x) * 3;
    if (Math.max(data[o], data[o + 1], data[o + 2]) > 72) continue;   // hair is the dark part; her shirt is white and her skin light
    let take = 0; for (let k = 0; k < 4; k++) if (sh.includes(J[4 * i + k]) && Wt[4 * i + k] > 0) { take += Wt[4 * i + k]; Wt[4 * i + k] = 0; }
    if (!take) continue;
    let at = -1; for (let k = 0; k < 4; k++) if (J[4 * i + k] === sp && Wt[4 * i + k] > 0) at = k;
    if (at < 0) for (let k = 0; k < 4; k++) if (Wt[4 * i + k] === 0) { at = k; J[4 * i + k] = sp; break; }
    Wt[4 * i + at] += take; moved++;
  }
  JA.setArray(J); WA.setArray(Wt); notes.push(`hair: ${moved} vertices moved from the shoulder bones to Spine2`);
}
await io.write(`npc/final/${n}.glb`, doc); console.log(n, notes.join('; ') || 'as delivered', fs.statSync(`npc/final/${n}.glb`).size);
