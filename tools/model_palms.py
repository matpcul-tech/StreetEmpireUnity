# Own-work Eastside palms in the style of the original web game's procedural palm (src/city.js): a tapered
# trunk and a crown of drooping alpha-tested frond planes. Textures drawn here with PIL in the web palette.
# Exported unfinished: Sovereign Asset does the finishing and checks. blender -b --python model_palms.py
import math, os, random
try: import bpy, bmesh
except ImportError: pass
try:
    from PIL import Image, ImageDraw
except ImportError: Image = None   # Blender python: the textures are drawn first with python3 (TEXONLY)
TEX = os.path.abspath('tex'); OUT = os.path.abspath('model')
def frond_tex():
    # v75: twice the resolution, leaflets about twice as thick with small gaps and tapered tips, so they hold up as
    # leaves up close and do not break into a barcode or shimmer in the mips
    W, H = 256, 1024; im = Image.new('RGBA', (W, H), (0x52, 0x5c, 0x2c, 0)); g = ImageDraw.Draw(im)   # leaf color under the cut
    g.line([(128, H), (128, 0)], fill='#4a4a26', width=12)
    for y in range(30, 1000, 22):
        L = 112 * math.sin((y / H) * math.pi) + 12
        c = ['#56602e', '#46532a', '#6a6a36'][(y // 22) % 3]
        for sgn in (-1, 1):
            tip = (128 + sgn * L, y - 46)
            g.polygon([(128, y - 7), (128, y + 7), (128 + sgn * L * 0.55, y - 21), tip, (128 + sgn * L * 0.5, y - 33)], fill=c)
    im.save(f'{TEX}/palm-frond.png')
def bark_tex():
    random.seed(5); W, H = 256, 512; im = Image.new('RGB', (W, H), (0x7a, 0x66, 0x50)); g = ImageDraw.Draw(im)
    for y in range(0, H, 14):   # leaf scar rings
        d = random.randint(-10, 6)
        g.rectangle([0, y, W, y + 3], fill=(0x5e + d, 0x4e + d, 0x3c + d))
    for _ in range(1400):
        x, y = random.randrange(W), random.randrange(H); d = random.randint(-18, 14)
        g.point((x, y), fill=(0x7a + d, 0x66 + d, 0x50 + d))
    im.save(f'{TEX}/palm-bark.png')
def mat(name, img, rough, cut=False):
    m = bpy.data.materials.new(name); m.use_nodes = True; nt = m.node_tree
    b = nt.nodes['Principled BSDF']; b.inputs['Roughness'].default_value = rough
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(f'{TEX}/{img}.png')
    nt.links.new(t.outputs['Color'], b.inputs['Base Color'])
    if cut:
        r = nt.nodes.new('ShaderNodeMath'); r.operation = 'ROUND'
        nt.links.new(t.outputs['Alpha'], r.inputs[0]); nt.links.new(r.outputs[0], b.inputs['Alpha'])
        m.blend_method = 'CLIP'; m.use_backface_culling = False
    return m
def T(x, y, z): return (x, -z, y)   # three.js (y up) to Blender (z up); the glTF export maps it back
def palm(name, h, seed, lean):
    bpy.ops.wm.read_factory_settings(use_empty=True); random.seed(seed)
    bark = mat('palm_bark', 'palm-bark', 1.0); fr = mat('palm_frond', 'palm-frond', 0.9, True)
    me = bpy.data.meshes.new(name); bm = bmesh.new(); uv = bm.loops.layers.uv.new()
    S, R = 8, 4; rings = []
    for j in range(R + 1):          # tapered trunk, 0.3 m to 0.15 m, a slight curved lean
        a = j / R; r = 0.3 + (0.15 - 0.3) * a; y = h * a; off = lean * a * a
        rings.append([bm.verts.new(T(off + r * math.cos(2 * math.pi * k / S), y, r * math.sin(2 * math.pi * k / S))) for k in range(S + 1)])
    for j in range(R):
        for k in range(S):
            f = bm.faces.new([rings[j][k], rings[j][k + 1], rings[j + 1][k + 1], rings[j + 1][k]]); f.material_index = 0; f.smooth = True
            for l, (u, v) in zip(f.loops, [(k / S, j / R), ((k + 1) / S, j / R), ((k + 1) / S, (j + 1) / R), (k / S, (j + 1) / R)]):
                l[uv].uv = (u * 2, v * h / 4.5)
    top = (lean, h - 0.1)
    N = 5                           # v75: five segments per frond (was three) so the droop bends smoothly
    for i in range(9):              # nine fronds, 1.3 x 4.4 m, bent then tilted out and spun round the crown
        ax = -0.85 - (i % 3) * 0.18; ay = (i / 9) * math.pi * 2 + (i % 2) * 0.2
        grid = []
        for j in range(N + 1):
            row = []
            for xs in (-0.65, 0.65):
                # v75 droop: the frond arcs up and out from the crown, then hangs (about twice the old bend)
                y = 4.4 * j / N; z = -0.13 * y * y + 0.12 * y; x = xs * (1 - 0.25 * j / N)
                y, z = y * math.cos(ax) - z * math.sin(ax), y * math.sin(ax) + z * math.cos(ax)
                x, z = x * math.cos(ay) + z * math.sin(ay), -x * math.sin(ay) + z * math.cos(ay)
                row.append(bm.verts.new(T(x + top[0], y + top[1], z)))
            grid.append(row)
        for j in range(N):
            f = bm.faces.new([grid[j][0], grid[j][1], grid[j + 1][1], grid[j + 1][0]]); f.material_index = 1; f.smooth = True
            for l, (u, v) in zip(f.loops, [(0, j / N), (1, j / N), (1, (j + 1) / N), (0, (j + 1) / N)]): l[uv].uv = (u, v)
    bm.to_mesh(me); bm.free()
    o = bpy.data.objects.new(name, me); bpy.context.collection.objects.link(o)
    me.materials.append(bark); me.materials.append(fr)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.gltf(filepath=f'{OUT}/{name}.glb', export_format='GLB', export_apply=True)
    print('MODEL', name, sum(len(p.vertices) - 2 for p in me.polygons))
if Image: frond_tex(); bark_tex()
if os.environ.get("TEXONLY"): raise SystemExit
palm('palm-tall', 9.0, 3, 0.0)
palm('palm-short', 6.8, 9, 0.0)
