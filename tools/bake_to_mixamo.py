# Bakes the animation pack's clips (Quaternius UAL skeleton) onto the Mixamo skeleton our characters use, so the game
# plays clips made for its own bones. Each bone takes the turn its counterpart made from the rest pose, measured in
# world space, so two upright T-poses stay matched whatever way the bones themselves were drawn.
# usage (needs the bpy module): python3 tools/bake_to_mixamo.py tools/anim-src/ual-clips.fbx \
#   Assets/Art/Models/Characters/Player/player-m-2.fbx Assets/Art/Animations/mixamo-clips.fbx
import bpy, sys, math
from mathutils import Matrix, Quaternion, Vector
src_path, tgt_path, out_path = sys.argv[-3:]
MAP = {'pelvis':'Hips','spine_01':'Spine','spine_02':'Spine1','spine_03':'Spine2','neck_01':'Neck','Head':'Head'}
for s,S in (('l','Left'),('r','Right')):
    MAP.update({f'clavicle_{s}':f'{S}Shoulder',f'upperarm_{s}':f'{S}Arm',f'lowerarm_{s}':f'{S}ForeArm',f'hand_{s}':f'{S}Hand',
                f'thigh_{s}':f'{S}UpLeg',f'calf_{s}':f'{S}Leg',f'foot_{s}':f'{S}Foot',f'ball_{s}':f'{S}ToeBase'})
    for f,F in (('index','Index'),('middle','Middle'),('pinky','Pinky'),('ring','Ring'),('thumb','Thumb')):
        for i in (1,2,3): MAP[f'{f}_0{i}_{s}'] = f'{S}Hand{F}{i}'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src_path)
src = [o for o in bpy.data.objects if o.type=='ARMATURE'][0]
actions = list(bpy.data.actions)
bpy.ops.import_scene.fbx(filepath=tgt_path)
tgt = [o for o in bpy.data.objects if o.type=='ARMATURE' and o != src][0]
for o in list(bpy.data.objects):
    if o.type == 'MESH': bpy.data.objects.remove(o)
tgt.name = 'Armature'
# rest poses, with no action playing
if src.animation_data: src.animation_data.action = None
for pb in src.pose.bones: pb.matrix_basis = Matrix.Identity(4)
bpy.context.view_layer.update()
SW = src.matrix_world.copy(); TW = tgt.matrix_world.copy()
src_rest = {b.name: (SW @ b.matrix_local) for b in src.data.bones}
tgt_rest_arm = {b.name: b.matrix_local.copy() for b in tgt.data.bones}
TWq = TW.to_quaternion(); TWqi = TWq.inverted()
tgt_rest_world_q = {n: (TWq @ m.to_quaternion()) for n,m in tgt_rest_arm.items()}
inv = {v:k for k,v in MAP.items()}
hip_t = 'mixamorig:Hips'
src_hip_h = (src_rest['pelvis'].translation - src_rest['root'].translation).length if 'root' in src_rest else src_rest['pelvis'].translation.z
tgt_hip_h = (TW @ tgt_rest_arm[hip_t]).translation.z
k = tgt_hip_h / max(src_hip_h, 1e-4)
print(f'[bake] hip height source {src_hip_h:.3f} target {tgt_hip_h:.3f} scale {k:.3f}; {len(actions)} clips; mapped {sum(1 for s in MAP if s in src_rest and "mixamorig:"+MAP[s] in tgt_rest_arm)} of {len(MAP)} bones')
order = [b.name for b in tgt.data.bones]   # parents first
scene = bpy.context.scene; scene.render.fps = 30
tgt.animation_data_create()
for pb in tgt.pose.bones: pb.rotation_mode = 'QUATERNION'
report = []
idle = [a for a in actions if 'Idle_Loop' in a.name and 'Talking' not in a.name and 'Pistol' not in a.name and 'Sitting' not in a.name][0]
src.animation_data.action = idle
if hasattr(src.animation_data, 'action_slot') and idle.slots: src.animation_data.action_slot = idle.slots[0]
scene.frame_set(int(idle.frame_range[0])); bpy.context.view_layer.update()
stand = (src.matrix_world @ src.pose.bones['pelvis'].matrix).translation - src.matrix_world.translation
foot0 = min((src.matrix_world @ src.pose.bones[n].matrix).translation.z for n in ('foot_l', 'foot_r')) - src.matrix_world.translation.z
k = tgt_hip_h / max(stand.z - foot0 + 0.09, 1e-4)   # ankle to floor is about 9 cm
print(f'[bake] the pelvis stands at {stand.z:.3f}, ankles at {foot0:.3f}; leg scale {k:.3f}')
for act in actions:
    name = act.name.split('|')[-1]
    if name.endswith('_Armature'): name = name[:-9]
    src.animation_data.action = act
    if hasattr(src.animation_data, 'action_slot') and act.slots: src.animation_data.action_slot = act.slots[0]
    f0, f1 = int(round(act.frame_range[0])), int(round(act.frame_range[1]))
    new = bpy.data.actions.new(name); tgt.animation_data.action = new
    leans = []; hips_z = []; feet_z = []
    for f in range(f0, f1 + 1):
        scene.frame_set(f); bpy.context.view_layer.update()
        SWf = src.matrix_world
        want = {}   # target bone -> world rotation wanted
        for tb in order:
            short = tb.split(':')[-1]; sb = inv.get(short)
            b = tgt.data.bones[tb]
            if sb and sb in src.pose.bones:
                sw = (SWf @ src.pose.bones[sb].matrix).to_quaternion()
                delta = sw @ src_rest[sb].to_quaternion().inverted()
                want[tb] = delta @ tgt_rest_world_q[tb]
            elif b.parent:   # carried by its parent
                pd = want[b.parent.name] @ tgt_rest_world_q[b.parent.name].inverted()
                want[tb] = pd @ tgt_rest_world_q[tb]
            else: want[tb] = tgt_rest_world_q[tb]
        for tb in order:
            b = tgt.data.bones[tb]; pb = tgt.pose.bones[tb]
            arm_q = TWqi @ want[tb]
            if b.parent:
                par_arm_q = TWqi @ want[b.parent.name]
                rest_rel = tgt_rest_arm[b.parent.name].to_quaternion().inverted() @ tgt_rest_arm[tb].to_quaternion()
                basis = rest_rel.inverted() @ (par_arm_q.inverted() @ arm_q)
            else:
                basis = tgt_rest_arm[tb].to_quaternion().inverted() @ arm_q
            pb.rotation_quaternion = basis; pb.keyframe_insert('rotation_quaternion', frame=f - f0 + 1)
        # the hips travel as the pelvis did from its rest, scaled to this body's leg length
        # (from where the pelvis stands in the idle, not from the pack's rest pose: that one sits 20 m under the floor)
        sp = (SWf @ src.pose.bones['pelvis'].matrix).translation - SWf.translation
        d_world = (sp - stand) * k
        d_arm = TWqi @ d_world
        hp = tgt.pose.bones[hip_t]
        hp.location = tgt_rest_arm[hip_t].to_quaternion().inverted() @ d_arm; hp.keyframe_insert('location', frame=f - f0 + 1)
        # lean of the upper body: hips to neck against straight up, forward positive (the character faces -Y)
        bpy.context.view_layer.update()
        hw = TW @ tgt.pose.bones[hip_t].head; nw = TW @ tgt.pose.bones['mixamorig:Neck'].head; v = nw - hw
        leans.append(math.degrees(math.atan2(-v.y, v.z)))
        shw = SWf @ src.pose.bones['pelvis'].head; snw = SWf @ src.pose.bones['neck_01'].head; sv = snw - shw
        leans.append(math.degrees(math.atan2(-sv.y, sv.z)))
        hips_z.append((TW @ tgt.pose.bones[hip_t].head).z); feet_z.append(min((TW @ tgt.pose.bones['mixamorig:' + n].head).z for n in ('LeftFoot', 'RightFoot')))
    t = leans[0::2]; s_ = leans[1::2]
    report.append((name, f1 - f0 + 1, sum(t)/len(t), sum(s_)/len(s_), min(hips_z), max(hips_z), min(feet_z)))
    new.use_fake_user = True
for r in report: print(f'[bake] {r[0]:22s} {r[1]:3d} frames  lean {r[2]:6.1f} deg (source {r[3]:6.1f})  hips {r[4]:5.2f} to {r[5]:5.2f} m, lowest ankle {r[6]:5.2f} m')
bpy.data.objects.remove(src)
for a in list(bpy.data.armatures):
    if a.users == 0: bpy.data.armatures.remove(a)
tgt.name = 'Armature'; tgt.data.name = 'Armature'   # the name the character files give theirs
tgt.animation_data.action = None
for pb in tgt.pose.bones: pb.matrix_basis = Matrix.Identity(4)
# the file's own standing pose must be the rest pose, not whatever frame was evaluated last
scene.frame_set(0); bpy.context.view_layer.update()
worst = max((pb.matrix.to_quaternion().rotation_difference(tgt.data.bones[pb.name].matrix_local.to_quaternion()).angle for pb in tgt.pose.bones))
print(f'[bake] exported standing pose is off the rest pose by at most {math.degrees(worst):.3f} deg')
# A second top level object goes in beside the armature. With the armature alone in the file Unity makes it the
# file's own root, the armature's turn (Blender's Z up to Unity's Y up) lands on the root, and the humanoid reads
# the whole body a quarter turn out: every character stood with its feet crossed. The character files have a mesh
# beside their armature, so theirs stays a child named Armature; this keeps the clips file the same shape.
anchor = bpy.data.objects.new('Clips', None); bpy.context.scene.collection.objects.link(anchor)
bpy.ops.object.select_all(action='DESELECT'); tgt.select_set(True); anchor.select_set(True); bpy.context.view_layer.objects.active = tgt
bpy.ops.export_scene.fbx(filepath=out_path, use_selection=True, object_types={'ARMATURE', 'EMPTY'}, add_leaf_bones=False, bake_anim=True,
    bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_use_all_bones=True, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.4,
    # real units on the bones themselves: Blender's default leaves the armature 100 times too big with bones 100 times
    # too small, and Unity then finds every bone a bone length out of place against the player's avatar
    apply_scale_options='FBX_SCALE_ALL')
print('[bake] wrote', out_path)
