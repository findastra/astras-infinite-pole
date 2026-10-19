# Claude, 2026-10-10 (round 38, owner: "fuse the cloud sea the same way").
# The pink cloud sea billows (1,520 spheres) and the banner booth mound (912 spheres) were separate round balls.
# Same recipe as make_cloud_redo.py, but keeping the existing layout: every sphere becomes a lobe, overlapping
# spheres form one clump, each clump is voxel-unioned into ONE connected body, softened, given billow relief and
# decimated to its share of the triangle budget.
#
# Input : Tools/Blender/out/sea_spheres.json (from Unity menu Astra/Claude/38b; Unity mesh-local space, ellipsoid
#         centre + half extents per sphere)
# Output: Tools/Blender/out/sea_fused.json (Unity space, winding flipped; r = height, g = fold, b = per-clump tint)
#         Review/round38/blender-sea.png
# Run:   blender -b --factory-startup --python Tools/Blender/fuse_sea.py -- Tools/Blender/out Review/round38
import bpy, bmesh, sys, os, json, math, random
from mathutils import Vector, Matrix, noise
from collections import defaultdict

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["Tools/Blender/out", "Review/round38"]
OUT, PREVIEW = os.path.abspath(argv[0]), os.path.abspath(argv[1])
os.makedirs(PREVIEW, exist_ok=True)
BUDGET = {"Sea billows": 140000, "Banner mound": 45000}

bpy.ops.wm.read_factory_settings(use_empty=True)
data = json.load(open(os.path.join(OUT, "sea_spheres.json")))


def clumps(sp):
    n = len(sp); par = list(range(n))
    def f(a):
        while par[a] != a:
            par[a] = par[par[a]]; a = par[a]
        return a
    for i in range(n):
        for j in range(i + 1, n):
            a, b = sp[i], sp[j]
            d2 = sum((a[k] - b[k]) ** 2 for k in range(3)); rr = max(a[3:]) + max(b[3:])
            if d2 < rr * rr * .9: par[f(i)] = f(j)
    g = defaultdict(list)
    for i in range(n): g[f(i)].append(sp[i])
    return list(g.values())


def keep_largest(me):
    bm = bmesh.new(); bm.from_mesh(me); seen = set(); isl = []
    for v in bm.verts:
        if v.index in seen: continue
        st, cur = [v], []; seen.add(v.index)
        while st:
            a = st.pop(); cur.append(a)
            for e in a.link_edges:
                b = e.other_vert(a)
                if b.index not in seen: seen.add(b.index); st.append(b)
        isl.append(cur)
    isl.sort(key=len, reverse=True)
    for c in isl[1:]: bmesh.ops.delete(bm, geom=c, context='VERTS')
    bm.to_mesh(me); bm.free()


def fbm(p, octaves, sv):
    v, amp, f = 0.0, 1.0, 1.0
    for _ in range(octaves):
        v += amp * noise.noise((p + sv) * f); amp *= .5; f *= 2.03
    return v


def apply_mod(ob, mod):
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)


results, preview = [], []
for target in data:
    name, sp = target["name"], target["spheres"]
    groups = clumps(sp)
    area = [sum(((s[3] + s[4] + s[5]) / 3) ** 2 for s in g) for g in groups]; total = sum(area)
    verts, norms, cols, tris = [], [], [], []
    for gi, g in enumerate(groups):
        rnd = random.Random(hash((name, gi)) & 0xffffff)
        bm = bmesh.new()
        for s in g:
            # Unity (x, y, z) -> Blender (x, z, y)
            c = Vector((s[0], s[2], s[1])); ex = (s[3], s[5], s[4])
            res = bmesh.ops.create_uvsphere(bm, u_segments=18, v_segments=10, radius=1.0)
            m = Matrix.Translation(c) @ Matrix.Rotation(rnd.uniform(0, 6.283), 4, 'Z') @ Matrix.Diagonal((ex[0], ex[1], ex[2], 1))
            bmesh.ops.transform(bm, matrix=m, verts=res["verts"])
        me = bpy.data.meshes.new(name + str(gi)); bm.to_mesh(me); bm.free()
        ob = bpy.data.objects.new(me.name, me); bpy.context.scene.collection.objects.link(ob)
        rmin = min((s[3] + s[4] + s[5]) / 3 for s in g); rmed = sorted((s[3] + s[4] + s[5]) / 3 for s in g)[len(g) // 2]
        rm = ob.modifiers.new("remesh", 'REMESH'); rm.mode = 'VOXEL'; rm.voxel_size = max(.06, min(.35, rmed * .18)); rm.adaptivity = 0
        apply_mod(ob, rm); keep_largest(ob.data)
        sm = ob.modifiers.new("soften", 'LAPLACIANSMOOTH'); sm.iterations = 3; sm.lambda_factor = .45; sm.use_volume_preserve = True; sm.use_normalized = True
        apply_mod(ob, sm)
        bm = bmesh.new(); bm.from_mesh(ob.data); bm.normal_update()
        sc = rmed / .6; sv = Vector((gi * .37, gi * .11, len(name)))
        for v in bm.verts:
            p = v.co / sc; up = max(0.0, v.normal.z)
            v.co += v.normal * (fbm(p * 1.9, 3, sv) * .085 + (1 - abs(fbm(p * 5.0, 2, sv + Vector((3, 1, 7))))) * .045 * (.45 + .55 * up)) * sc
        bm.normal_update(); bmesh.ops.smooth_vert(bm, verts=bm.verts, factor=.3, use_axis_x=True, use_axis_y=True, use_axis_z=True)
        bm.to_mesh(ob.data); bm.free()
        budget = max(200, int(BUDGET[name] * area[gi] / total))
        nt = sum(len(p.vertices) - 2 for p in ob.data.polygons)
        if nt > budget:
            dm = ob.modifiers.new("dec", 'DECIMATE'); dm.ratio = budget / nt * .98; apply_mod(ob, dm)
        tm = ob.modifiers.new("tri", 'TRIANGULATE'); apply_mod(ob, tm); keep_largest(ob.data)
        bm = bmesh.new(); bm.from_mesh(ob.data); bm.normal_update()
        crease = {}
        for v in bm.verts:
            acc, c = 0.0, 0
            for e in v.link_edges:
                d = e.other_vert(v).co - v.co
                if d.length > 1e-6: acc += v.normal.dot(d.normalized()); c += 1
            crease[v.index] = acc / max(1, c)
        for _ in range(3):
            crease = {v.index: (crease[v.index] + sum(crease[e.other_vert(v).index] for e in v.link_edges)) / (1 + len(v.link_edges)) for v in bm.verts}
        zs = [v.co.z for v in bm.verts]; z0, z1 = min(zs), max(zs); tint = rnd.random()
        base = len(verts) // 3; idx = {}
        for i, v in enumerate(bm.verts):
            idx[v.index] = base + i
            verts += [round(v.co.x, 4), round(v.co.z, 4), round(v.co.y, 4)]
            norms += [round(v.normal.x, 3), round(v.normal.z, 3), round(v.normal.y, 3)]
            cols += [round((v.co.z - z0) / max(.001, z1 - z0), 3), round(min(1, max(0, crease[v.index] * 3.2)), 3), round(tint, 3), 1]
        for f in bm.faces:
            a, b, c = [idx[x.index] for x in f.verts]; tris += [a, c, b]
        bm.free()
        if (name == "Banner mound" or gi < 6): preview.append(ob)
        else: bpy.data.objects.remove(ob)
    results.append(dict(name=name, clumps=len(groups), vertices=verts, normals=norms, colors=cols, triangles=tris))
    print("FUSED", name, "spheres", len(sp), "clumps", len(groups), "tris", len(tris) // 3, "verts", len(verts) // 3)

with open(os.path.join(OUT, "sea_fused.json"), "w") as f:
    json.dump(results, f, separators=(",", ":"))

# preview: the banner mound and a few sea clumps
sc = bpy.context.scene
try: sc.render.engine = 'BLENDER_EEVEE_NEXT'
except Exception: sc.render.engine = 'BLENDER_EEVEE'
sc.render.resolution_x, sc.render.resolution_y = 1600, 900
mat = bpy.data.materials.new("cloud"); mat.use_nodes = True
mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (1, .92, .97, 1)
for o in preview:
    o.data.materials.append(mat)
    for p in o.data.polygons: p.use_smooth = True
w = bpy.data.worlds.new("w"); sc.world = w; w.use_nodes = True; w.node_tree.nodes["Background"].inputs[0].default_value = (.5, .42, .65, 1)
sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN')); sc.collection.objects.link(sun); sun.rotation_euler = (math.radians(50), 0, math.radians(30)); sun.data.energy = 3.5
cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
mound = [o for o in preview if o.name.startswith("Banner")]
if mound:
    mo = mound[0]; bb = [mo.matrix_world @ Vector(c) for c in mo.bound_box]; cen = sum(bb, Vector()) / 8
    for o in preview:
        o.hide_render = o is not mo
    cam.location = cen + Vector((0, -16, 9)); cam.rotation_euler = (math.radians(62), 0, 0)
    sc.render.filepath = os.path.join(PREVIEW, "blender-mound.png"); bpy.ops.render.render(write_still=True)
sea = [o for o in preview if not o.name.startswith("Banner")]
for o in preview: o.hide_render = o not in sea
for i, o in enumerate(sea):
    bb = [Vector(c) for c in o.bound_box]; cen = sum(bb, Vector()) / 8
    o.location = Vector(((i % 3) * 22 - 22, (i // 3) * 18, 0)) - cen
cam.location = (0, -42, 30); cam.rotation_euler = (math.radians(58), 0, 0)
sc.render.filepath = os.path.join(PREVIEW, "blender-sea.png"); bpy.ops.render.render(write_still=True)
print("SEA_FUSE_DONE")
