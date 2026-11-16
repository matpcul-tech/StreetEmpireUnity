// Headless screenshots of the WebGL build with software rendering (SwiftShader). Box tool, no GPU needed.
// Usage: node capture_webgl.mjs <build dir with index.html> <out dir> [--no3d] [--keys=w:2000,shift+w:1500] [--query=shot=job]
import http from 'http'; import fs from 'fs'; import path from 'path';
import puppeteer from 'puppeteer-core';
const [dir, out, ...flags] = process.argv.slice(2);
const no3d = flags.includes('--no3d');
const query = (flags.find((f) => f.startsWith('--query=')) || '').slice(8);
const keys = (flags.find((f) => f.startsWith('--keys=')) || '').slice(7).split(',').filter(Boolean);
fs.mkdirSync(out, { recursive: true });
const types = { '.html': 'text/html', '.js': 'application/javascript', '.wasm': 'application/wasm', '.data': 'application/octet-stream', '.unityweb': 'application/octet-stream' };
const server = http.createServer((req, res) => {
  const p = path.join(dir, decodeURIComponent(req.url.split('?')[0]).replace(/^\/$/, '/index.html'));
  fs.readFile(p, (e, b) => { if (e) { res.writeHead(404); res.end(); return; } res.writeHead(200, { 'Content-Type': types[path.extname(p)] || 'application/octet-stream' }); res.end(b); });
}).listen(8765);
const args = ['--no-sandbox', '--window-size=1280,720', '--ignore-gpu-blocklist'];
args.push(...(no3d ? ['--disable-webgl', '--disable-webgl2', '--disable-3d-apis'] : ['--use-angle=swiftshader', '--enable-unsafe-swiftshader']));
const browser = await puppeteer.launch({ executablePath: '/usr/bin/google-chrome', headless: 'new', args, protocolTimeout: 600000 });
const page = await browser.newPage();
await page.setViewport({ width: 1280, height: 720 });
const logs = []; page.on('console', (m) => logs.push(m.type() + ': ' + m.text()));
await page.goto('http://localhost:8765/' + (query ? '?' + query : ''), { waitUntil: 'load', timeout: 120000 });
const t0 = Date.now();
await page.waitForFunction(() => window.seStatus && window.seStatus !== 'loading', { timeout: 420000, polling: 1000 });
const status = await page.evaluate(() => window.seStatus);
console.log('status', status, 'after', (Date.now() - t0) / 1000, 's');
if (status === 'running') {
  await new Promise((r) => setTimeout(r, 15000));
  await page.screenshot({ path: path.join(out, 'start.png') });
  await page.mouse.click(640, 360);
  for (const [i, k] of keys.entries()) {
    const [combo, ms] = k.split(':'); const parts = combo.split('+').map((x) => ({ w: 'KeyW', a: 'KeyA', s: 'KeyS', d: 'KeyD', shift: 'ShiftLeft', space: 'Space', e: 'KeyE' }[x] || x));
    for (const p of parts) await page.keyboard.down(p);
    await new Promise((r) => setTimeout(r, Number(ms) * 0.7));
    await page.screenshot({ path: path.join(out, `keys-${i + 1}-${combo}.png`) });
    await new Promise((r) => setTimeout(r, Number(ms) * 0.3));
    for (const p of parts.reverse()) await page.keyboard.up(p);
  }
} else {
  await page.screenshot({ path: path.join(out, 'no3d.png') });
}
fs.writeFileSync(path.join(out, 'console.txt'), logs.join('\n'));
await browser.close(); server.close();
