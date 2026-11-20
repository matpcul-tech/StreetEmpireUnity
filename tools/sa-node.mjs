// v75l: one building through the Sovereign Asset engine (Sovereign-draft main a642bef, src/assetapp/engine.js) in Node, no browser.
// Preset "Unity building", Size Height. Paths below are where this was run; point SA at a Sovereign-draft checkout with npm install done.
//   node sa-node.mjs <in.glb> <out dir> <height m> <triangle target>
import { readFileSync, writeFileSync, mkdirSync } from "node:fs";
import { basename } from "node:path";
import sharp from "sharp";
import { finishModel, inspectModel } from "/home/claude/matpcul-tech/sovereign-draft/src/assetapp/engine.js";
import { presetById } from "/home/claude/matpcul-tech/sovereign-draft/src/assetapp/presets.js";
import { SCANS } from "/home/claude/matpcul-tech/sovereign-draft/src/assetapp/materials.js";
import { validate } from "/home/claude/matpcul-tech/sovereign-draft/src/assetapp/validate.js";
const SA = "/home/claude/matpcul-tech/sovereign-draft";
const codec = {
  async decode(bytes) { const { data, info } = await sharp(Buffer.from(bytes)).ensureAlpha().raw().toBuffer({ resolveWithObject: true }); return { width: info.width, height: info.height, rgba: new Uint8Array(data.buffer, data.byteOffset, data.byteLength) }; },
  async encode(rgba, width, height, mime, quality = 0.9) {
    const s = sharp(Buffer.from(rgba.buffer, rgba.byteOffset, rgba.byteLength), { raw: { width, height, channels: 4 } });
    const q = Math.round(quality * 100);
    const b = mime === "image/jpeg" ? await s.removeAlpha().jpeg({ quality: q }).toBuffer() : mime === "image/webp" ? await s.webp({ quality: q }).toBuffer() : await s.png().toBuffer();
    return new Uint8Array(b);
  }
};
const cache = new Map();
const materials = async (name) => {
  if (!SCANS[name]) return null;
  if (!cache.has(name)) { const out = { meters: SCANS[name].meters }; for (const p of ["color", "normal", "roughness"]) out[p] = await codec.decode(readFileSync(`${SA}/public/asset-materials/${name}_${p}.jpg`)); cache.set(name, out); }
  return cache.get(name);
};
const [file, out, meters, tris, extra] = process.argv.slice(2);
mkdirSync(out, { recursive: true });
const name = basename(file).replace(/\.glb$/, "");
const bytes = new Uint8Array(readFileSync(file));
const options = { ...presetById("unity-building").options, size: { axis: "height", meters: +meters }, triangles: +tris || null, ...(extra ? JSON.parse(extra) : {}) };
const t0 = Date.now();
const r = await finishModel(bytes, options, { codec, materials, validate, onProgress: () => {} });
if (r.bytes) writeFileSync(`${out}/${name}.glb`, r.bytes);
const j = { name, engine: "Sovereign-draft main a642bef, engine.js in Node", preset: "unity-building", options, ready: r.ready, before: r.before, after: r.after, checks: r.checks, log: r.log };
writeFileSync(`${out}/${name}.verdict.json`, JSON.stringify(j, (k, v) => (v instanceof Uint8Array || ArrayBuffer.isView(v)) ? undefined : v, 1));
console.log(`${name}: ${r.ready ? "READY" : "NOT READY"} ${r.before?.triangles}->${r.after?.triangles} tris, ${(r.bytes?.length / 1e6).toFixed(1)} MB, ${((Date.now() - t0) / 1000).toFixed(0)}s`);
for (const c of r.checks || []) if (c.status !== "pass") console.log(`   ${c.status} ${c.title}: ${String(c.text).slice(0, 200)}`);
