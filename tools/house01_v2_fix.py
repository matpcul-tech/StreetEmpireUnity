# v75e: Matthew's v2 white house (house-01-bungalow-finished.glb). Two fixes, nothing else changed:
# 1. its walls material also covers the roof, and the vertex normals were smoothed across the wall and roof edges
#    (39% of the wall triangles had normals bent up toward the sky), so the walls caught the sky light and the moon
#    and read nearly as bright at night as by day. Normals are recomputed smooth by angle (30 degrees).
# 2. the roof's faces (up-facing, normal z over 0.45, in the walls material) move to a material named "roof" (the
#    same textures), so each placed house can get a slight wall tint without tinting the roof.
# 3. the two front windows are only painted in the texture (the file's glass is on the sides and back), so they get a
#    pane each in windows_lit, 1 cm proud of the wall and 6 cm inside the painted frame, so they can light at night like the rest (SplitWindows turns
#    about a third of the panes off). Rects measured on a 1 cm per pixel orthographic render of the front.
# Usage: blender -b -P house01_v2_fix.py -- in.glb out.glb
import sys, math, bpy, mathutils
inp, out = sys.argv[sys.argv.index('--') + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=inp)
for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
    me = o.data
    wi = next((i for i, m in enumerate(me.materials) if m and m.name.startswith('walls')), -1)
    if wi >= 0:
        roof = me.materials[wi].copy(); roof.name = 'roof'; me.materials.append(roof); ri = len(me.materials) - 1
        n = 0
        for p in me.polygons:
            if p.material_index == wi and p.normal.z > 0.45: p.material_index = ri; n += 1
        print('roof faces', n)
    bpy.context.view_layer.objects.active = o; o.select_set(True)
    if me.has_custom_normals: bpy.ops.mesh.customdata_custom_splitnormals_clear()   # the glTF normals win over smoothing otherwise
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(30))
import bmesh
FRONT = [(-2.28, -1.37, 1.08, 1.85), (2.09, 2.93, 1.08, 1.85)]   # glTF x0, x1, height y0, y1 (front is glTF +Z, Blender -Y)
o = next(o for o in bpy.context.scene.objects if o.type == 'MESH'); me = o.data
li = next(i for i, m in enumerate(me.materials) if m and m.name.startswith('windows_lit'))
dg = bpy.context.evaluated_depsgraph_get()
bm = bmesh.new(); bm.from_mesh(me)
for x0, x1, y0, y1 in FRONT:
    ys = []
    for fx in (0.25, 0.5, 0.75):
        for fy in (0.25, 0.5, 0.75):
            hit, loc, nrm, idx, ob, mw = sc_ray = bpy.context.scene.ray_cast(dg, (x0 + (x1 - x0) * fx, -30, y0 + (y1 - y0) * fy), (0, 1, 0))
            if hit: ys.append(loc.y)
    yf = min(ys) - 0.01; print('pane at', x0, x1, 'front y', yf)
    vs = [bm.verts.new(o.matrix_world.inverted() @ mathutils.Vector(c)) for c in ((x0, yf, y0), (x1, yf, y0), (x1, yf, y1), (x0, yf, y1))]
    f = bm.faces.new(vs); f.material_index = li; f.normal_update()
    if (o.matrix_world.to_3x3() @ f.normal).y > 0: f.normal_flip()
bm.to_mesh(me); bm.free()
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='AUTO', export_apply=True, export_yup=True, export_normals=True, export_tangents=False)
