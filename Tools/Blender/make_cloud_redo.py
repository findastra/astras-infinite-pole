# Claude, 2026-10-06 (owner's cloud redo). Reproducible source for the fused cloud meshes.
#
# Each cloud is ONE connected body: tilted ellipsoid metaballs of different sizes fused together
# (asymmetrical lobes along a bent spine, side billows, a hanging belly, small crown billows),
# voxel-remeshed, given gentle multi-scale billow relief, smoothed and decimated to budget.
# The walkable top stays near y = 0 (orbit) / y = 0.6 (outer), so standing heights don't change.
#
# Vertex colours (Unity): r = height 0 (bottom) .. 1 (top), g = crease (1 = deep fold between billows),
#                         b = per-billow random value (gentle tint variety), a = 1.
#
# Run (no downloads, built-in Blender only):
#   blender -b --factory-startup --python Tools/Blender/make_cloud_redo.py -- Tools/Blender/out Review/cloud-redo
# Output: Tools/Blender/out/cloud_redo.json (Unity space: y up, left-handed, winding already flipped)
#         Review/cloud-redo/blender-preview.png
import bpy, bmesh, sys, os, json, math, random
from mathutils import Vector, Quaternion, Euler, noise

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else ["Tools/Blender/out", "Review/cloud-redo"]
OUT, PREVIEW = os.path.abspath(argv[0]), os.path.abspath(argv[1])
os.makedirs(OUT, exist_ok=True); os.makedirs(PREVIEW, exist_ok=True)

# name, kind, seed, half x, half y (Unity z), top, bottom, triangle budget
SPECS = [("Orbit Cloud %d" % i, "orbit", 7100 + i * 37, 1.52, 1.17, 0.0, -1.32, 1400) for i in range(8)] + \
        [("Outer Cloud %d" % i, "outer", 9300 + i * 53, 5.25, 2.55, 0.6, -1.05, 1500) for i in range(6)]


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def build_lobes(name, kind, seed, ax, ay, top, bottom):
    """Grow overlapping, tilted ellipsoid lobes out of each other, so the body is always one connected piece."""
    rnd = random.Random(seed)
    s = 1.0 if kind == "orbit" else 2.6            # outer clouds are the same recipe, bigger
    lobes = []                                     # (centre Vector, radius, (sx, sy, sz), quaternion)
    def tilt():
        return Euler((rnd.uniform(-.45, .45), rnd.uniform(-.45, .45), rnd.uniform(0, math.pi * 2))).to_quaternion()
    def add(c, r, sx, sy, sz):
        lobes.append((Vector(c), r, (sx, sy, sz), tilt()))
    # main body: a bent chain of big lobes whose tops sit at the standing height (+ up to 12 cm)
    n = rnd.randint(4, 5) if kind == "orbit" else rnd.randint(6, 8)
    heading = rnd.uniform(-.5, .5); x, y = 0.0, 0.0
    pts = []
    for k in range(n):
        r = rnd.uniform(.5, .72) * s
        sz = rnd.uniform(.62, .8)
        pts.append((x, y, r, sz))
        step = r * rnd.uniform(.8, 1.05)
        heading += rnd.uniform(-.6, .6)
        x += math.cos(heading) * step; y += math.sin(heading) * step * .8
    mx = sum(p[0] for p in pts) / n; my = sum(p[1] for p in pts) / n
    for (x, y, r, sz) in pts:
        add((x - mx, y - my, top - r * sz + rnd.uniform(0, .1) * s), r, rnd.uniform(.95, 1.35), rnd.uniform(.85, 1.15), sz)
    mains = list(lobes)
    # side billows grown out of a main lobe (lopsided: most of them on one side)
    side = rnd.choice([-1, 1])
    for k in range(rnd.randint(7, 10) if kind == "orbit" else rnd.randint(12, 16)):
        c0, r0, _, _ = rnd.choice(mains)
        a = rnd.uniform(0, math.pi * 2)
        if rnd.random() < .6: a = side * math.pi / 2 + rnd.uniform(-1.3, 1.3)
        r = rnd.uniform(.32, .5) * s
        d = Vector((math.cos(a), math.sin(a) * .9, rnd.uniform(-.55, -.15))).normalized() * (r0 * .78)
        c = c0 + d
        c.z = min(c.z, top - r * .7)
        add(c, r, rnd.uniform(.9, 1.4), rnd.uniform(.85, 1.25), rnd.uniform(.65, .9))
    # hanging belly lobes, grown downward
    for k in range(rnd.randint(3, 4) if kind == "orbit" else rnd.randint(5, 7)):
        c0, r0, _, _ = rnd.choice(mains)
        r = rnd.uniform(.38, .55) * s
        c = c0 + Vector((rnd.uniform(-.4, .4) * r0, rnd.uniform(-.4, .4) * r0, -r0 * rnd.uniform(.55, .75)))
        add(c, r, rnd.uniform(1.0, 1.4), rnd.uniform(.9, 1.3), rnd.uniform(.6, .8))
    # small crown billows riding on the top, kept low so the top stays walkable
    for k in range(rnd.randint(5, 8) if kind == "orbit" else rnd.randint(9, 13)):
        c0, r0, sc0, _ = rnd.choice(mains)
        r = rnd.uniform(.18, .3) * s
        c = c0 + Vector((rnd.uniform(-.6, .6) * r0, rnd.uniform(-.5, .5) * r0, 0))
        c.z = top + rnd.uniform(.02, .1) * s - r * .7
        add(c, r, rnd.uniform(.9, 1.4), rnd.uniform(.9, 1.3), .7)
    bm = bmesh.new()
    from mathutils import Matrix
    for (c, r, (sx, sy, sz), q) in lobes:
        g = bmesh.ops.create_uvsphere(bm, u_segments=20, v_segments=12, radius=1.0)
        m = Matrix.Translation(c) @ q.to_matrix().to_4x4() @ Matrix.Diagonal((r * sx, r * sy, r * sz, 1))
        bmesh.ops.transform(bm, matrix=m, verts=g["verts"])
    me = bpy.data.meshes.new(name); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name.replace(" ", ""), me)
    bpy.context.scene.collection.objects.link(ob)
    blobs = [((c.x, c.y, c.z), r) for (c, r, _, _) in lobes]
    return ob, blobs


def fbm(p, octaves, seedv):
    v, amp, f = 0.0, 1.0, 1.0
    for _ in range(octaves):
        v += amp * noise.noise((p + seedv) * f); amp *= .5; f *= 2.03
    return v


def keep_largest(me):
    """One connected body only: drop every island but the largest."""
    bm = bmesh.new(); bm.from_mesh(me)
    islands, seen = [], set()
    for v in bm.verts:
        if v.index in seen: continue
        stack, isl = [v], []
        seen.add(v.index)
        while stack:
            a = stack.pop(); isl.append(a)
            for e in a.link_edges:
                b = e.other_vert(a)
                if b.index not in seen: seen.add(b.index); stack.append(b)
        islands.append(isl)
    islands.sort(key=len, reverse=True)
    for isl in islands[1:]: bmesh.ops.delete(bm, geom=isl, context='VERTS')
    bm.to_mesh(me); bm.free()
    return len(islands)


def finish(ob, name, kind, seed, ax, ay, top, bottom, budget, blobs):
    bpy.context.view_layer.objects.active = ob; ob.select_set(True)
    # even topology
    rm = ob.modifiers.new("remesh", 'REMESH'); rm.mode = 'VOXEL'
    rm.voxel_size = .045 if kind == "orbit" else .11; rm.adaptivity = 0
    bpy.ops.object.modifier_apply(modifier=rm.name)
    keep_largest(ob.data)
    sm = ob.modifiers.new("soften joins", 'LAPLACIANSMOOTH'); sm.iterations = 3; sm.lambda_factor = .45
    sm.use_volume_preserve = True; sm.use_normalized = True
    bpy.ops.object.modifier_apply(modifier=sm.name)
    # billow relief: broad soft swells + finer cauliflower detail, strongest on the upper sides, calmer underneath
    bm = bmesh.new(); bm.from_mesh(ob.data); bm.normal_update()
    sv = Vector((seed * .013, seed * .029, seed * .007))
    sc = 1.0 if kind == "orbit" else 2.6
    for v in bm.verts:
        p = v.co / sc; n = v.normal
        up = max(0.0, n.z)
        big = fbm(p * 1.9, 3, sv) * .085
        fine = (1 - abs(fbm(p * 5.0, 2, sv + Vector((3, 1, 7))))) * .045
        w = .45 + .55 * up
        v.co += n * (big + fine * w) * sc
    bm.normal_update()
    bmesh.ops.smooth_vert(bm, verts=bm.verts, factor=.3, use_axis_x=True, use_axis_y=True, use_axis_z=True)
    # keep the top walkable: billows above the standing height are squashed softly, never sharply
    lid = .16 if kind == "orbit" else .45
    for v in bm.verts:
        dz = v.co.z - top
        if dz > 0: v.co.z = top + lid * math.tanh(dz / lid)
    bm.to_mesh(ob.data); bm.free()
    # budget
    tris = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    dm = ob.modifiers.new("decimate", 'DECIMATE'); dm.ratio = min(1.0, budget / max(1, tris) * .98)
    bpy.ops.object.modifier_apply(modifier=dm.name)
    tm = ob.modifiers.new("tri", 'TRIANGULATE'); bpy.ops.object.modifier_apply(modifier=tm.name)
    keep_largest(ob.data)
    # fit the exact footprint/height contract (pivot = centre of the top)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    xs = [v.co.x for v in bm.verts]; ys = [v.co.y for v in bm.verts]; zs = [v.co.z for v in bm.verts]
    cx, cy = (min(xs) + max(xs)) / 2, (min(ys) + max(ys)) / 2
    fx, fy = ax / ((max(xs) - min(xs)) / 2), ay / ((max(ys) - min(ys)) / 2)
    zmax = max(zs); zmin = min(zs)
    for v in bm.verts:
        v.co.x = (v.co.x - cx) * fx; v.co.y = (v.co.y - cy) * fy
        # bottom maps to `bottom`, the standing plateau stays at `top`
        if v.co.z < top: v.co.z = top + (v.co.z - top) * (top - bottom) / max(.01, top - zmin)
    bm.normal_update()
    # crease (concavity) per vertex, smoothed: 1 = deep fold between two billows
    crease = {}
    for v in bm.verts:
        acc, c = 0.0, 0
        for e in v.link_edges:
            d = e.other_vert(v).co - v.co
            if d.length > 1e-6: acc += v.normal.dot(d.normalized()); c += 1
        crease[v.index] = acc / max(1, c)
    for _ in range(3):
        crease = {v.index: (crease[v.index] + sum(crease[e.other_vert(v).index] for e in v.link_edges)) / (1 + len(v.link_edges)) for v in bm.verts}
    zs = [v.co.z for v in bm.verts]; z0, z1 = min(zs), max(zs)
    verts, norms, cols, index = [], [], [], {}
    for i, v in enumerate(bm.verts):
        index[v.index] = i
        # Unity: x = x, y = z (up), z = y
        verts += [round(v.co.x, 5), round(v.co.z, 5), round(v.co.y, 5)]
        norms += [round(v.normal.x, 4), round(v.normal.z, 4), round(v.normal.y, 4)]
        near = min(range(len(blobs)), key=lambda k: (Vector(blobs[k][0]) - v.co).length / blobs[k][1])
        lobe = random.Random(seed * 1000 + near).random()
        cols += [round((v.co.z - z0) / max(.001, z1 - z0), 3), round(min(1, max(0, crease[v.index] * 3.2)), 3), round(lobe, 3), 1]
    tris = []
    for f in bm.faces:
        a, b, c = [index[x.index] for x in f.verts]
        tris += [a, c, b]                         # handedness flip
    info = dict(name=name, kind=kind, seed=seed, vertices=verts, normals=norms, colors=cols, triangles=tris,
                tris=len(tris) // 3)
    bm.free()
    return ob, info


def render_preview(objs):
    sc = bpy.context.scene
    try: sc.render.engine = 'BLENDER_EEVEE_NEXT'
    except Exception: sc.render.engine = 'BLENDER_EEVEE'
    sc.render.resolution_x, sc.render.resolution_y = 1600, 900
    mat = bpy.data.materials.new("cloud"); mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]; bsdf.inputs["Base Color"].default_value = (1, .9, .97, 1)
    bsdf.inputs["Roughness"].default_value = .8
    for o in objs:
        o.data.materials.append(mat)
        for p in o.data.polygons: p.use_smooth = True
    world = bpy.data.worlds.new("w"); sc.world = world; world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (.5, .42, .65, 1)
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", 'SUN')); sc.collection.objects.link(sun)
    sun.rotation_euler = (math.radians(50), 0, math.radians(30)); sun.data.energy = 3.5
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam")); sc.collection.objects.link(cam); sc.camera = cam
    for group, prefix, dx, dy, row, lens in (("orbit", "Orbit", 4.0, 3.4, 4, 50), ("outer", "Outer", 12.5, 7.5, 3, 50)):
        for o in objs: o.hide_render = not o.name.startswith(prefix)
        sel = [o for o in objs if o.name.startswith(prefix)]
        for i, o in enumerate(sel): o.location = ((i % row) * dx - (row - 1) * dx / 2, -(i // row) * dy, 0)
        w = row * dx
        cam.location = (0, -dy * .5 - w * 1.05, w * .62); cam.rotation_euler = (math.radians(62), 0, 0); cam.data.lens = lens
        sc.render.filepath = os.path.join(PREVIEW, "blender-%s.png" % group)
        bpy.ops.render.render(write_still=True)
        cam.location = (0, -dy * .5, w * 1.15); cam.rotation_euler = (0, 0, 0)
        sc.render.filepath = os.path.join(PREVIEW, "blender-%s-top.png" % group)
        bpy.ops.render.render(write_still=True)


reset()
out, objs = [], []
for name, kind, seed, ax, ay, top, bottom, budget in SPECS:
    ob, blobs = build_lobes(name, kind, seed, ax, ay, top, bottom)
    ob, info = finish(ob, name, kind, seed, ax, ay, top, bottom, budget, blobs)
    ob.name = name.replace(" ", "")
    out.append(info); objs.append(ob)
    print("CLOUD", name, "tris", info["tris"], "verts", len(info["vertices"]) // 3)
with open(os.path.join(OUT, "cloud_redo.json"), "w") as f:
    json.dump(out, f, separators=(",", ":"))
render_preview(objs)
print("CLOUD_REDO_DONE", len(out))
