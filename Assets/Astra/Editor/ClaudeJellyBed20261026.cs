// Claude, 2026-10-26. Astra's first squishy jelly bed, made with DynamicMochiMochiShader (OmochiNobiru, MIT, Booth 3129610).
//   Jelly bed 1 survey: lists the sun lights (the jelly needs one that casts shadows), the spawn and the lounge's sleepy nook,
//                       with their walkable colliders, and photographs them.
//   Jelly bed 2 place:  puts the package's MochiMochiJelly (5 x 5 x 1.3 m) on its own floor in the first clear spot beside the
//                       sleepy nook, with a shadow-only sun the jelly needs, and photographs it. Running it again replaces it.
//   Jelly bed 3 remove: takes the whole bed out again (one Undo step).
// Reports and photos go to Review/jelly-bed-20261026/ (private, not in git).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class ClaudeJellyBed20261026
{
    const string Out = "Review/jelly-bed-20261026";

    [MenuItem("Astra/Claude/Jelly bed 1 survey")]
    public static void Survey()
    {
        Directory.CreateDirectory(Out);
        var rep = new List<string> { "# Jelly bed survey " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "", "## Lights" };
        foreach (var l in Object.FindObjectsOfType<Light>(true))
            rep.Add("- `" + PathOf(l.transform) + "` " + l.type + " active=" + l.gameObject.activeInHierarchy + " enabled=" + l.enabled +
                    " shadows=" + l.shadows + " mode=" + l.lightmapBakeType + " mask=" + l.cullingMask + " intensity=" + l.intensity);

        rep.Add(""); rep.Add("## Places");
        foreach (var name in new[] { "Spawn - facing pole", "Sleepy nook", "13 - Cloud lounge (Claude)", "Invisible platform" })
        {
            var go = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == name);
            if (go == null) { rep.Add("- " + name + ": not found"); continue; }
            rep.Add("- `" + PathOf(go) + "` at " + go.position.ToString("0.00") + " active=" + go.gameObject.activeInHierarchy);
            var rs = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToList();
            if (rs.Count > 0)
            {
                var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                rep.Add("  - renderer bounds centre " + b.center.ToString("0.00") + " size " + b.size.ToString("0.00"));
                Shot(Safe(name), b);
            }
            foreach (var c in go.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).Take(30))
                rep.Add("  - collider `" + PathOf(c.transform) + "` " + c.GetType().Name + " bounds " + c.bounds.center.ToString("0.00") + " size " + c.bounds.size.ToString("0.00") + " layer " + LayerMask.LayerToName(c.gameObject.layer));
        }

        // Every big flat non-moving walkable box the world has, so a 5 x 5 m bed can be put on a real floor.
        rep.Add(""); rep.Add("## Big flat floors (>= 6 x 6 m, < 1 m thick)");
        foreach (var c in Object.FindObjectsOfType<BoxCollider>().Where(c => c.enabled && !c.isTrigger && c.gameObject.activeInHierarchy))
        {
            var s = c.bounds.size;
            if (s.x >= 6 && s.z >= 6 && s.y < 1) rep.Add("- `" + PathOf(c.transform) + "` top y=" + c.bounds.max.y.ToString("0.00") + " centre " + c.bounds.center.ToString("0.0") + " size " + s.ToString("0.0"));
        }
        File.WriteAllText(Path.Combine(Out, "survey.md"), string.Join("\n", rep) + "\n");
    }

    // Round 2 (Astra, 2026-10-27): booths back to neon pink, the bed in a cloud cup in front of the theatre, rainbow chairs.
    [MenuItem("Astra/Claude/Jelly bed 4 survey theatre")]
    public static void SurveyTheatre()
    {
        Directory.CreateDirectory(Out);
        var rx = new System.Text.RegularExpressions.Regex(@"(?i)(theat|cinema|screen|movie|stage|^1\d - |booth|armchair|^sofa$|chair|seat|station)");
        var rep = new List<string> { "# Theatre survey " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "" };
        foreach (var t in Object.FindObjectsOfType<Transform>(true).Where(t => rx.IsMatch(t.name)).OrderBy(t => PathOf(t)).Take(260))
        {
            var rs = t.GetComponentsInChildren<Renderer>().Where(r => r.enabled).ToList();
            string bounds = "";
            if (rs.Count > 0) { var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); bounds = " centre " + b.center.ToString("0.0") + " size " + b.size.ToString("0.0"); }
            var comps = string.Join(",", t.GetComponents<Component>().Where(c => c && !(c is Transform)).Select(c => c.GetType().Name));
            var mats = t.GetComponent<Renderer>() ? " mats " + string.Join(",", t.GetComponent<Renderer>().sharedMaterials.Where(m => m).Select(m => m.name + "(" + m.shader.name + ")")) : "";
            rep.Add("- `" + PathOf(t) + "` active=" + t.gameObject.activeInHierarchy + bounds + " [" + comps + "]" + mats);
        }
        rep.Add(""); rep.Add("## Old booth body colours");
        foreach (var r in Object.FindObjectsOfType<Renderer>(true).Where(r => r.name == "Booth body" || r.name == "Booth sign glow"))
            foreach (var m in r.sharedMaterials.Where(m => m))
                rep.Add("- `" + PathOf(r.transform) + "` " + m.name + " (" + m.shader.name + ") " + string.Join(" ", new[] { "_Color", "_EmissionColor", "_TintColor", "_MainColor", "_GlowColor" }.Where(m.HasProperty).Select(p => p + "=" + m.GetColor(p))));
        var K6 = Object.FindObjectsOfType<Renderer>().Where(r => PathOf(r.transform).Contains("K6")).SelectMany(r => r.sharedMaterials).Where(m => m).Distinct();
        rep.Add(""); rep.Add("## K6 materials");
        foreach (var m in K6) rep.Add("- " + m.name + " `" + AssetDatabase.GetAssetPath(m) + "` (" + m.shader.name + ")" + (m.HasProperty("_Color") ? " _Color=" + m.GetColor("_Color") : ""));
        File.WriteAllText(Path.Combine(Out, "theatre-survey.md"), string.Join("\n", rep) + "\n");
    }

    [MenuItem("Astra/Claude/Jelly bed 5 survey cinema")]
    public static void SurveyCinema()
    {
        Directory.CreateDirectory(Out);
        Physics.SyncTransforms();
        var rep = new List<string> { "# Cinema survey " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "" };
        var scr = Object.FindObjectsOfType<Transform>().First(t => t.name == "VideoScreen");
        var mf = scr.GetComponent<MeshFilter>();
        rep.Add("- screen pos " + scr.position.ToString("0.00") + " fwd " + scr.forward.ToString("0.00") + " up " + scr.up.ToString("0.00") + " right " + scr.right.ToString("0.00") + " lossy " + scr.lossyScale.ToString("0.00") + " mesh " + mf.sharedMesh.name + " local bounds " + mf.sharedMesh.bounds.size.ToString("0.00"));
        if (mf.sharedMesh.normals.Length > 0) rep.Add("- mesh normal[0] world " + scr.TransformDirection(mf.sharedMesh.normals[0]).ToString("0.00"));
        foreach (var n in new[] { "Cloud keep-clear - video screen", "Screen cloud kill zone", "Spawn - facing pole" })
        {
            var t = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(x => x.name == n);
            if (t) rep.Add("- `" + n + "` pos " + t.position.ToString("0.00") + " scale " + t.lossyScale.ToString("0.0") + " rot " + t.eulerAngles.ToString("0") + (t.GetComponent<BoxCollider>() ? " box " + t.GetComponent<BoxCollider>().bounds.size.ToString("0.0") : ""));
        }
        // Down-rays on a grid in front of the screen face: where can people stand and look at it.
        var normal = mf.sharedMesh.normals.Length > 0 ? scr.TransformDirection(mf.sharedMesh.normals[0]).normalized : scr.forward;
        foreach (var side in new[] { 1f, -1f })
        {
            rep.Add(""); rep.Add("## Floors along normal x " + side);
            var flat = new Vector3(normal.x, 0, normal.z).normalized * side;
            var right = Vector3.Cross(Vector3.up, flat);
            for (float d = 6; d <= 40; d += 4)
            {
                var row = new List<string>();
                for (float s = -12; s <= 12; s += 6)
                {
                    var o = scr.position + flat * d + right * s + Vector3.up * 30;
                    row.Add(Physics.Raycast(o, Vector3.down, out var hit, 80, ~0, QueryTriggerInteraction.Ignore) ? hit.point.y.ToString("0.0") + "(" + hit.collider.name + ")" : "-");
                }
                rep.Add("- d " + d + ": " + string.Join(" | ", row));
            }
        }
        var b = scr.GetComponent<Renderer>().bounds;
        Shot("cinema", new Bounds(b.center, b.size * 1.3f));
        File.WriteAllText(Path.Combine(Out, "cinema-survey.md"), string.Join("\n", rep) + "\n");
    }

    const string CinemaRoot = "Cinema cloud cup and rainbow chairs (Claude)";

    // Round 2 apply. Running it again rebuilds the cup and chairs and re-places the bed (the booth paint swap is idempotent).
    [MenuItem("Astra/Claude/Jelly bed 6 cinema round")]
    public static void CinemaRound()
    {
        Directory.CreateDirectory(Out);
        Physics.SyncTransforms();
        var rep = new List<string> { "# Jelly bed round 2 " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "" };
        BoothsNeonPink(rep);

        var bed = GameObject.Find(RootName);
        var scr = Object.FindObjectsOfType<Transform>().FirstOrDefault(t => t.name == "VideoScreen");
        var glass = AssetDatabase.LoadAssetAtPath<Material>("Assets/LiminalDoors/Booth/Materials/Booth glass (pink).mat");
        var puff = Object.FindObjectsOfType<MeshFilter>().FirstOrDefault(m => m.name == "Cloud seat" && m.transform.parent && m.transform.parent.name == "Tiny cloud chair");
        var arm = Object.FindObjectsOfType<Transform>().FirstOrDefault(t => t.name == "Armchair" && PathOf(t).Contains("Tea lounge"));
        var chairPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VRCChair/VRCChair3.prefab");
        if (!bed || !scr || !glass || !puff || !arm || !chairPrefab)
        { rep.Add("FAIL: missing " + (bed ? "" : "bed ") + (scr ? "" : "screen ") + (glass ? "" : "glass ") + (puff ? "" : "puff ") + (arm ? "" : "armchair ") + (chairPrefab ? "" : "VRCChair3")); Write(rep, "cinema-round.md"); return; }
        var oldRoot = GameObject.Find(CinemaRoot);
        if (oldRoot) Undo.DestroyObjectImmediate(oldRoot);

        // Frame: origin on the ground 11 m out from the screen toward the pole; +z points away from the screen.
        var n = scr.TransformDirection(scr.GetComponent<MeshFilter>().sharedMesh.normals[0]); n.y = 0; n.Normalize();
        var origin = new Vector3(scr.position.x, 0, scr.position.z) + n * 11f;
        if (Physics.Raycast(origin + Vector3.up * 3, Vector3.down, out var g, 10, ~0, QueryTriggerInteraction.Ignore)) origin.y = g.point.y;
        var frame = Quaternion.LookRotation(n);
        var root = new GameObject(CinemaRoot);
        Undo.RegisterCreatedObjectUndo(root, "Cinema round");
        root.transform.SetPositionAndRotation(origin, frame);
        rep.Add("- deck origin " + origin.ToString("0.00") + ", 11 m in front of the screen, facing away from it");

        // The bed moves into the cup, its open face toward the screen.
        Undo.RecordObject(bed.transform, "Move bed");
        bed.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(-n));
        JellyBlue(bed, rep);

        // Cloud cup: a rounded-square ring of glass puffs hugging the 5 x 5 m jelly, a little taller than it.
        var cup = new GameObject("Cloud cup (pink glass)"); cup.transform.SetParent(root.transform, false);
        var rnd = new System.Random(20261027);
        int puffs = 0;
        for (float a = 0; a < 360; a += 20f)
        {
            float r = Mathf.Deg2Rad * a, cx = Mathf.Cos(r), sz = Mathf.Sin(r);
            float k = 1f / Mathf.Pow(Mathf.Pow(Mathf.Abs(cx), 4) + Mathf.Pow(Mathf.Abs(sz), 4), 0.25f); // squircle
            var p = new Vector3(cx, 0, sz) * (Half + 0.55f) * k;
            for (int layer = 0; layer < 2; layer++)
            {
                var go = new GameObject("Cup puff " + (++puffs));
                go.transform.SetParent(cup.transform, false);
                float s = 1.5f + (float)rnd.NextDouble() * 0.7f;
                go.transform.localPosition = p + Vector3.up * (0.35f + layer * 0.75f);
                go.transform.localRotation = Quaternion.Euler(0, a + (float)rnd.NextDouble() * 40f, 0);
                go.transform.localScale = new Vector3(s, s * (layer == 0 ? 1.4f : 1.2f), s) / Mathf.Max(puff.sharedMesh.bounds.size.x, 0.01f) * 1.1f;
                go.AddComponent<MeshFilter>().sharedMesh = puff.sharedMesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = glass;
            }
        }
        // The cup wall you bump into: four thin boxes just outside the jelly, 1.2 m high, with a gap on the screen side to walk in.
        foreach (var w in new[] { (new Vector3(0, 0.6f, Half + 0.4f), new Vector3(Half * 2 + 1.6f, 1.2f, 0.3f)),
                                  (new Vector3(-(Half + 0.4f), 0.6f, 0), new Vector3(0.3f, 1.2f, Half * 2 + 1.6f)),
                                  (new Vector3(Half + 0.4f, 0.6f, 0), new Vector3(0.3f, 1.2f, Half * 2 + 1.6f)) })
        {
            var wall = new GameObject("Cup wall"); wall.transform.SetParent(cup.transform, false);
            var bc = wall.AddComponent<BoxCollider>(); bc.center = w.Item1; bc.size = w.Item2;
        }
        rep.Add("- cup: " + puffs + " pink-glass cloud puffs (mesh `" + puff.sharedMesh.name + "`), 3 walls, open toward the screen");

        // Seven rainbow cloud armchairs, loosely scattered behind and beside the cup, all turned roughly to the screen.
        var seats = new (string name, Color top, Vector2 at, float scale, float turn)[] {
            ("Red",    new Color(1.30f, 0.18f, 0.22f), new Vector2(-7.4f, -0.8f), 1.15f, -12f),
            ("Orange", new Color(1.35f, 0.55f, 0.12f), new Vector2(-5.6f,  3.9f), 0.85f,   9f),
            ("Yellow", new Color(1.35f, 1.15f, 0.20f), new Vector2(-2.1f,  6.6f), 1.30f,  -6f),
            ("Green",  new Color(0.30f, 1.20f, 0.40f), new Vector2( 1.7f,  5.5f), 0.90f,  14f),
            ("Blue",   new Color(0.25f, 0.55f, 1.40f), new Vector2( 5.1f,  7.1f), 1.20f,  -8f),
            ("Purple", new Color(0.80f, 0.30f, 1.35f), new Vector2( 7.6f,  2.6f), 0.80f,  18f),
            ("Pink",   new Color(1.30f, 0.42f, 0.82f), new Vector2( 6.6f, -2.0f), 1.05f,  -4f) };
        var chairs = new GameObject("Rainbow chairs"); chairs.transform.SetParent(root.transform, false);
        var moving = MovingRenderers();
        foreach (var s in seats)
        {
            var c = Object.Instantiate(arm.gameObject, chairs.transform, true); // keep its world size ("Furniture 2x" is scaled)
            c.name = s.name + " cloud chair";
            // On the ground (low orbit clouds sit on top of it), nudged up to 3 m until no orbiting cloud's path crosses it.
            var world = root.transform.TransformPoint(new Vector3(s.at.x, 0, s.at.y));
            world.y = origin.y;
            var probe = new Vector3(2.6f, 3f, 2.6f) * s.scale;
            Vector3 found = world; bool ok = false;
            for (int step = 0; step <= 24 && !ok; step++)
            {
                var tryAt = world + Quaternion.Euler(0, step * 47f, 0) * Vector3.forward * (step == 0 ? 0 : 0.5f + step * 0.11f);
                if (!moving.Any(m => m.Intersects(new Bounds(tryAt + Vector3.up * probe.y / 2, probe)))) { found = tryAt; ok = true; }
            }
            world = found;
            // Which way the armchair faces: from its back toward its seat.
            var seatC = c.GetComponentsInChildren<BoxCollider>().FirstOrDefault(b => b.name == "Cloud seat collider");
            var backC = c.GetComponentsInChildren<BoxCollider>().FirstOrDefault(b => b.name == "Cloud back collider");
            var face = seatC && backC ? seatC.bounds.center - backC.bounds.center : c.transform.forward; face.y = 0;
            var toScreen = -n;
            float yaw = Vector3.SignedAngle(face.normalized, Quaternion.Euler(0, s.turn, 0) * toScreen, Vector3.up);
            c.transform.Rotate(0, yaw, 0, Space.World);
            c.transform.localScale = c.transform.localScale * s.scale;
            // Sit its lowest renderer on the floor.
            var rs = c.GetComponentsInChildren<Renderer>();
            var b0 = rs[0].bounds; foreach (var r in rs) b0.Encapsulate(r.bounds);
            c.transform.position += new Vector3(world.x - b0.center.x, world.y - b0.min.y, world.z - b0.center.z);
            var mat = RainbowCloud(s.name, s.top);
            foreach (var r in rs) r.sharedMaterials = r.sharedMaterials.Select(_ => mat).ToArray();

            // A real seat: the SDK's VRCChair3 (station + click-to-sit), its own chair mesh hidden, on top of the cloud seat.
            Physics.SyncTransforms();
            seatC = c.GetComponentsInChildren<BoxCollider>().FirstOrDefault(b => b.name == "Cloud seat collider");
            var st = (GameObject)PrefabUtility.InstantiatePrefab(chairPrefab, c.transform);
            st.name = "Sit here (VRCChair3)";
            var top = seatC ? new Vector3(seatC.bounds.center.x, seatC.bounds.max.y, seatC.bounds.center.z) : c.transform.position;
            if (seatC && backC)
            {
                Physics.SyncTransforms();
                var f2 = seatC.bounds.center - c.GetComponentsInChildren<BoxCollider>().First(b => b.name == "Cloud back collider").bounds.center; f2.y = 0;
                st.transform.rotation = Quaternion.LookRotation(f2.normalized);
            }
            st.transform.position = top; st.transform.localScale = Vector3.one / Mathf.Max(c.transform.lossyScale.x, 0.01f);
            foreach (var r in st.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            bool clear = ok;
            rep.Add("- " + s.name + ": at " + world.ToString("0.0") + " scale " + s.scale + ", seat top y " + top.y.ToString("0.00") + (clear ? "" : "  WARN: an orbiting cloud passes here"));
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        var view = new Bounds(origin + Vector3.up * 1.5f, new Vector3(20, 4, 16));
        Shot("round2-deck", view);
        ShotFrom("round2-from-pole", view.center, view.center + n * 16 + Vector3.up * 5);
        ShotFrom("round2-from-screen", view.center, view.center - n * 14 + Vector3.up * 7);
        ShotFrom("round2-cup-close", origin + Vector3.up * 0.8f, origin + Quaternion.Euler(0, 40, 0) * n * 6 + Vector3.up * 3.5f);
        var booth = Object.FindObjectsOfType<Transform>().FirstOrDefault(t => t.name.StartsWith("K6 booth") && PathOf(t).StartsWith("16 - Banner"));
        if (booth)
        {
            var bb = booth.GetComponentsInChildren<Renderer>()[0].bounds; foreach (var r in booth.GetComponentsInChildren<Renderer>()) bb.Encapsulate(r.bounds);
            ShotFrom("round2-banner-booth", bb.center, bb.center + booth.forward * 5.5f + booth.right * 2.5f + Vector3.up * 0.8f);
            ShotFrom("round2-banner-booth-back", bb.center, bb.center - booth.forward * 5.5f - booth.right * 2.5f + Vector3.up * 0.8f);
        }
        Write(rep, "cinema-round.md");
    }

    // Astra, 2026-10-27: "just change the pink to the neon pink in my VRChat banner" (banner-booth.jpg, measured #F61A7E),
    // and the gold hinges and crowns silver and sparkling. World-only copies of the K6 textures; the library's files stay as
    // they are. Pink pixels take the banner pink at their own brightness; gold pixels turn bright silver metal, with tiny
    // glints in the glow map; the gold sign glow turns silver-white. Nothing else glows.
    static readonly Color BannerPink = new Color(246 / 255f, 26 / 255f, 126 / 255f);
    const string BoothTex = "Assets/LiminalDoors/Booth/Textures/", NeonDir = "Assets/Astra/Textures/Booth neon pink/";

    static void BoothsNeonPink(List<string> rep)
    {
        const string src = "Assets/LiminalDoors/Booth/Materials/Booth.mat", dst = "Assets/Astra/Materials/Booth neon pink (Pole).mat";
        var orig = AssetDatabase.LoadAssetAtPath<Material>(src);
        if (!orig) { rep.Add("- booths: no " + src); return; }
        Directory.CreateDirectory(NeonDir);
        var alb = Load(BoothTex + "booth-albedo.png"); var emi = Load(BoothTex + "booth-emission.png"); var met = Load(BoothTex + "booth-metallic-smoothness.png");
        var a = alb.GetPixels(); var e = emi.GetPixels(); var mm = met.GetPixels();
        // Reference brightness of the pink paint, so the median pink lands exactly on the banner pink.
        var pinkV = a.Where(IsPink).Select(c => { Color.RGBToHSV(c, out _, out _, out var v); return v; }).OrderBy(v => v).ToList();
        float refV = pinkV.Count > 0 ? pinkV[pinkV.Count / 2] : 1f;
        // The world's warm sunset light and colour grading eat blue (a texture of exactly #F61A7E renders #FF1148), so the
        // texture's pink carries extra blue to land on the banner pink on screen.
        Color.RGBToHSV(new Color(BannerPink.r * 0.94f, BannerPink.g, Mathf.Min(1f, BannerPink.b * 1.25f)), out var ph, out var ps, out var pv);
        var rnd = new System.Random(246026126);
        int pink = 0, gold = 0;
        for (int i = 0; i < a.Length; i++)
        {
            var c = a[i];
            if (IsGold(c))
            {
                Color.RGBToHSV(c, out _, out _, out var v);
                float g = Mathf.Clamp01(0.55f + v * 0.45f);
                a[i] = new Color(g * 0.96f, g * 0.97f, g, c.a);
                mm[i] = new Color(1f, mm[i].g, mm[i].b, 0.93f);                       // metal, very smooth
                float glint = rnd.NextDouble() < 0.06 ? 1.6f : 0.12f;                   // scattered sparkles
                e[i] = new Color(glint * 0.9f, glint * 0.93f, glint, 1f);
                gold++;
            }
            else if (IsPink(c))
            {
                Color.RGBToHSV(c, out _, out _, out var v);
                a[i] = Color.HSVToRGB(ph, ps, Mathf.Clamp01(pv * v / refV)); a[i].a = c.a;
                // This world's light is dim (sun 0.55), so the pink also glows a little in its own colour, the way the old
                // cloud booths did. The map is sRGB, so 0.7 here is about 0.45 in light, x 2 from the material's emission colour.
                if (e[i].r < 0.05f) e[i] = new Color(a[i].r * 0.7f, a[i].g * 0.7f, a[i].b * 0.7f, 1f);
                pink++;
            }
            if (e[i].r > 0.05f && IsGold(e[i])) { Color.RGBToHSV(e[i], out _, out _, out var ev); e[i] = new Color(ev, ev, ev * 1.04f, 1f); }
        }
        Save(alb, a, NeonDir + "booth-albedo-neon-pink.png", BoothTex + "booth-albedo.png");
        Save(emi, e, NeonDir + "booth-emission-silver.png", BoothTex + "booth-emission.png");
        Save(met, mm, NeonDir + "booth-metallic-smoothness-silver.png", BoothTex + "booth-metallic-smoothness.png");

        var m = AssetDatabase.LoadAssetAtPath<Material>(dst);
        if (!m) { m = new Material(orig); AssetDatabase.CreateAsset(m, dst); }
        m.CopyPropertiesFromMaterial(orig);
        m.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>(NeonDir + "booth-albedo-neon-pink.png"));
        m.SetTexture("_EmissionMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NeonDir + "booth-emission-silver.png"));
        m.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(NeonDir + "booth-metallic-smoothness-silver.png"));
        m.SetFloat("_Saturation", 1f);
        EditorUtility.SetDirty(m);
        int swapped = 0;
        foreach (var r in Object.FindObjectsOfType<Renderer>(true))
        {
            var ms = r.sharedMaterials;
            bool hit = false;
            for (int i = 0; i < ms.Length; i++) if (ms[i] == orig) { ms[i] = m; hit = true; }
            if (hit) { Undo.RecordObject(r, "Neon booths"); r.sharedMaterials = ms; swapped++; }
        }
        AssetDatabase.SaveAssets();
        rep.Add("- booths: " + swapped + " more renderers on `" + dst + "`; " + pink + " pink pixels to banner pink #F61A7E (ref value " + refV.ToString("0.00") + "), " + gold + " gold pixels to sparkly silver");
    }

    static bool IsGold(Color c) { Color.RGBToHSV(c, out var h, out var s, out var v); return h > 0.06f && h < 0.17f && s > 0.18f && v > 0.25f; }
    static bool IsPink(Color c) { Color.RGBToHSV(c, out var h, out var s, out _); return (h > 0.82f || h < 0.02f) && s > 0.2f; }

    static Texture2D Load(string path)
    {
        var t = new Texture2D(2, 2, TextureFormat.RGBA32, false, AssetImporter.GetAtPath(path) is TextureImporter ti && !ti.sRGBTexture);
        t.LoadImage(File.ReadAllBytes(path));
        return t;
    }

    static void Save(Texture2D t, Color[] px, string path, string likePath)
    {
        t.SetPixels(px); t.Apply();
        File.WriteAllBytes(path, t.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        var like = (TextureImporter)AssetImporter.GetAtPath(likePath);
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.sRGBTexture = like.sRGBTexture; ti.maxTextureSize = like.maxTextureSize; ti.mipmapEnabled = like.mipmapEnabled;
        ti.textureCompression = like.textureCompression; ti.alphaSource = like.alphaSource; ti.alphaIsTransparency = like.alphaIsTransparency;
        ti.SaveAndReimport();
    }

    // Astra, 2026-10-27: take the vines off the banner booth (the vine tool gets fixed first; more vines later).
    [MenuItem("Astra/Claude/Jelly bed 8 neon booths, no booth vines")]
    public static void NeonBoothsNoVines()
    {
        Directory.CreateDirectory(Out);
        var rep = new List<string> { "# Neon booths " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "" };
        BoothsNeonPink(rep);
        var vines = Object.FindObjectsOfType<Transform>(true).FirstOrDefault(t => t.name == "Banner booth vines");
        if (vines) { Undo.DestroyObjectImmediate(vines.gameObject); rep.Add("- removed `Banner booth vines` (DJ booth and bench vines stay)"); }
        else rep.Add("- no `Banner booth vines` in the scene");
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        BoothPhotos();
        Write(rep, "neon-booths.md");
    }

    // Selects the jelly bed and the cinema deck and frames them in the Scene view, so Astra can see where they are.
    [MenuItem("Astra/Claude/Jelly bed 9 show the deck")]
    public static void ShowDeck()
    {
        var picks = new[] { GameObject.Find(RootName), GameObject.Find(CinemaRoot) }.Where(g => g).ToArray();
        Selection.objects = picks;
        var sv = SceneView.lastActiveSceneView ?? EditorWindow.GetWindow<SceneView>();
        sv.Focus();
        var b = new Bounds(picks[0].transform.position, Vector3.one);
        foreach (var r in picks.SelectMany(g => g.GetComponentsInChildren<Renderer>())) b.Encapsulate(r.bounds);
        // From the screen's side, looking down at the cup with the chairs around it (the screen is at 18, 15, 14.4).
        var look = b.center - new Vector3(18, b.center.y, 14.4f); look.y = 0;
        sv.LookAt(b.center, Quaternion.LookRotation(look.normalized + Vector3.down * 0.8f), 11f, false, false);
        sv.Repaint();
        EditorGUIUtility.PingObject(picks[0]);
    }

    static void JellyBlue(GameObject bed, List<string> rep)
    {
        const string src = "Assets/DynamicMochiMochi/MochiVolume.mat", dst = "Assets/Astra/Materials/Jelly blue (Pole).mat";
        var orig = AssetDatabase.LoadAssetAtPath<Material>(src);
        var m = AssetDatabase.LoadAssetAtPath<Material>(dst);
        if (!m) { m = new Material(orig); AssetDatabase.CreateAsset(m, dst); }
        m.CopyPropertiesFromMaterial(orig);
        m.SetColor("_Color", new Color(0.12f, 0.55f, 1f, 1f));
        m.SetColor("_FogColor", new Color(0.35f, 0.75f, 1f, 0.22f));
        m.SetFloat("_Transparency", 0.8f);
        EditorUtility.SetDirty(m);
        int n = 0;
        foreach (var r in bed.GetComponentsInChildren<Renderer>(true))
        {
            var ms = r.sharedMaterials;
            for (int i = 0; i < ms.Length; i++) if (ms[i] == orig) { ms[i] = m; n++; }
            r.sharedMaterials = ms;
        }
        rep.Add("- jelly: " + n + " slot(s) now `" + dst + "` (blue, transparency 0.8)");
    }

    static Material RainbowCloud(string name, Color top)
    {
        string path = "Assets/Astra/Materials/Rainbow chair " + name.ToLower() + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m)
        {
            var baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Astra/Materials/Lounge/Lounge Pink.mat");
            m = baseMat ? new Material(baseMat) : new Material(Shader.Find("Astra/Glitter Cloud"));
            AssetDatabase.CreateAsset(m, path);
        }
        m.SetColor("_Top", top);
        m.SetColor("_Bottom", top * 0.62f);
        m.SetColor("_Rim", Color.Lerp(top, Color.white, 0.45f) * 1.3f);
        m.SetFloat("_Pastel", 0f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static void ShotFrom(string name, Vector3 target, Vector3 from)
    {
        var go = new GameObject("Claude photo camera") { hideFlags = HideFlags.DontSave };
        var cam = go.AddComponent<Camera>(); cam.transform.position = from; cam.transform.LookAt(target); cam.fieldOfView = 55; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 3000;
        var pp = go.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform;
        var rt = new RenderTexture(1200, 800, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1200, 800, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
    }

    [MenuItem("Astra/Claude/Jelly bed 7 booth photos")]
    public static void BoothPhotos()
    {
        int k = 0;
        foreach (var r in Object.FindObjectsOfType<Renderer>().Where(r => r.name == "Booth body" && PathOf(r.transform).Contains("K6")))
        {
            k++;
            var c = r.bounds.center;
            foreach (var a in new[] { 0f, 120f, 240f })
                ShotFrom("round2-booth" + k + "-" + a, c, c + Quaternion.Euler(0, a, 0) * r.transform.forward * 3.2f + Vector3.up * 0.6f);
        }
    }

    static void Write(List<string> rep, string file) => File.WriteAllText(Path.Combine(Out, file), string.Join("\n", rep) + "\n");

    const string RootName = "Jelly bed (Claude)", Prefab = "Assets/DynamicMochiMochi/MochiMochiJelly.prefab";
    const float Half = 2.5f, Tall = 1.3f;
    const int ShadowLayer = 31; // unnamed and empty: the shadow sun lights nothing, it only switches on the depth the jelly reads

    [MenuItem("Astra/Claude/Jelly bed 2 place")]
    public static void Place()
    {
        Directory.CreateDirectory(Out);
        var rep = new List<string> { "# Jelly bed place " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "" };
        var old = GameObject.Find(RootName);
        if (old) { Undo.DestroyObjectImmediate(old); rep.Add("- replaced the earlier bed"); }

        var nook = Object.FindObjectsOfType<Transform>().FirstOrDefault(t => t.name == "Sleepy nook");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        if (nook == null || prefab == null) { rep.Add("FAIL: " + (nook ? "" : "no Sleepy nook ") + (prefab ? "" : "no " + Prefab)); Write(rep); return; }
        int onShadowLayer = Object.FindObjectsOfType<Transform>(true).Count(t => t.gameObject.layer == ShadowLayer);
        if (onShadowLayer > 0) { rep.Add("FAIL: " + onShadowLayer + " objects already use layer " + ShadowLayer); Write(rep); return; }

        // The nook's floor height: the most common walk-slab top inside it.
        var slabs = nook.GetComponentsInChildren<BoxCollider>().Where(c => c.enabled && !c.isTrigger && c.name == "Cloud walk collider").ToList();
        float floorY = slabs.Select(c => Mathf.Round(c.bounds.max.y * 10f) / 10f).GroupBy(y => y).OrderByDescending(g => g.Count()).First().Key;
        Physics.SyncTransforms();
        var moving = MovingRenderers();
        var fixedR = Object.FindObjectsOfType<MeshRenderer>().Where(r => r.enabled && r.bounds.size.magnitude < 150f).Select(r => r.bounds).ToList();
        Vector3 c0 = new Vector3(nook.position.x, floorY, nook.position.z), best = Vector3.zero;
        float bestD = float.MaxValue; int tried = 0;
        for (float dx = -22; dx <= 22; dx += 1f)
            for (float dz = -22; dz <= 22; dz += 1f)
            {
                var p = c0 + new Vector3(dx, 0, dz);
                float d = new Vector2(dx, dz).magnitude;
                if (d >= bestD) continue;
                tried++;
                if (Clear(p, moving, fixedR) && Reachable(p)) { best = p; bestD = d; }
            }
        if (bestD == float.MaxValue) { rep.Add("FAIL: no clear 6 x 6 m spot within 22 m of the nook (" + tried + " tried)"); Write(rep); return; }

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Jelly bed");
        root.transform.position = best;
        // Face the bed's open side toward the nook, so it reads as part of the lounge.
        var look = c0 - best; look.y = 0;
        if (look.sqrMagnitude > 0.01f) root.transform.rotation = Quaternion.LookRotation(look);

        var jelly = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
        jelly.transform.localPosition = Vector3.zero; jelly.transform.localRotation = Quaternion.identity;

        // Its own floor: a thin walk slab with its top at the jelly's bottom, plus a glossy deep-blue base you see through the jelly.
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Jelly floor"; floor.transform.SetParent(root.transform, false);
        floor.transform.localPosition = new Vector3(0, -0.05f, 0); floor.transform.localScale = new Vector3(Half * 2 + 0.2f, 0.1f, Half * 2 + 0.2f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = BaseMaterial();
        var walk = floor.GetComponent<BoxCollider>(); walk.size = new Vector3(1, 0.6f, 1); walk.center = new Vector3(0, 0.2f, 0); // 0.06 m thick in world, top flush

        var sun = new GameObject("Jelly shadow sun");
        sun.transform.SetParent(root.transform, false);
        sun.transform.rotation = Quaternion.Euler(80, 0, 0);
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional; l.shadows = LightShadows.Hard; l.intensity = 0.01f; l.cullingMask = 1 << ShadowLayer; l.lightmapBakeType = LightmapBakeType.Realtime;

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(root.scene);
        rep.Add("- placed at " + best.ToString("0.00") + ", " + bestD.ToString("0.0") + " m from the nook centre, floor y " + floorY.ToString("0.00") + " (" + tried + " spots tried)");
        rep.Add("- jelly 5 x 5 x 1.3 m from `" + Prefab + "`; dent camera mask " + jelly.GetComponentsInChildren<Camera>(true).Select(cm => cm.name + "=" + cm.cullingMask).Aggregate((a, b) => a + ", " + b));
        rep.Add("- walk slab top y " + walk.bounds.max.y.ToString("0.000") + ", thickness " + walk.bounds.size.y.ToString("0.000") + " m");
        rep.Add("- shadow sun: hard shadows, intensity 0.01, lights only layer " + ShadowLayer + " (empty)");
        var b = new Bounds(best + Vector3.up * 0.65f, new Vector3(5.2f, 1.4f, 5.2f));
        Shot("bed-near", b); Shot("bed-wide", new Bounds(b.center, b.size * 4f));
        Write(rep);
    }

    [MenuItem("Astra/Claude/Jelly bed 3 remove")]
    public static void Remove()
    {
        var root = GameObject.Find(RootName);
        if (root) Undo.DestroyObjectImmediate(root);
        Directory.CreateDirectory(Out);
        File.WriteAllText(Path.Combine(Out, "remove.txt"), (root ? "removed " : "nothing to remove: no ") + RootName + "\n");
    }

    // No collider and no fixed renderer inside the bed's space (with half a metre to spare), and no orbiting cloud's path through it.
    static bool Clear(Vector3 p, List<Bounds> moving, List<Bounds> fixedR)
    {
        var box = new Bounds(p + Vector3.up * 1.2f, new Vector3(Half * 2 + 1, 2.2f, Half * 2 + 1));
        if (Physics.OverlapBox(box.center, box.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Length > 0) return false;
        foreach (var r in fixedR) if (r.Intersects(box)) return false;
        float rad = new Vector2(p.x, p.z).magnitude;
        foreach (var m in moving)
        {
            float mr = new Vector2(m.center.x, m.center.z).magnitude, reach = Mathf.Max(m.extents.x, m.extents.z) + Half + 0.5f;
            if (Mathf.Abs(mr - rad) < reach && m.max.y > box.min.y && m.min.y < box.max.y) return false;
        }
        return true;
    }

    // Some walkable top at the same height within 1.5 m of the bed's edge, so you can step straight onto it.
    static bool Reachable(Vector3 p)
    {
        var ring = Physics.OverlapBox(p, new Vector3(Half + 1.5f, 0.4f, Half + 1.5f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        return ring.Any(c => Mathf.Abs(c.bounds.max.y - p.y) < 0.35f);
    }

    // Everything AstraCloudOrbit spins (its pivots and clouds) and the duck, as world bounds.
    static List<Bounds> MovingRenderers()
    {
        var roots = new List<Transform>();
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>().Where(m => m))
        {
            var type = mb.GetType().Name;
            if (type == "AstraDuckFloat") roots.Add(mb.transform);
            if (type != "AstraCloudOrbit") continue;
            foreach (var f in mb.GetType().GetFields())
                if (f.FieldType == typeof(Transform[]) && (f.Name == "pivots" || f.Name == "clouds"))
                    roots.AddRange(((Transform[])f.GetValue(mb) ?? new Transform[0]).Where(x => x));
        }
        return roots.Distinct().SelectMany(t => t.GetComponentsInChildren<Renderer>()).Where(r => r.enabled && r.bounds.size.magnitude < 150f).Select(r => r.bounds).ToList();
    }

    static Material BaseMaterial()
    {
        const string path = "Assets/Astra/Materials/Jelly bed base.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m) return m;
        Directory.CreateDirectory("Assets/Astra/Materials");
        m = new Material(Shader.Find("Standard")) { name = "Jelly bed base", enableInstancing = true };
        m.SetColor("_Color", new Color(0.05f, 0.18f, 0.55f));
        m.SetFloat("_Glossiness", 0.9f);
        m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.0f, 0.35f, 0.9f) * 0.6f);
        AssetDatabase.CreateAsset(m, path);
        return m;
    }

    static void Write(List<string> rep) => File.WriteAllText(Path.Combine(Out, "place.md"), string.Join("\n", rep) + "\n");

    static void Shot(string name, Bounds b)
    {
        float r = Mathf.Max(b.extents.magnitude, 0.6f);
        var pos = b.center + new Vector3(1f, 0.45f, 1f).normalized * (r / Mathf.Sin(25f * Mathf.Deg2Rad) * 1.05f);
        var go = new GameObject("Claude photo camera") { hideFlags = HideFlags.DontSave };
        var cam = go.AddComponent<Camera>(); cam.transform.position = pos; cam.transform.LookAt(b.center); cam.fieldOfView = 50; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 3000;
        var pp = go.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.PostProcessing.PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform;
        var rt = new RenderTexture(1200, 800, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 4 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(1200, 800, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, 1200, 800), 0, 0); tex.Apply();
        File.WriteAllBytes(Path.Combine(Out, name + ".png"), tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(tex); Object.DestroyImmediate(go);
    }

    static string PathOf(Transform t) => t.parent ? PathOf(t.parent) + "/" + t.name : t.name;

    static string Safe(string s) => System.Text.RegularExpressions.Regex.Replace(s, @"[^A-Za-z0-9 _\-]", "").Trim();
}
