# dump per-triangle UVs and dominant bone for a character GLB -> npz
import bpy, sys, numpy as np
src, out = sys.argv[sys.argv.index("--")+1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
ob = next(o for o in bpy.context.scene.objects if o.type == "MESH"); me = ob.data
me.calc_loop_triangles()
uv = me.uv_layers.active.data
names = [g.name for g in ob.vertex_groups]
dom = np.zeros(len(me.vertices), dtype=np.int32)
for v in me.vertices:
    if v.groups: dom[v.index] = max(v.groups, key=lambda g: g.weight).group
tris = np.zeros((len(me.loop_triangles), 3, 2), dtype=np.float32); bone = np.zeros(len(me.loop_triangles), dtype=np.int32)
pos = np.zeros((len(me.loop_triangles), 3, 3), dtype=np.float32)
for i, t in enumerate(me.loop_triangles):
    for k in range(3):
        tris[i, k] = uv[t.loops[k]].uv; pos[i, k] = ob.matrix_world @ me.vertices[t.vertices[k]].co
    d = [dom[v] for v in t.vertices]; bone[i] = max(set(d), key=d.count)
np.savez(out, tris=tris, bone=bone, names=np.array(names), pos=pos)
print("tris", len(tris), "bones", len(names))
