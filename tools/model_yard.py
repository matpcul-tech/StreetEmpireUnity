# Own-work yard pieces for Street Empire Eastside: a wood fence panel with one real-scale board per plank (the old
# panel cube-projected one noise texture and stretched it), a small dry grass clump for the yards, and a carrier
# quad for the yard color variation map so it goes through Sovereign Asset like any model.
# blender -b --python model_yard.py
import bpy, bmesh, math, os, random
TEX = os.path.abspath('tex'); OUT = os.path.abspath('model')
def reset(): bpy.ops.wm.read_factory_settings(use_empty=True)
def mat(name, img, rough, mode=None):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree
    b = nt.nodes['Principled BSDF']; b.inputs['Roughness'].default_value = rough
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(f'{TEX}/{img}.png')
    nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if mode == 'CLIP':
        r = nt.nodes.new('ShaderNodeMath'); r.operation = 'ROUND'; nt.links.new(t.outputs['Alpha'], r.inputs[0]); nt.links.new(r.outputs[0], b.inputs['Alpha'])
        m.blend_method = 'CLIP'; m.use_backface_culling = False
    return m
def export(name):
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=f'{OUT}/{name}.glb', export_format='GLB', export_apply=True)
    print('MODEL', name, sum(len(p.vertices) - 2 for o in bpy.data.objects if o.type == 'MESH' for p in o.data.polygons))

B = 8   # boards in fence-boards.png
def board(bm, uv, x0, x1, y0, y1, z0, z1, k, along):
    """A box; front and back show board k at real scale: grain along Z (planks, posts) or along X (rails)."""
    vs = [bm.verts.new((x, y, z)) for x in (x0, x1) for y in (y0, y1) for z in (z0, z1)]
    V = lambda i, j, l: vs[i * 4 + j * 2 + l]
    quads = [(V(0,0,0), V(1,0,0), V(1,0,1), V(0,0,1)), (V(1,1,0), V(0,1,0), V(0,1,1), V(1,1,1)),   # front -Y, back +Y
             (V(0,1,0), V(0,0,0), V(0,0,1), V(0,1,1)), (V(1,0,0), V(1,1,0), V(1,1,1), V(1,0,1)),   # ends
             (V(0,0,1), V(1,0,1), V(1,1,1), V(0,1,1)), (V(0,1,0), V(1,1,0), V(1,0,0), V(0,0,0))]   # top, bottom
    u0 = k / B; du = 1 / B; vlen = 1.8   # one board image covers 1.8 m of grain
    random.seed(k * 7 + int(z0 * 100)); voff = random.uniform(0, 0.4)
    for f_i, q in enumerate(quads):
        f = bm.faces.new(q)
        for l in f.loops:
            x, y, z = l.vert.co
            if along == 'z':   # across the board = X (or Y on the ends), grain = Z
                a = (x - x0) / (x1 - x0) if f_i in (0, 1, 4, 5) else (y - y0) / (y1 - y0)
                g = z if f_i < 4 else (y - y0)
            else:              # rail: across = Z, grain = X
                a = (z - z0) / (z1 - z0) if f_i in (0, 1, 2, 3) else (y - y0) / (y1 - y0)
                g = x - x0 if f_i not in (2, 3) else (y - y0)
            l[uv].uv = (u0 + 0.06 * du + a * 0.88 * du, voff + g / vlen)
# wood fence panel 2.4 x 1.8: 16 planks, two rails behind, two posts (same footprint as the old panel)
reset(); m = mat('fence_boards', 'fence-boards', 0.88)
me = bpy.data.meshes.new('wood-fence'); bm = bmesh.new(); uv = bm.loops.layers.uv.new()
for k in range(16):
    x = -1.125 + k * 0.15; h = 1.75 - (k % 2) * 0.03
    board(bm, uv, x - 0.065, x + 0.065, -0.01, 0.01, 0, h, k % B, 'z')
for z in (0.35, 1.45): board(bm, uv, -1.2, 1.2, 0.01, 0.05, z, z + 0.08, (3 + int(z)) % B, 'x')
for i, x in enumerate((-1.2, 1.2)): board(bm, uv, x - 0.045, x + 0.045, -0.015, 0.075, 0, 1.85, (5 + i) % B, 'z')
bm.to_mesh(me); bm.free(); o = bpy.data.objects.new('wood-fence', me); bpy.context.collection.objects.link(o); me.materials.append(m)
export('wood-fence')
# dry grass clump, about 0.35 m: five small crossed blade cards
reset(); m = mat('dry_blades', 'dry-blades', 0.95, 'CLIP')
me = bpy.data.meshes.new('grass-clump'); bm = bmesh.new(); uv = bm.loops.layers.uv.new(); random.seed(21)
for c, (cx, cy, w, h) in enumerate([(0, 0, 0.42, 0.36), (0.16, 0.08, 0.3, 0.26), (-0.13, 0.1, 0.32, 0.3)]):
    for k in range(2):
        a = c * 0.9 + k * math.pi / 2; ca, sa = math.cos(a) * w / 2, math.sin(a) * w / 2; t = random.uniform(-0.05, 0.05)
        v = [bm.verts.new((cx - ca, cy - sa, 0)), bm.verts.new((cx + ca, cy + sa, 0)), bm.verts.new((cx + ca + t, cy + sa, h)), bm.verts.new((cx - ca + t, cy - sa, h))]
        f = bm.faces.new(v)
        for l, q in zip(f.loops, [(0, 0), (1, 0), (1, 1), (0, 1)]): l[uv].uv = q
bm.to_mesh(me); bm.free(); o = bpy.data.objects.new('grass-clump', me); bpy.context.collection.objects.link(o); me.materials.append(m)
export('grass-clump')
# carrier quad for the yard color variation detail map (4 m square)
reset(); m = mat('yard_variation', 'yard-variation', 1.0)
bpy.ops.mesh.primitive_plane_add(size=4); o = bpy.context.object; o.name = 'yard-variation'; o.data.materials.append(m)
export('yard-variation')
