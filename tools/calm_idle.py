# Give the UAL Idle_Loop a relaxed standing stance: the CC0 clip stands in a wide, staggered athletic stance
# (feet about 0.4 m apart, knees bent 15 to 27 deg), which every Humanoid in the game copied as a squat.
# The upper body (spine_01 and up) keeps its exact world motion from the clip; the pelvis faces forward and the
# legs stand nearly straight (knees about 5 deg) with the feet a little wider than the hips, soles where the
# clip had them. Walk_Loop folds the swing knee a little less. Every other clip is written back unchanged.
# Usage: blender -b --python tools/calm_idle.py -- <ual-clips.fbx in> <out.fbx>
import bpy, sys, math
from mathutils import Matrix, Vector, Quaternion
src, dst = sys.argv[sys.argv.index('--') + 1:][:2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
arm = next(o for o in bpy.data.objects if o.type == 'ARMATURE')
MW = arm.matrix_world; P = arm.pose.bones; sc = bpy.context.scene
bpy.context.view_layer.objects.active = arm
bpy.ops.object.mode_set(mode='POSE')
arm.animation_data_create()
def upd(): bpy.context.view_layer.update()
def world_head(n): return MW @ P[n].head
def aim(name, child, target):
    pb = P[name]; upd()
    h = pb.head.copy(); cur = (P[child].head - h).normalized()
    t = (MW.inverted().to_3x3() @ Vector(target)).normalized()
    q = cur.rotation_difference(t)
    pb.matrix = Matrix.Translation(h) @ q.to_matrix().to_4x4() @ Matrix.Translation(-h) @ pb.matrix; upd()
def set_world_rot(name, R3):
    pb = P[name]; m = pb.matrix.copy(); loc = m.translation.copy()
    pb.matrix = Matrix.Translation(loc) @ R3.to_4x4(); upd()
# rest foot direction (ankle to ball), so the soles stay flat as in the bind pose
arm.animation_data.action = None
for pb in P: pb.matrix_basis = Matrix.Identity(4)
upd()
rest_foot = {s: (world_head('ball_' + s) - world_head('foot_' + s)).normalized() for s in 'lr'}
idle = next(a for a in bpy.data.actions if a.name.endswith('|Idle_Loop_Armature'))
arm.animation_data.action = idle
f0, f1 = map(int, idle.frame_range)
side = {'l': 1, 'r': -1}   # +X is the character's left (the rig faces -Y)
legs = [f'{b}_{s}' for s in 'lr' for b in ('thigh', 'calf', 'foot', 'ball')]
frames = {}
for f in range(f0, f1 + 1):
    sc.frame_set(f); upd()
    frames[f] = dict(spine=(MW @ P['spine_01'].matrix).to_3x3().copy(),
                     feet={s: world_head('foot_' + s).z for s in 'lr'})
for f in range(f0, f1 + 1):
    sc.frame_set(f); upd()
    for b in legs: P[b].matrix_basis = Matrix.Identity(4)
    P['pelvis'].rotation_quaternion = (1, 0, 0, 0); upd()
    set_world_rot('spine_01', MW.inverted().to_3x3() @ frames[f]['spine'])
    for s in 'lr':
        sx = side[s]; a = math.radians(2.0)
        aim('thigh_' + s, 'calf_' + s, (sx * math.sin(a), -0.05, -1))   # knee a little forward and out
        aim('calf_' + s, 'foot_' + s, (sx * math.sin(a), 0.035, -1))    # shin back under it: about 5 deg at the knee
        toe = rest_foot[s].copy(); toe.x += sx * 0.12                   # toes turned out a touch
        aim('foot_' + s, 'ball_' + s, toe)
    dz = sum(frames[f]['feet'][s] - world_head('foot_' + s).z for s in 'lr') / 2
    pm = P['pelvis'].matrix.copy(); pm.translation += MW.inverted().to_3x3() @ Vector((0, 0, dz)); P['pelvis'].matrix = pm; upd()
    for b in ['pelvis', 'spine_01'] + legs:
        P[b].keyframe_insert('rotation_quaternion', frame=f)
    P['pelvis'].keyframe_insert('location', frame=f)
# Walk_Loop: the swing leg folds to about 87 deg at the knee (a high, marching step); fold only the swing part
# less (flexion past 40 deg is scaled by 0.6), so the planted leg and the stride length are untouched.
walk = next(a for a in bpy.data.actions if a.name.endswith('|Walk_Loop_Armature'))
arm.animation_data.action = walk
w0, w1 = map(int, walk.frame_range)
for f in range(w0, w1 + 1):
    sc.frame_set(f); upd()
    for s in 'lr':
        t, k, ft = world_head('thigh_' + s), world_head('calf_' + s), world_head('foot_' + s)
        flex = 180 - math.degrees((t - k).angle(ft - k))
        if flex > 40:
            want = 40 + (flex - 40) * 0.6
            c = P['calf_' + s]; q = c.rotation_quaternion.copy()
            c.rotation_quaternion = Quaternion((1, 0, 0, 0)).slerp(q, want / flex); upd()
        P['calf_' + s].keyframe_insert('rotation_quaternion', frame=f)
bpy.ops.object.mode_set(mode='OBJECT')
for a in [arm]:
    if a.animation_data:
        for t in list(a.animation_data.nla_tracks): a.animation_data.nla_tracks.remove(t)
bpy.ops.export_scene.fbx(filepath=dst, object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False,
    path_mode='STRIP', embed_textures=False, bake_anim=True, bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.5,
    apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y', use_armature_deform_only=True)
print('wrote', dst)
