# v75: the white house with the red roof (house-01-bungalow) rebuilt as clean geometry. Only the Meshy roof is
# kept (every face above the 2.2 m eave of the Sovereign Asset v74 output); the walls, porch, windows and door are
# new boxes and planes with box-projected UVs on the house's own clean wall texture. No Meshy spend.
# Exported unfinished; Sovereign Asset ("Unity building", Size 4 m) finishes and checks it.
#   blender -b --python rebuild_house01.py -- <house-01 SA output.glb> <out.glb>
import bpy, bmesh, sys, math
src, out = sys.argv[sys.argv.index('--') + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
o = [x for x in bpy.context.scene.objects if x.type == 'MESH'][0]
bpy.context.view_layer.objects.active = o; o.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
me = o.data
EAVE = 2.2
def G(x, y, z): return (x, -z, y)          # glTF (y up, +z front) to Blender
bm = bmesh.new(); bm.from_mesh(me)
kill = [f for f in bm.faces if min(v.co.z for v in f.verts) < EAVE]
bmesh.ops.delete(bm, geom=kill, context='FACES')
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
# materials: keep the roof's Material_0, the SA clean wall color, and add plain ones
names = [m.name for m in me.materials]
def M(name, color, rough=0.8, img=None):
    if name in names: return names.index(name)
    m = bpy.data.materials.new(name); m.use_nodes = True; b = m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value = (*color, 1); b.inputs['Roughness'].default_value = rough
    if img is not None:
        t = m.node_tree.nodes.new('ShaderNodeTexImage'); t.image = img; m.node_tree.links.new(t.outputs['Color'], b.inputs['Base Color'])
    me.materials.append(m); names.append(name); return len(names) - 1
# a tiling white stucco (drawn by tools/stucco in the u75 work folder, next to the source): the SA wall texture
# is an atlas laid out for the old mesh, so it cannot tile on new walls
import os
wall_img = bpy.data.images.load(os.path.join(os.path.dirname(os.path.abspath(out)), 'stucco.jpg'))
mi_wall = M('walls_new', (0.85, 0.83, 0.78), 0.9, wall_img)
mi_trim = M('trim', (0.86, 0.85, 0.82), 0.6)
mi_door = M('door', (0.36, 0.22, 0.12), 0.55)
mi_lit = M('glass_lit', (0.16, 0.12, 0.09), 0.3)     # plain glass; renamed windows_lit / windows_dark at the end
mi_dark = M('glass_dark', (0.06, 0.07, 0.08), 0.25)
uv = bm.loops.layers.uv.verify()
def quad(p, mi, s=2.0):
    vs = [bm.verts.new(G(*q)) for q in p]
    f = bm.faces.new(vs); f.material_index = mi; f.normal_update()
    n = f.normal
    for l in f.loops:   # box projection, 1 UV = s meters
        c = l.vert.co
        if abs(n.z) > 0.7: u, v = c.x, c.y
        elif abs(n.x) > 0.7: u, v = c.y, c.z
        else: u, v = c.x, c.z
        l[uv].uv = (u / s, v / s)
    return f
def box(x0, x1, y0, y1, z0, z1, mi, s=2.0):
    P = lambda x, y, z: (x, y, z)
    quad([P(x0,y0,z1), P(x1,y0,z1), P(x1,y1,z1), P(x0,y1,z1)], mi, s)   # front +z
    quad([P(x1,y0,z0), P(x0,y0,z0), P(x0,y1,z0), P(x1,y1,z0)], mi, s)   # back
    quad([P(x1,y0,z1), P(x1,y0,z0), P(x1,y1,z0), P(x1,y1,z1)], mi, s)   # +x
    quad([P(x0,y0,z0), P(x0,y0,z1), P(x0,y1,z1), P(x0,y1,z0)], mi, s)   # -x
    quad([P(x0,y1,z1), P(x1,y1,z1), P(x1,y1,z0), P(x0,y1,z0)], mi, s)   # top
    quad([P(x0,y0,z0), P(x1,y0,z0), P(x1,y0,z1), P(x0,y0,z1)], mi, s)   # bottom
X, Z, H = 3.55, 3.6, EAVE + 0.25   # the wall tops tuck up under the kept roof
PX0, PX1, PZ = -1.0, 2.4, 1.7          # the porch: the front wall steps back to z 1.7 between x -1.0 and 2.4
# walls as an L plan solid: left front block, the back block, the right front block
box(-X, PX0, 0.0, H, PZ, Z, mi_wall)
box(PX1, X, 0.0, H, PZ, Z, mi_wall)
box(-X, X, 0.0, H, -Z, PZ, mi_wall)
# porch slab, step, two posts, a beam under the roof
box(PX0, PX1, 0.0, 0.16, PZ, Z + 0.05, mi_trim)
box(PX0 + 0.9, PX1 - 0.9, 0.0, 0.08, Z + 0.05, Z + 0.4, mi_trim)
for px in (PX0 + 0.12, PX1 - 0.12): box(px - 0.09, px + 0.09, 0.16, H, Z - 0.22, Z - 0.04, mi_trim)
box(PX0, PX1, H - 0.2, H, Z - 0.24, Z - 0.02, mi_trim)
# a window: frame bars proud of the wall, the glass set back between them
def window(cx, cy, w, h, face, lit):
    t, d = 0.07, 0.06
    if face in ('+z', '-z'):
        z = face == '+z' and 1 or -1
        wz = {'+z': None}.get(face)
    def fr(a0, a1, b0, b1):
        if face == '+z': box(a0, a1, b0, b1, fz, fz + d, mi_trim, 1)
        elif face == '-z': box(a0, a1, b0, b1, fz - d, fz, mi_trim, 1)
        elif face == '+x': box(fz, fz + d, b0, b1, a0, a1, mi_trim, 1)
        else: box(fz - d, fz, b0, b1, a0, a1, mi_trim, 1)
    fz = cx[1]; c = cx[0]
    fr(c - w/2 - t, c + w/2 + t, cy - h/2 - t, cy - h/2)        # sill
    fr(c - w/2 - t, c + w/2 + t, cy + h/2, cy + h/2 + t)        # head
    fr(c - w/2 - t, c - w/2, cy - h/2, cy + h/2)                # jambs
    fr(c + w/2, c + w/2 + t, cy - h/2, cy + h/2)
    fr(c - 0.025, c + 0.025, cy - h/2, cy + h/2)                # the middle mullion
    g = 0.012; mi = mi_lit if lit else mi_dark
    if face == '+z': quad([(c - w/2, cy - h/2, fz + g), (c + w/2, cy - h/2, fz + g), (c + w/2, cy + h/2, fz + g), (c - w/2, cy + h/2, fz + g)], mi, 1)
    elif face == '-z': quad([(c + w/2, cy - h/2, fz - g), (c - w/2, cy - h/2, fz - g), (c - w/2, cy + h/2, fz - g), (c + w/2, cy + h/2, fz - g)], mi, 1)
    elif face == '+x': quad([(fz + g, cy - h/2, c + w/2), (fz + g, cy - h/2, c - w/2), (fz + g, cy + h/2, c - w/2), (fz + g, cy + h/2, c + w/2)], mi, 1)
    else: quad([(fz - g, cy - h/2, c - w/2), (fz - g, cy - h/2, c + w/2), (fz - g, cy + h/2, c + w/2), (fz - g, cy + h/2, c - w/2)], mi, 1)
window((-2.3, Z), 1.35, 1.15, 1.1, '+z', True)
window((1.75, PZ), 1.45, 1.0, 1.1, '+z', False)
for zc, lit in ((1.6, True), (-1.8, False)): window((zc, X), 1.0, 1.15, 1.1, '+x', lit); window((zc, -X), 1.0, 1.15, 1.1, '-x', not lit)
for xc, lit in ((-1.8, False), (1.8, True)): window((xc, -Z), -0.0 + 1.0, 1.15, 1.1, '-z', lit)
# the door: a frame and a panel set back a little, in the porch wall
box(0.1 - 0.52, 0.1 + 0.52, 0.16, 2.2, PZ, PZ + 0.06, mi_trim, 1)
box(0.1 - 0.45, 0.1 + 0.45, 0.16, 2.12, PZ + 0.06, PZ + 0.09, mi_door, 1)
bm.normal_update(); bm.to_mesh(me); bm.free()
# drop the old walls and window materials that no face uses now
used = {p.material_index for p in me.polygons}
for i in reversed(range(len(me.materials))):
    if i not in used: me.materials.pop(index=i)
me.update()
for m in list(bpy.data.materials):
    if m.name in ('windows_lit', 'windows_dark', 'walls') and m.users == 0: bpy.data.materials.remove(m)
for m in me.materials:
    m.name = {'walls_new': 'walls', 'glass_lit': 'windows_lit', 'glass_dark': 'windows_dark'}.get(m.name, m.name)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_apply=True)
print('HOUSE', out, len(me.polygons), [m.name for m in me.materials])
