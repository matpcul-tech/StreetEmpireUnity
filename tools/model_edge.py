# v75 district edge, own work: a freeway segment (deck, barriers, pillars and a sound wall under it) that rings the
# Eastside, and four far towers for the stacked skyline layers (facade texture with an emission map of lit windows).
# Textures drawn with PIL first (python3 model_edge.py), then the models (blender -b --python model_edge.py).
# Exported unfinished; Sovereign Asset ("Unity prop") finishes and checks them.
import math, os, random
try: import bpy, bmesh
except ImportError: bpy = None
TEX = os.path.abspath('tex'); OUT = os.path.abspath('model')
def textures():
    from PIL import Image, ImageDraw, ImageFilter
    import numpy as np
    rng = np.random.default_rng(3)
    def noise(n, r):
        a = rng.random((n, n))
        for _ in range(2):
            acc = sum(np.roll(a, d, 0) for d in range(-r, r + 1)) / (2 * r + 1); a = sum(np.roll(acc, d, 1) for d in range(-r, r + 1)) / (2 * r + 1)
        return (a - a.mean()) / a.std()
    n = noise(512, 1) * 0.6 + noise(512, 8) * 0.4
    c = np.clip(np.array([150, 146, 138])[None, None, :] * (1 + 0.06 * n[..., None]), 0, 255).astype('uint8')
    Image.fromarray(c).save(f'{TEX}/concrete.jpg', quality=90)
    # tower facade: 8 columns x 8 floors of windows on a 512 tile (one tile = 16 m wide, 28 m tall)
    random.seed(11); W = 512
    base = Image.new('RGB', (W, W), (88, 86, 84)); em = Image.new('RGB', (W, W), (0, 0, 0))
    gb, ge = ImageDraw.Draw(base), ImageDraw.Draw(em)
    for fy in range(8):
        for cx in range(8):
            x0, y0 = cx * 64 + 12, fy * 64 + 16; x1, y1 = x0 + 40, y0 + 34
            gb.rectangle([x0, y0, x1, y1], fill=(36, 40, 46))
            if random.random() < 0.42:
                w = random.choice([(255, 196, 120), (255, 214, 150), (230, 236, 255), (255, 176, 96)])
                k = random.uniform(0.55, 1.0); ge.rectangle([x0 + 2, y0 + 2, x1 - 2, y1 - 2], fill=tuple(int(v * k) for v in w))
        gb.rectangle([0, fy * 64 + 58, W, fy * 64 + 63], fill=(104, 101, 98))
    base.filter(ImageFilter.GaussianBlur(0.6)).save(f'{TEX}/tower.jpg', quality=90)
    em.filter(ImageFilter.GaussianBlur(0.8)).save(f'{TEX}/tower_emission.jpg', quality=90)
def T(x, y, z): return (x, -z, y)   # glTF y up to Blender z up
def mat(name, img, rough, emit=None):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree; b = nt.nodes['Principled BSDF']
    b.inputs['Roughness'].default_value = rough
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(f'{TEX}/{img}'); nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if emit:
        e = nt.nodes.new('ShaderNodeTexImage'); e.image = bpy.data.images.load(f'{TEX}/{emit}'); nt.links.new(e.outputs['Color'], b.inputs['Emission Color'])
        b.inputs['Emission Strength'].default_value = 1.0
    return m
class Mesh:
    def __init__(s): s.bm = bmesh.new(); s.uv = s.bm.loops.layers.uv.new()
    def quad(s, p, mi, su, sv):
        vs = [s.bm.verts.new(T(*q)) for q in p]; f = s.bm.faces.new(vs); f.material_index = mi
        a, b, d = [__import__('mathutils').Vector(q) for q in (p[0], p[1], p[3])]
        U, V = (b - a), (d - a); lu, lv = U.length, V.length
        for l, (u, v) in zip(f.loops, [(0, 0), (lu, 0), (lu, lv), (0, lv)]): l[s.uv].uv = (u / su, v / sv)
    def box(s, x0, x1, y0, y1, z0, z1, mi, su=4.0, sv=4.0):
        q = s.quad
        q([(x0,y0,z1),(x1,y0,z1),(x1,y1,z1),(x0,y1,z1)], mi, su, sv); q([(x1,y0,z0),(x0,y0,z0),(x0,y1,z0),(x1,y1,z0)], mi, su, sv)
        q([(x1,y0,z1),(x1,y0,z0),(x1,y1,z0),(x1,y1,z1)], mi, su, sv); q([(x0,y0,z0),(x0,y0,z1),(x0,y1,z1),(x0,y1,z0)], mi, su, sv)
        q([(x0,y1,z1),(x1,y1,z1),(x1,y1,z0),(x0,y1,z0)], mi, su, sv); q([(x0,y0,z0),(x1,y0,z0),(x1,y0,z1),(x0,y0,z1)], mi, su, sv)
    def done(s, name, mats):
        me = bpy.data.meshes.new(name); s.bm.to_mesh(me); s.bm.free()
        o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
        for m in mats: me.materials.append(m)
        bpy.ops.object.select_all(action='SELECT')
        bpy.ops.export_scene.gltf(filepath=f'{OUT}/{name}.glb', export_format='GLB', export_apply=True)
        print('MODEL', name, len(me.polygons))
def freeway():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    c = mat('concrete', 'concrete.jpg', 0.9); m = Mesh(); L = 40.0
    # a 40 m piece along z: deck 14 m wide at 7.5 m, two barriers, two pillars, a 4 m sound wall on the outer side (+x)
    m.box(-7, 7, 7.5, 8.4, -L/2, L/2, 0)
    for x in (-7, 6.6): m.box(x, x + 0.4, 8.4, 9.3, -L/2, L/2, 0)
    for z in (-10, 10): m.box(-0.8, 0.8, 0, 7.5, z - 0.8, z + 0.8, 0)
    m.box(6.2, 6.6, 0, 4.2, -L/2, L/2, 0)
    m.done('edge-freeway', [c])
def tower(name, w, d, h):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    t = mat('tower', 'tower.jpg', 0.85, 'tower_emission.jpg'); r = mat('roof', 'concrete.jpg', 0.9); m = Mesh()
    q = m.quad; x0, x1, z0, z1 = -w/2, w/2, -d/2, d/2
    # four facades with the window tile at 16 m x 28 m, a flat roof
    q([(x0,0,z1),(x1,0,z1),(x1,h,z1),(x0,h,z1)], 0, 16, 28); q([(x1,0,z0),(x0,0,z0),(x0,h,z0),(x1,h,z0)], 0, 16, 28)
    q([(x1,0,z1),(x1,0,z0),(x1,h,z0),(x1,h,z1)], 0, 16, 28); q([(x0,0,z0),(x0,0,z1),(x0,h,z1),(x0,h,z0)], 0, 16, 28)
    q([(x0,h,z1),(x1,h,z1),(x1,h,z0),(x0,h,z0)], 1, 8, 8)
    m.done(name, [t, r])
if bpy is None:
    os.makedirs(TEX, exist_ok=True); textures()
else:
    os.makedirs(OUT, exist_ok=True); freeway()
    for n, w, d, h in (('edge-tower-a', 18, 18, 42), ('edge-tower-b', 26, 16, 64), ('edge-tower-c', 14, 14, 28), ('edge-tower-d', 32, 22, 20)): tower(n, w, d, h)
