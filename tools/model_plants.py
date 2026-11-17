# Own-work dry plants for the Eastside yards (the web game has dry grass, no round hedges) and a decal set:
# one flat quad per decal tile of tex/decal-atlas.png (cracks, oil, litter, graffiti) so the atlas goes through
# Sovereign Asset like any model. blender -b --python model_plants.py
import bpy, bmesh, math, os, random
TEX = os.path.abspath('tex'); OUT = os.path.abspath('model')
def reset(): bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name, img, rough, mode):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree
    b = nt.nodes['Principled BSDF']; b.inputs['Roughness'].default_value = rough
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(f'{TEX}/{img}.png')
    nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if mode == 'CLIP':
        r = nt.nodes.new('ShaderNodeMath'); r.operation = 'ROUND'; nt.links.new(t.outputs['Alpha'], r.inputs[0]); nt.links.new(r.outputs[0], b.inputs['Alpha'])
    else: nt.links.new(t.outputs['Alpha'], b.inputs['Alpha'])
    m.blend_method = mode; m.use_backface_culling = False; return m
def export(name, objs):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=f'{OUT}/{name}.glb', export_format='GLB', export_apply=True)
    print('MODEL', name, sum(len(p.vertices) - 2 for o in bpy.data.objects if o.type == 'MESH' for p in o.data.polygons))
def cards(bm, uv, cx, cy, w, h, n, spin, seed):
    random.seed(seed)
    for k in range(n):   # crossed vertical blade cards, a little tilted out
        a = spin + k * math.pi / n; ca, sa = math.cos(a) * w / 2, math.sin(a) * w / 2; tilt = random.uniform(-0.12, 0.12)
        v = [bm.verts.new((cx - ca, cy - sa, 0)), bm.verts.new((cx + ca, cy + sa, 0)),
             bm.verts.new((cx + ca + tilt, cy + sa, h)), bm.verts.new((cx - ca + tilt, cy - sa, h))]
        f = bm.faces.new(v)
        for l, q in zip(f.loops, [(0, 0), (1, 0), (1, 1), (0, 1)]): l[uv].uv = q
def plant(name, spots, seed):
    reset(); m = mat('dry_blades', 'dry-blades', 0.95, 'CLIP')
    me = bpy.data.meshes.new(name); bm = bmesh.new(); uv = bm.loops.layers.uv.new()
    for i, (x, y, w, h) in enumerate(spots): cards(bm, uv, x, y, w, h, 3, i * 0.7, seed + i)
    bm.to_mesh(me); bm.free(); o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o); me.materials.append(m)
    export(name, [o])
plant('dry-shrub', [(0, 0, 1.2, 0.95), (0.35, 0.2, 0.8, 0.7)], 3)
plant('dry-hedge', [(-0.9 + k * 0.6, random.uniform(-0.1, 0.1), 0.9, random.uniform(0.7, 0.95)) for k in range(4)], 9)
# decal set: 8 ground tiles (1.6 m) and 4 graffiti (2.4 x 1.2 m), laid out flat in a grid
reset(); m = mat('decals', 'decal-atlas', 0.9, 'BLEND')
me = bpy.data.meshes.new('decal-set'); bm = bmesh.new(); uv = bm.loops.layers.uv.new()
def quad(x, y, w, h, u0, v0, u1, v1):
    v = [bm.verts.new((x - w / 2, y - h / 2, 0)), bm.verts.new((x + w / 2, y - h / 2, 0)), bm.verts.new((x + w / 2, y + h / 2, 0)), bm.verts.new((x - w / 2, y + h / 2, 0))]
    f = bm.faces.new(v)
    for l, q in zip(f.loops, [(u0, v0), (u1, v0), (u1, v1), (u0, v1)]): l[uv].uv = q
for i in range(8):   # atlas rows from the top: Blender v runs bottom to top
    c, r = i % 4, i // 4; quad(c * 2.0, -r * 2.0, 1.6, 1.6, c / 4, 1 - (r + 1) / 4, (c + 1) / 4, 1 - r / 4)
for i in range(4):
    c, r = i % 2, i // 2; quad(c * 3.0, -5 - r * 1.6, 2.4, 1.2, c / 2, 1 - (2 + r + 1) / 4, (c + 1) / 2, 1 - (2 + r) / 4)
bm.to_mesh(me); bm.free(); o = bpy.data.objects.new('decal-set', me); bpy.context.collection.objects.link(o); me.materials.append(m)
export('decal-set', [o])
