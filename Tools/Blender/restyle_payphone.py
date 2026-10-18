# Claude, 2026-10-18 (owner: "make this payphone look realistic").
# Restyles the CC0 "Korean Public Payphone 01" (Poly Haven, by UM JOORIN, CC0) for the pink phone booths.
# Unity Unicorn's rule for real-looking props: start from a real scan or model, never from primitives, and keep
# what makes it read as real (true proportions, soft 5-10 mm bevels, wear, grime and the PBR texture set).
#
# What it does
#   1. Imports the glTF (1k set), keeps one connected body, decimates to the triangle budget with UVs kept,
#      sharp edges by angle, and turns it so its face points along Unity +Z with the pivot at the back, bottom centre.
#   2. Rebuilds the texture set for Unity's Standard shader:
#      albedo  - stainless steel becomes rose-pink metal (luminance and grime kept); the orange card unit becomes
#                hot pink; dark plastics, the screen and the stickers stay as they are
#      metallic-smoothness (R = metal, A = 1 - roughness), normal (OpenGL, as Unity expects), occlusion (from ARM.R)
#   3. Writes mesh JSON (Unity space) + PNGs, and renders a check image.
# Run:
#   blender -b --factory-startup --python-exit-code 1 --python Tools/Blender/restyle_payphone.py -- <payphone.gltf> <out-dir> <tex-dir> [tris]
import bpy, bmesh, sys, os, json, math
import numpy as np
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:]
SRC, OUT, TEX = (os.path.abspath(a) for a in argv[:3])
BUDGET = int(argv[3]) if len(argv) > 3 else 16000
os.makedirs(OUT, exist_ok=True); os.makedirs(TEX, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=SRC)
ob = next(o for o in bpy.context.scene.objects if o.type == 'MESH')
bpy.context.view_layer.objects.active = ob; ob.select_set(True)
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
me = ob.data

# ---------------------------------------------------------------- textures (before the mesh changes)
mat = me.materials[0]
imgs = {n.image.name: n.image for n in mat.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image}
def px(img):
    a = np.array(img.pixels[:], dtype=np.float32).reshape(img.size[1], img.size[0], 4); return a
diff = px(next(v for k, v in imgs.items() if "diff" in k))
arm = px(next(v for k, v in imgs.items() if "arm" in k or "rough" in k))
nor = px(next(v for k, v in imgs.items() if "nor" in k))
ao, rough, metal = arm[..., 0], arm[..., 1], arm[..., 2]
rgb = diff[..., :3]
lum = rgb @ np.array([.2126, .7152, .0722], dtype=np.float32)
mx, mn = rgb.max(-1), rgb.min(-1); sat = (mx - mn) / np.maximum(mx, 1e-4)
# hue (0..1)
r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
d = np.maximum(mx - mn, 1e-5)
hue = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) / 6
rose = np.array([1.0, .55, .78], dtype=np.float32)       # rose-pink metal (linear)
hot = np.array([1.0, .16, .55], dtype=np.float32)        # hot pink like the booth
steel = (sat < .22) & (lum > .16)                       # bare and painted metal panels (sides and back read low on metal)
H, W = lum.shape
def box(x0, y0, x1, y1):                                  # pixel box given top-down at 1024, scaled to this texture
    k = W / 1024.0; return slice(H - int(y1 * k), H - int(y0 * k)), slice(int(x0 * k), int(x1 * k))
# no brand marks in the world: the card's company logo and the transit card's brand become plain card stock
logo = box(407, 336, 488, 367)
card = box(140, 318, 192, 440)
screen = box(806, 8, 1000, 78)
steel[screen] = False
orange = (sat > .35) & (hue > .02) & (hue < .16) & (lum > .15)
out = rgb.copy()
# steel: keep its brightness variation (scratches, grime) but colour it rose
out[steel] = (rose * (lum[steel] / max(lum[steel].mean(), 1e-3) * .82)[:, None]).clip(0, 1)
out[orange] = (hot * (.35 + lum[orange])[:, None]).clip(0, 1)
fill = np.median(out[box(410, 461, 480, 465)].reshape(-1, 3), 0)   # blank card stock below the text
out[logo] = fill
out[card] = np.array([.98, .78, .9], dtype=np.float32)
for bx in (logo, card):                                  # the logo is embossed and glossy too: flatten relief, shading and gloss
    nor[bx][..., 0] = .5; nor[bx][..., 1] = .5; nor[bx][..., 2] = 1.0
    ao[bx] = np.median(ao[bx])
    rough[bx] = np.median(rough[bx]); metal[bx] = np.median(metal[bx])   # printed logos also differ in gloss
albedo = np.concatenate([out, np.ones_like(lum)[..., None]], -1)
# Unity Standard: metallic in R, smoothness in A; pink steel stays metal but a little less glossy than chrome
ms = np.stack([metal, metal, metal, (1 - rough) * .72], -1)   # a little less gloss: no hot edge lines under the bloom
occ = np.stack([ao, ao, ao, np.ones_like(ao)], -1)
def save(name, a):
    h, w = a.shape[:2]; img = bpy.data.images.new(name, w, h, alpha=True, float_buffer=False)
    img.pixels[:] = a.astype(np.float32).ravel(); img.filepath_raw = os.path.join(TEX, name + ".png"); img.file_format = 'PNG'; img.save()
    return img
save("payphone-pink-albedo-20261018", albedo)
save("payphone-pink-metallic-smoothness-20261018", ms)
save("payphone-pink-normal-20261018", nor)
save("payphone-pink-occlusion-20261018", occ)
print("STEEL px", int(steel.sum()), "ORANGE px", int(orange.sum()))

# ---------------------------------------------------------------- mesh: budget, sharp edges, orientation, pivot
tris0 = sum(len(p.vertices) - 2 for p in me.polygons)
if tris0 > BUDGET:
    dm = ob.modifiers.new("dec", 'DECIMATE'); dm.ratio = BUDGET / tris0; dm.use_collapse_triangulate = True
    bpy.ops.object.modifier_apply(modifier=dm.name)
tm = ob.modifiers.new("tri", 'TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=tm.name)
bpy.ops.object.shade_smooth()
try: me.set_sharpness_by_angle(angle=math.radians(40))
except Exception: pass
# face is -Y in Blender. Unity +Z = Blender +Y, so turn 180 degrees about up to face Unity +Z
me.transform(Matrix.Rotation(math.pi, 4, 'Z'))
vs = [v.co for v in me.vertices]
lo = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs))); hi = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
me.transform(Matrix.Translation(Vector((-(lo.x + hi.x) / 2, -lo.y, -lo.z))))   # back at y=0, bottom at z=0
me.update()

# per-corner export (position, normal, uv), deduplicated; Unity = (x, z, y) with winding flipped
uvl = me.uv_layers.active.data
cn = me.corner_normals
key2i, V, N, U, T = {}, [], [], [], []
for p in me.polygons:
    ids = []
    for li in p.loop_indices:
        vi = me.loops[li].vertex_index; co = me.vertices[vi].co; n = cn[li].vector; uv = uvl[li].uv
        k = (vi, round(n.x, 2), round(n.y, 2), round(n.z, 2), round(uv.x, 4), round(uv.y, 4))
        if k not in key2i:
            key2i[k] = len(V) // 3; V += [round(co.x, 5), round(co.z, 5), round(co.y, 5)]; N += [round(n.x, 4), round(n.z, 4), round(n.y, 4)]; U += [round(uv.x, 5), round(uv.y, 5)]
        ids.append(key2i[k])
    T += [ids[0], ids[2], ids[1]]
size = hi - lo
info = dict(name="Payphone pink", source="Poly Haven korean_public_payphone_01 (CC0, UM JOORIN)", tris=len(T) // 3, tris_source=tris0,
            size_m=[round(size.x, 3), round(size.z, 3), round(size.y, 3)], vertices=V, normals=N, uvs=U, triangles=T)
json.dump(info, open(os.path.join(OUT, "payphone_pink.json"), "w"), separators=(",", ":"))
print("PAYPHONE tris", tris0, "->", info["tris"], "verts", len(V) // 3, "size m", info["size_m"])

# ---------------------------------------------------------------- check render with the new textures
nt = mat.node_tree; bsdf = next(n for n in nt.nodes if n.type == 'BSDF_PRINCIPLED')
for n in [n for n in nt.nodes if n.type == 'TEX_IMAGE']: nt.nodes.remove(n)
def tex(name, sock, non_color=False, ch=None):
    t = nt.nodes.new('ShaderNodeTexImage'); t.image = bpy.data.images.load(os.path.join(TEX, name + ".png"))
    if non_color: t.image.colorspace_settings.name = 'Non-Color'
    if ch is None: nt.links.new(t.outputs['Color'], sock)
    else:
        sp = nt.nodes.new('ShaderNodeSeparateColor'); nt.links.new(t.outputs['Color'], sp.inputs[0]); nt.links.new(sp.outputs[ch], sock)
    return t
tex("payphone-pink-albedo-20261018", bsdf.inputs['Base Color'])
tex("payphone-pink-metallic-smoothness-20261018", bsdf.inputs['Metallic'], True, 0)
rt = nt.nodes.new('ShaderNodeTexImage'); rt.image = bpy.data.images.load(os.path.join(TEX, "payphone-pink-metallic-smoothness-20261018.png")); rt.image.colorspace_settings.name = 'Non-Color'
inv = nt.nodes.new('ShaderNodeMath'); inv.operation = 'SUBTRACT'; inv.inputs[0].default_value = 1; nt.links.new(rt.outputs['Alpha'], inv.inputs[1]); nt.links.new(inv.outputs[0], bsdf.inputs['Roughness'])
nm = nt.nodes.new('ShaderNodeNormalMap'); tn = tex("payphone-pink-normal-20261018", nm.inputs['Color'], True); nt.links.new(nm.outputs['Normal'], bsdf.inputs['Normal'])
sc = bpy.context.scene
try: sc.render.engine = 'BLENDER_EEVEE_NEXT'
except Exception: sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1100, 1400
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (1, .7, .85, 1); w.node_tree.nodes["Background"].inputs[1].default_value = 1.2
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN')); sc.collection.objects.link(sun); sun.rotation_euler = (math.radians(50), 0, math.radians(200)); sun.data.energy = 3
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
c = Vector((0, size.y / 2, size.z / 2)); cam.location = c + Vector((.55, 1.25, .2)); cam.rotation_euler = (c - cam.location).to_track_quat('-Z', 'Y').to_euler(); cam.data.lens = 50
sc.render.filepath = os.path.join(OUT, "payphone-pink-check.png"); bpy.ops.render.render(write_still=True)
print("RESTYLE_DONE")
