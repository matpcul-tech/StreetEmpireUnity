// Per face box projection for a textured box: each vertex takes the two axes across its normal's
// dominant axis, scaled to the mesh bounds, so no face is collapsed to a line in UV space.
// node box_uv.mjs <in.glb> <out.glb> <meshName>
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
const [inp, out, name] = process.argv.slice(2);
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS); const d = await io.read(inp);
for (const m of d.getRoot().listMeshes()) { if (m.getName() !== name) continue;
  for (const p of m.listPrimitives()) {
    const P = p.getAttribute('POSITION').getArray(), N = p.getAttribute('NORMAL').getArray(), uv = p.getAttribute('TEXCOORD_0');
    const a = uv.getArray().slice(); const mn = [1e9,1e9,1e9], mx = [-1e9,-1e9,-1e9];
    for (let i = 0; i < P.length; i++) { mn[i%3] = Math.min(mn[i%3], P[i]); mx[i%3] = Math.max(mx[i%3], P[i]); }
    const n01 = (v, k) => (v - mn[k]) / ((mx[k] - mn[k]) || 1);
    for (let v = 0; v < P.length / 3; v++) {
      const nx = Math.abs(N[v*3]), ny = Math.abs(N[v*3+1]), nz = Math.abs(N[v*3+2]);
      const x = P[v*3], y = P[v*3+1], z = P[v*3+2];
      let u, w;
      if (nz >= nx && nz >= ny) { u = n01(x, 0); w = n01(y, 1); }       // front/back (glTF +Z)
      else if (nx >= ny) { u = n01(z, 2); w = n01(y, 1); }              // sides
      else { u = n01(x, 0); w = n01(z, 2); }                            // top/bottom
      a[v*2] = u; a[v*2+1] = 1 - w;
    }
    uv.setArray(a);
  } }
await io.write(out, d); console.log('box uv', name);
