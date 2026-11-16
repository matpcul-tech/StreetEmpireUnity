import bpy, sys
argv = sys.argv[sys.argv.index('--') + 1:]
src, dst, anim = argv[0], argv[1], argv[2] == 'anim'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src, bone_heuristic='TEMPERANCE')
arms = [o for o in bpy.data.objects if o.type == 'ARMATURE']
print('armatures', [a.name for a in arms], 'actions', [a.name for a in bpy.data.actions])
if anim:
    # one take per action; drop NLA so each action exports on its own
    for a in arms:
        if a.animation_data:
            for t in list(a.animation_data.nla_tracks): a.animation_data.nla_tracks.remove(t)
    # keep only the first body and the armature, the clips are what matter
    for o in list(bpy.data.objects):
        if o.type == 'MESH' and o.name not in ('m_body',): bpy.data.objects.remove(o, do_unlink=True)
import os
if not anim:
    td = os.path.join(os.path.dirname(dst), 'tex'); os.makedirs(td, exist_ok=True)
    for i, im in enumerate(bpy.data.images):
        if im.type != 'IMAGE': continue
        ext = '.png' if im.file_format == 'PNG' else '.jpg'
        fp = os.path.join(td, (im.name or f'img{i}').replace('/', '_').split('.')[0] + ext)
        im.filepath_raw = fp; im.file_format = 'PNG' if ext == '.png' else 'JPEG'; im.save()
        print('image', im.name, fp)
bpy.ops.export_scene.fbx(filepath=dst, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
    path_mode='COPY', embed_textures=not anim, bake_anim=anim, bake_anim_use_all_actions=anim,
    bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.5,
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', use_armature_deform_only=True)
print('wrote', dst)
