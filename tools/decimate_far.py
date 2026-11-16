# Far version of a kit building: Blender's collapse decimate (it works across the UV seams that stop the
# meshoptimizer simplifier on these Meshy meshes), exported with no images; Unity gives it the near
# model's material. Usage: blender -b -P decimate_far.py -- in.glb out.glb ratio
import sys, bpy
inp, out, ratio = sys.argv[sys.argv.index('--') + 1:][:3]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=inp)
for o in [o for o in bpy.context.scene.objects if o.type == 'MESH']:
    bpy.context.view_layer.objects.active = o
    m = o.modifiers.new('far', 'DECIMATE'); m.decimate_type = 'COLLAPSE'; m.ratio = float(ratio); m.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=m.name)
    print('far', o.name, len(o.data.polygons), 'faces')
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', export_image_format='NONE', export_apply=True, export_yup=True)
