// second pass on a finished file: only turn inside-out shells and fix normals (the recipe in docs/sovereign-assets/buildings/README.md)
import { readFileSync, writeFileSync } from "node:fs";
import sharp from "sharp";
import { finishModel } from "/home/claude/matpcul-tech/sovereign-draft/src/assetapp/engine.js";
import { validate } from "/home/claude/matpcul-tech/sovereign-draft/src/assetapp/validate.js";
const codec = {
  async decode(bytes) { const { data, info } = await sharp(Buffer.from(bytes)).ensureAlpha().raw().toBuffer({ resolveWithObject: true }); return { width: info.width, height: info.height, rgba: new Uint8Array(data.buffer, data.byteOffset, data.byteLength) }; },
  async encode(rgba, width, height, mime, quality = 0.9) { const s = sharp(Buffer.from(rgba.buffer, rgba.byteOffset, rgba.byteLength), { raw: { width, height, channels: 4 } }); return new Uint8Array(mime === "image/jpeg" ? await s.removeAlpha().jpeg({ quality: Math.round(quality * 100) }).toBuffer() : await s.png().toBuffer()); }
};
const [file, name] = process.argv.slice(2);
const r = await finishModel(new Uint8Array(readFileSync(file)), { maxTexture: 2048, textureFormat: "keep", triangles: null, size: null, closeHoles: "off", removeDebris: false }, { codec, validate, onProgress: () => {} });
writeFileSync(file, r.bytes);
writeFileSync(file.replace(/\.glb$/, ".pass2.json"), JSON.stringify({ name, ready: r.ready, checks: r.checks, log: r.log, after: r.after }, (k, v) => ArrayBuffer.isView(v) ? undefined : v, 1));
console.log(`${name} pass 2: ${r.ready ? "READY" : "NOT READY"} ${r.after.triangles} tris, materials ${r.after.materials}, inverted ${r.after.defects.inverted}`);
for (const c of r.checks) if (c.status === "fail") console.log("   FAIL " + c.label + ": " + c.detail);
