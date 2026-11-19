// Sovereign-draft main d9dcc22 (local build of /workspace/sa-grayblock/sa-main in /tmp/sa-main-dist, served at SA_URL, default 4176; checks only, its output is not used for Matthew's finished files): one model through Sovereign Asset /asset.html with the
// "Unity building or prop" (unity-static) or "Unity character" (unity-character) preset as it stands, geometry
// compression off. Optional env: FLAT=light|strong|off (Flatten walls), FIXNORMALS=1, TRIS=<target>, TEX=<px>.
// Writes the finished GLB, the app's report, a verdict screenshot and <name>.verdict.json with every check line,
// the Texture detail number (texture pixels per meter) and the Color standard result.
//   node sa-unity.mjs <in.glb> <out dir> <preset>
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { basename } from "node:path";
import { chromium } from "/workspace/sovereign-draft-ro/node_modules/playwright-core/index.mjs";
const [file, out, preset = "unity-static"] = process.argv.slice(2);
mkdirSync(out, { recursive: true });
const name = basename(file).replace(/\.glb$/, "");
const browser = await chromium.launch({ executablePath: "/opt/google/chrome/chrome", headless: true, args: ["--no-sandbox", "--disable-dev-shm-usage", "--use-angle=swiftshader", "--enable-unsafe-swiftshader"] });
const ctx = await browser.newContext({ viewport: { width: 1280, height: 900 }, acceptDownloads: true, serviceWorkers: "block" });
const page = await ctx.newPage();
const errors = [];
page.on("pageerror", (e) => errors.push(e.message));
await page.goto(process.env.SA_URL || "http://127.0.0.1:4176/asset.html", { waitUntil: "load" });
await page.setInputFiles("#file", [file]);
await page.waitForSelector("#tiles .tile", { timeout: 300000 });
const found = (await page.locator("#issues").innerText()).replace(/\s+/g, " ");
await page.click(`.preset[data-id="${preset}"]`);
await page.evaluate(() => { const d = document.querySelector("#more"); if (d) d.open = true; });
if (process.env.TRIS) await page.fill("#o-tris", process.env.TRIS);
if (process.env.TEX) await page.selectOption("#o-tex", process.env.TEX);
await page.selectOption("#o-zip", "off");
if (process.env.FLAT) await page.selectOption("#o-flat", process.env.FLAT);
if (process.env.FIXNORMALS) { await page.check("#o-normals"); await page.check("#o-facing"); }
const opts = await page.evaluate(() => ({ flat: document.querySelector("#o-flat")?.value, light: document.querySelector("#o-light")?.value, maps: document.querySelector("#o-maps")?.value, color: document.querySelector("#o-color")?.checked, tex: document.querySelector("#o-tex")?.value, tris: document.querySelector("#o-tris")?.value }));
await page.click("#finish");
await page.waitForSelector("#verdict .banner", { timeout: 1500000 });
const banner = await page.locator("#verdict .banner").innerText();
const sum = (await page.locator("#verdict .sum").innerText()).replace(/\s+/g, " ");
const checks = [];
for (const row of await page.locator("#verdict .checks li").all()) checks.push((await row.innerText()).replace(/\s+/g, " "));
const r = await page.evaluate(() => { const r = window.sovereignAsset.state.result; const a = r.after || {}; return { ready: r.ready, tris: a.triangles, before: r.before && r.before.triangles, detail: a.detail || null, color: a.color || null, checks: (r.checks || []).map((c) => ({ id: c.id, status: c.status, title: c.title, text: c.text })) }; });
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
const colorLine = checks.find((c) => /Color standard/.test(c)) || "";
const detailLine = checks.find((c) => /Texture detail/.test(c)) || "";
const line = { name, banner, preset, options: opts, tris_before: r.before, tris_after: r.tris,
  texture_detail_px_per_m: r.detail ? Math.round(r.detail.median) : null, stretched_pct: r.detail ? Math.round(r.detail.stretched * 100) : null,
  color_standard: colorLine.startsWith("PASS") ? "PASS" : colorLine ? colorLine.split(" ")[0] : "none", color_line: colorLine, detail_line: detailLine,
  sum, checks, failed: checks.filter((c) => !c.startsWith("PASS") && !c.startsWith("NOTE")), found, errors };
writeFileSync(`${out}/${name}.verdict.json`, JSON.stringify(line, null, 1));
console.log(`${name}: ${banner} detail ${line.texture_detail_px_per_m} px/m stretched ${line.stretched_pct}% color ${line.color_standard} ${r.before}->${r.tris} tris`);
for (const c of line.failed) console.log("   " + c.slice(0, 220));
await browser.close();
