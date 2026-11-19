# v75h: recompute a finished building's vertex normals smooth by angle (30 degrees), nothing else changed. Matthew's
# v3 house-02-two-story had 37% of its wall triangles with normals bent more than 34 degrees off the face (smoothed
# across wall, roof and trim edges), the same fault that made the v2 white house glow at night.
# Usage: blender -b -P fix_normals.py -- in.glb out.glb
import sys, math, bpy
inp, out = sys.argv[sys.argv.index('--') + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=inp)
for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
    bpy.context.view_layer.objects.active = o; o.select_set(True)
    if o.data.has_custom_normals: bpy.ops.mesh.customdata_custom_splitnormals_clear()
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(30))
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='AUTO', export_apply=True, export_yup=True, export_normals=True, export_tangents=False)
