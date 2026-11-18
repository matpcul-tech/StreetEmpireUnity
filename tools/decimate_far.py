# Far version of a kit building: Blender's collapse decimate (it works across the UV seams that stop the
# meshoptimizer simplifier on these Meshy meshes), exported with no images; Unity gives it the near
# model's material. Usage: blender -b -P decimate_far.py -- in.glb out.glb ratio
import os, sys, bpy
inp, out, ratio = sys.argv[sys.argv.index('--') + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=inp)
for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
    bpy.context.view_layer.objects.active = o
    m = o.modifiers.new('far', 'DECIMATE'); m.decimate_type = 'COLLAPSE'; m.ratio = float(ratio); m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)
    if os.environ.get('RECALC'):   # v75d: a shell the collapse turned inside out (Sovereign Asset: Faces point outward)
        bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.normals_make_consistent(inside=False); bpy.ops.object.mode_set(mode='OBJECT')
    print('far', o.name, len(o.data.polygons), 'faces')
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='NONE', export_apply=True, export_yup=True)
