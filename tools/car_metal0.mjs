// v75e: the v75c sedan rebake exported metallicFactor 1 (the source had 0); the red paint read as dark brown metal at dusk
import { NodeIO } from '@gltf-transform/core'; import { ALL_EXTENSIONS } from '@gltf-transform/extensions';
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
for (const f of process.argv.slice(2)) { const d = await io.read(f); for (const m of d.getRoot().listMaterials()) m.setMetallicFactor(0); await io.write(f, d); console.log('metal 0', f); }
