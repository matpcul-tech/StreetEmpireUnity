// Flatten walls build (Sovereign-draft branch claude/asset-flatten-walls 9ad9173, served at SA_URL). Runs one model through Sovereign Asset (the real app, /asset.html, served by vite preview) the same way
// scripts/asset-browser.mjs does, with the settings panel filled in: triangle target, largest texture,
// texture format, size, compression. Writes the finished GLB, the app's report and a verdict line.
//   node sa-finish.mjs <in.glb> <out dir> <preset> <tris> <maxTex> [axis meters]
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { basename } from "node:path";
import { chromium } from "/workspace/sovereign-draft-ro/node_modules/playwright-core/index.mjs";
const [file, out, preset = "game", tris = "", tex = "1024", axis = "", meters = ""] = process.argv.slice(2);
mkdirSync(out, { recursive: true });
const name = basename(file).replace(/\.glb$/, "");
const browser = await chromium.launch({ executablePath: "/opt/google/chrome/chrome", headless: true, args: ["--no-sandbox", "--disable-dev-shm-usage", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
const page = await (await browser.newContext({ viewport: { width: 1280, height: 900 }, acceptDownloads: true })).newPage();
const errors = [];
page.on("pageerror", (e) => errors.push(e.message));
await page.goto((process.env.SA_URL || "http://127.0.0.1:4175/asset.html") + "", { waitUntil: "load" });
await page.setInputFiles("#file", [file]);
await page.waitForSelector("#tiles .tile", { timeout: 180000 });
const found = (await page.locator("#issues").innerText()).replace(/\s+/g, " ");
await page.click(`.preset[data-id="${preset}"]`);
await page.evaluate(() => { const d = document.querySelector("#more"); if (d) d.open = true; });
await page.fill("#o-tris", String(tris));
await page.selectOption("#o-tex", String(tex));
await page.selectOption("#o-fmt", "keep");
await page.selectOption("#o-zip", "off");
if (axis) { await page.selectOption("#o-axis", axis); await page.fill("#o-m", String(meters)); }
await page.check("#o-ground"); await page.check("#o-center");
if (process.env.FLAT) await page.selectOption("#o-flat", process.env.FLAT);
const flat = (await page.locator("#o-flat").count()) ? await page.locator("#o-flat").inputValue() : "none";
if (process.env.FIXNORMALS) { await page.check("#o-normals"); await page.check("#o-facing"); }
await page.click("#finish");
await page.waitForSelector("#verdict .banner", { timeout: 900000 });
const banner = await page.locator("#verdict .banner").innerText();
const sum = (await page.locator("#verdict .sum").innerText()).replace(/\s+/g, " ");
const checks = [];
for (const row of await page.locator("#verdict .checks li").all()) checks.push((await row.innerText()).replace(/\s+/g, " "));
const r = await page.evaluate(() => { const r = window.sovereignAsset.state.result; return { ready: r.ready, tris: r.after && r.after.triangles, before: r.before && r.before.triangles }; });
let saved = "";
if (await page.locator("#dl").count()) {
  const wait = page.waitForEvent("download"); await page.click("#dl"); const d = await wait;
  saved = `${out}/${name}.glb`; writeFileSync(saved, readFileSync(await d.path()));
}
if (await page.locator("#dlreport").count()) {
  const wait = page.waitForEvent("download"); await page.click("#dlreport"); const d = await wait;
  writeFileSync(`${out}/${name}.report.txt`, readFileSync(await d.path()));
}
await page.screenshot({ path: `${out}/${name}.verdict.png`, fullPage: true });
const line = { name, banner, preset, flatten: flat, tris_before: r.before, tris_after: r.tris, maxTex: tex, sum, failed: checks.filter((c) => !c.startsWith("PASS")), found, errors };
writeFileSync(`${out}/${name}.verdict.json`, JSON.stringify(line, null, 1));
console.log(`${name}: ${banner} ${r.before} -> ${r.tris} tris. ${sum}`);
for (const c of line.failed) console.log("   " + c.slice(0, 220));
await browser.close();
