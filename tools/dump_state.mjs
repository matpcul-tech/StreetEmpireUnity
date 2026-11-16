// Dump every exported data constant (and HUSTLES) from Street Empire's src/state.js as JSON.
// Read only: it imports the module, it never writes to the JS repo.
import fs from 'fs';
import { pathToFileURL } from 'url';
const file = process.argv[2];
const S = await import(pathToFileURL(file).href);
const out = {};
for (const [k, v] of Object.entries(S)) if (typeof v !== 'function') out[k] = v;
const m = fs.readFileSync(file, 'utf8').match(/const HUSTLES = (\[[^\]]*\]);/);
out.HUSTLES = JSON.parse(m[1].replace(/'/g, '"'));
process.stdout.write(JSON.stringify(out));
