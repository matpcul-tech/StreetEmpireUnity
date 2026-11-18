# v75c: the kit sedan's 1024 atlas packs its charts edge to edge, so the mip levels the game samples past a few
# meters mix neighboring charts and the paint breaks into white and dark patches (the "camo"). This lays the
# car out again with Smart UV Project and a wide margin, bakes the old base color, painted base color,
# roughness and normal into the new layout (Cycles, 8 px bake margin), and exports the car with the new
# maps; the kit's paint mask in the emissive slot is dropped (black factor, it never glowed). Sovereign Asset
# finishes and checks the result.
#   blender -b --python rebake_car.py -- <car.glb> <painted atlas.jpg> <out.glb> <out painted.jpg> [lod ratio out_lod.glb] [src=dst ...]
# Each extra src=dst map is baked to dst on the new layout too (a .png dst also gets its alpha as dst + '.alpha.png').
import bpy, sys, os
a = sys.argv[sys.argv.index('--') + 1:]
src, paint_src, out, paint_out = a[0], a[1], a[2], a[3]
lod_ratio = float(a[4]) if len(a) > 4 else 0; lod_out = a[5] if len(a) > 5 and a[5] != '-' else None
extras = [x.split('=') for x in a[6:]]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src, merge_vertices=True)   # weld the old chart seams so the new islands are whole panels
objs = [o for o in bpy.context.scene.objects if o.type == 'MESH']
mat = objs[0].material_slots[0].material
nodes = mat.node_tree.nodes
bsdf = next(n for n in nodes if n.type == 'BSDF_PRINCIPLED')
def src_img(sock):
    for l in mat.node_tree.links:
        if l.to_socket == sock:
            n = l.from_node
            while n.type != 'TEX_IMAGE':
                ins = [x for x in n.inputs if x.is_linked]
                if not ins: return None
                n = ins[0].links[0].from_node
            return n.image
    return None
base = src_img(bsdf.inputs['Base Color']); rough_img = src_img(bsdf.inputs['Roughness'])
print('base', base and base.name, 'rough', rough_img and rough_img.name)
old_uv = objs[0].data.uv_layers[0].name
# the body node carries a 2.25 scale and the wheels 0.3: put the scales into the meshes so Smart UV Project
# gives every part the same texel density (otherwise the wheels took most of the atlas)
bpy.ops.object.select_all(action='DESELECT')
for o in objs: o.select_set(True)
bpy.context.view_layer.objects.active = objs[0]
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
# one shared new layout over every part of the car
bpy.ops.object.select_all(action='DESELECT')
for o in objs:
    o.select_set(True); nl = o.data.uv_layers.new(name='bake'); o.data.uv_layers.active = nl
    o.data.uv_layers[old_uv].active_render = True
bpy.context.view_layer.objects.active = objs[0]
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
if os.environ.get('WELD'): bpy.ops.mesh.remove_doubles(threshold=float(os.environ['WELD']))   # a triangle soup (the police car) welded first
bpy.ops.uv.smart_project(angle_limit=1.15, island_margin=float(os.environ.get('MARGIN', '0.006')), area_weight=0.0, scale_to_bounds=False)
bpy.ops.uv.pack_islands(margin=float(os.environ.get('MARGIN', '0.006')), rotate=True)
bpy.ops.object.mode_set(mode='OBJECT')
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 4; sc.cycles.device = 'CPU'
sc.render.bake.margin = 8; sc.render.bake.use_clear = True
def target(name, size, color_space='sRGB'):
    im = bpy.data.images.new(name, size, size, alpha=False, float_buffer=False); im.colorspace_settings.name = color_space; return im
def bake_emit(img_src, im, alpha=False):
    # a temporary emission material reading the old map through the old UVs, baked into im on the new UVs
    t = bpy.data.materials.new('bake_tmp'); t.use_nodes = True; nt = t.node_tree; nt.nodes.clear()
    uvn = nt.nodes.new('ShaderNodeUVMap'); uvn.uv_map = old_uv
    tx = nt.nodes.new('ShaderNodeTexImage'); tx.image = img_src; tx.interpolation = 'Closest'
    em = nt.nodes.new('ShaderNodeEmission'); outn = nt.nodes.new('ShaderNodeOutputMaterial')
    nt.links.new(uvn.outputs['UV'], tx.inputs['Vector'])
    nt.links.new(tx.outputs['Alpha' if alpha else 'Color'], em.inputs['Color']); nt.links.new(em.outputs['Emission'], outn.inputs['Surface'])
    tg = nt.nodes.new('ShaderNodeTexImage'); tg.image = im; nt.nodes.active = tg
    for o in objs: o.material_slots[0].material = t
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.ops.object.bake(type='EMIT', margin=8, use_clear=True)
    for o in objs: o.material_slots[0].material = mat
SZ = 2048
alb = target('albedo', SZ); bake_emit(base, alb)
painted_src = bpy.data.images.load(paint_src); pal = target('painted', SZ); bake_emit(painted_src, pal)
orm = None
if rough_img is not None:
    rough_img.colorspace_settings.name = 'Non-Color'
    orm = target('orm', 1024, 'Non-Color'); bake_emit(rough_img, orm)
# normals: Cycles bakes the shading normal of the original material (its normal map through the old UVs) into
# tangent space of the new layout
nrm = target('normal', SZ, 'Non-Color')
tg = nodes.new('ShaderNodeTexImage'); tg.image = nrm; nodes.active = tg
for n in nodes:
    if n.type == 'TEX_IMAGE' and n is not tg:
        uvn = nodes.new('ShaderNodeUVMap'); uvn.uv_map = old_uv; mat.node_tree.links.new(uvn.outputs['UV'], n.inputs['Vector'])
bpy.ops.object.select_all(action='DESELECT')
for o in objs: o.select_set(True)
bpy.ops.object.bake(type='NORMAL', normal_space='TANGENT', margin=8, use_clear=True)
pal.filepath_raw = paint_out; pal.file_format = 'JPEG'; pal.save()
for xs, xd in extras:
    xi = bpy.data.images.load(xs); lin = xd.endswith('.png') and 'metal' in xd
    if lin: xi.colorspace_settings.name = 'Non-Color'
    xo = target('x', xi.size[0], 'Non-Color' if lin else 'sRGB'); bake_emit(xi, xo)
    xo.filepath_raw = xd; xo.file_format = 'PNG' if xd.endswith('.png') else 'JPEG'; xo.save()
    if xd.endswith('.png'):
        xa = target('xa', xi.size[0], 'Non-Color'); xi.alpha_mode = 'STRAIGHT'; bake_emit(xi, xa, alpha=True)
        xa.filepath_raw = xd + '.alpha.png'; xa.file_format = 'PNG'; xa.save()
    print('BAKED', xd)
# the new material on the new UVs: base color, glTF metallic roughness (as baked), normal
nm = bpy.data.materials.new(mat.name); nm.use_nodes = True; nt = nm.node_tree
p = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
def tex(im):
    n = nt.nodes.new('ShaderNodeTexImage'); n.image = im; return n
nt.links.new(tex(alb).outputs['Color'], p.inputs['Base Color'])
if orm is not None:
    sep = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(tex(orm).outputs['Color'], sep.inputs['Color'])
    nt.links.new(sep.outputs['Green'], p.inputs['Roughness']); nt.links.new(sep.outputs['Blue'], p.inputs['Metallic'])
else:
    p.inputs['Roughness'].default_value = 0.6; p.inputs['Metallic'].default_value = 0.0
nmap = nt.nodes.new('ShaderNodeNormalMap'); nt.links.new(tex(nrm).outputs['Color'], nmap.inputs['Color']); nt.links.new(nmap.outputs['Normal'], p.inputs['Normal'])
for o in objs:
    o.material_slots[0].material = nm
    o.data.uv_layers.remove(o.data.uv_layers[old_uv]); o.data.uv_layers['bake'].name = 'UVMap'
    o.data.uv_layers['UVMap'].active = True; o.data.uv_layers['UVMap'].active_render = True
for im in (alb, nrm) + ((orm,) if orm else ()):
    im.pack()
def export(path):
    bpy.ops.export_scene.gltf(filepath=path, export_format='GLB', export_image_format='JPEG', export_jpeg_quality=92, export_materials='EXPORT', export_yup=True)
export(out); print('WROTE', out)
if lod_out:
    for o in objs:
        d = o.modifiers.new('dec', 'DECIMATE'); d.ratio = lod_ratio; d.delimit = {'UV', 'SEAM'}
        bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier='dec')
    export(lod_out); print('WROTE', lod_out)
