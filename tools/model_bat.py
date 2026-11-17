# Own-work baseball bat for the mugger's friend (the web game's bat weapon): a 0.84 m turned profile,
# 10 sides, grip tape at the knob end. blender -b --python model_bat.py
import bpy, bmesh, math, os
TEX = os.path.abspath('tex'); OUT = os.path.abspath('model')
bpy.ops.wm.read_factory_settings(use_empty=True)
m = bpy.data.materials.new('bat_wood'); m.use_nodes = True; nt = m.node_tree
b = nt.nodes['Principled BSDF']; b.inputs['Roughness'].default_value = 0.55
t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(f'{TEX}/bat-wood.png'); nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
# (height, radius) from the knob up to the barrel end, z up
prof = [(0.0, 0.0), (0.0, 0.022), (0.02, 0.022), (0.03, 0.013), (0.30, 0.014), (0.50, 0.022), (0.68, 0.031), (0.82, 0.033), (0.84, 0.026), (0.84, 0.0)]
N = 10; me = bpy.data.meshes.new('bat'); bm = bmesh.new(); uv = bm.loops.layers.uv.new()
rings = []
for z, r in prof: rings.append([bm.verts.new((r * math.cos(2 * math.pi * k / N), r * math.sin(2 * math.pi * k / N), z)) for k in range(N)])
for i in range(len(prof) - 1):
    for k in range(N):
        a, c = rings[i], rings[i + 1]; k2 = (k + 1) % N
        try: f = bm.faces.new([a[k], a[k2], c[k2], c[k]])
        except ValueError: continue
        v0 = prof[i][0] / 0.84; v1 = prof[i + 1][0] / 0.84   # image bottom (v 0) is the grip tape at the knob
        for l, q in zip(f.loops, [(k / N, v0), ((k + 1) / N, v0), ((k + 1) / N, v1), (k / N, v1)]): l[uv].uv = q
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(me); bm.free()
o = bpy.data.objects.new('bat', me); bpy.context.collection.objects.link(o); me.materials.append(m)
for p in me.polygons: p.use_smooth = True
bpy.ops.object.select_all(action='SELECT')
bpy.ops.export_scene.gltf(filepath=f'{OUT}/bat.glb', export_format='GLB', export_apply=True)
print('MODEL bat', sum(len(p.vertices) - 2 for p in me.polygons))
