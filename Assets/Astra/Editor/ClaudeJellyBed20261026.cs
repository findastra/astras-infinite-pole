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
