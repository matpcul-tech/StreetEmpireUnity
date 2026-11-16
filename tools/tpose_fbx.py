# Put an A-posed Mixamo rig into a T-pose and make that the rest pose, so Unity Humanoid retargets cleanly.
import bpy, sys
from mathutils import Matrix, Vector
argv = sys.argv[sys.argv.index('--') + 1:]
src, dst = argv[0], argv[1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')
def align(name, target):
    pb = arm.pose.bones.get(name)
    if not pb: print('missing', name); return
    bpy.context.view_layer.update()
    head = pb.head.copy(); cur = (pb.tail - pb.head).normalized()
    q = cur.rotation_difference(Vector(target).normalized())
    R = Matrix.Translation(head) @ q.to_matrix().to_4x4() @ Matrix.Translation(-head)
    pb.matrix = R @ pb.matrix
    bpy.context.view_layer.update()
for side, sx in (('Left', 1), ('Right', -1)):
    for b in ('Arm', 'ForeArm', 'Hand'):
        align(f'mixamorig:{side}{b}', (sx, 0, 0))
bpy.ops.object.mode_set(mode='OBJECT')
for o in bpy.data.objects:
    if o.type != 'MESH': continue
    mods = [m for m in o.modifiers if m.type == 'ARMATURE']
    for m in mods:
        bpy.context.view_layer.objects.active = o
        keep = o.modifiers.new('ArmatureKeep', 'ARMATURE'); keep.object = m.object
        bpy.ops.object.modifier_apply(modifier=m.name)
        keep.name = 'Armature'
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')
bpy.ops.pose.armature_apply(selected=False)
bpy.ops.object.mode_set(mode='OBJECT')
bpy.ops.export_scene.fbx(filepath=dst, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, path_mode='COPY',
    embed_textures=False, bake_anim=False, apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', use_armature_deform_only=True)
print('wrote', dst)
