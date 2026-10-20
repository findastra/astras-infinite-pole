// Claude, 2026-10-19. Trying Unity Unicorn's vine-spawn-with-trumpet-flowers in this world through the agent queue.
//   Vines 1 survey: lists places vines could grow (meshes that don't orbit) and photographs each one.
//   Vines 2 grow:   grows on the places Astra picked (Review/vines-20261019/pick.txt) with the page's newest job,
//                   checks that one Undo removes them (then redoes), and photographs before and after.
//   Vines 3 remove: deletes the grown vines (one Undo step), for when Astra doesn't want them.
//   Vines 4 close-ups: two close photos of each vine mesh.
//   Vines 5 regrow brighter: takes the first Grow out (and its files) and grows again with Flower glow 1.5 and light leaves.
// Reports and photos go to Review/vines-20261019/ (private, not in git).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityUnicorn;
using Object = UnityEngine.Object;

public static class ClaudeVines20261019
{
    const string Out = "Review/vines-20261019", RootName = "Vines with trumpet flowers (Unity Unicorn)";
    static readonly Regex Want = new Regex(@"(?i)(45mm|spiral stairs|banner booth|^dj booth$|deck console|^bench$|^phone booth [12]$|K6 booth|rail|arch|column|pillar|fence|trellis|lamp|pergola|gate|planter)");

    [MenuItem("Astra/Claude/Vines 1 survey")]
    public static void Survey()
    {
        Directory.CreateDirectory(Out);
        var moving = MovingRoots();
        var all = Object.FindObjectsOfType<Transform>().Where(t => t.gameObject.activeInHierarchy && Want.IsMatch(t.name)).ToList();
        var picked = all.Where(t => !all.Any(o => o != t && t.IsChildOf(o))).Take(16).ToList();
        var rep = new List<string> { "# Vine spots in Astra's Infinite Pole (survey " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm") + ")", "",
            "| # | object | size (m) | meshes | triangles | moves at runtime |", "|---|---|---|---|---|---|" };
        int k = 0;
        foreach (var t in picked)
        {
            var mfs = Meshes(t.gameObject);
            if (mfs.Count == 0) continue;
            k++;
            var b = BoundsOf(mfs);
            bool moves = moving.Any(m => t.IsChildOf(m));
            int tris = mfs.Sum(m => (int)(m.sharedMesh.triangles.Length / 3));
            rep.Add("| " + k + " | `" + PathOf(t) + "` | " + b.size.x.ToString("0.0") + " x " + b.size.y.ToString("0.0") + " x " + b.size.z.ToString("0.0") +
                    " | " + mfs.Count + " | " + tris + " | " + (moves ? "yes (orbits; vines would stay behind)" : "no") + " |");
            Shot(k.ToString("00") + " " + Safe(t.name), Frame(b));
        }
        File.WriteAllText(Path.Combine(Out, "survey.md"), string.Join("\n", rep) + "\n");
    }

    [MenuItem("Astra/Claude/Vines 2 grow")]
    public static void GrowPicked() => GrowPicked(null, null, "grow");

    // Astra, after the first Grow came out nearly black under the Pole's light: "Regrow them brighter".
    // Takes out the first Grow and its mesh files, then grows on the same picks with Flower glow 1.5 and light leaves.
    [MenuItem("Astra/Claude/Vines 5 regrow brighter")]
    public static void RegrowBrighter()
    {
        var old = GameObject.Find(RootName);
        if (old)
        {
            var files = old.GetComponentsInChildren<MeshFilter>().Select(m => AssetDatabase.GetAssetPath(m.sharedMesh))
                .Concat(old.GetComponentsInChildren<MeshRenderer>().Select(r => AssetDatabase.GetAssetPath(r.sharedMaterial)))
                .Where(p => p.StartsWith("Assets/UnityUnicorn/Generated/Vines/")).Distinct().ToList();
            Undo.DestroyObjectImmediate(old);
            foreach (var f in files) AssetDatabase.DeleteAsset(f);
        }
        GrowPicked(1.5f, "#8fe388", "bright");
    }

    static void GrowPicked(float? glow, string leafHex, string tag)
    {
        Directory.CreateDirectory(Out);
        var rep = new List<string>();
        try
        {
            var targets = new List<GameObject>();
            foreach (var line in File.ReadAllLines(Path.Combine(Out, "pick.txt")).Select(l => l.Trim()).Where(l => l.Length > 0 && !l.StartsWith("#")))
            {
                var go = FindPath(line);
                if (go == null) rep.Add("NOT FOUND " + line); else targets.Add(go);
            }
            if (targets.Count == 0) throw new System.InvalidOperationException("Nothing to grow on: pick.txt names no object in the open scene.");
            var jobFile = new DirectoryInfo("UnityUnicornReports/jobs").GetFiles("unity-unicorn-vine-spawn-*.json").OrderByDescending(f => f.LastWriteTimeUtc).First();
            if (!VineJobs20261019.TryRead(jobFile.FullName, out var job, out var why)) throw new System.InvalidOperationException("Job unreadable: " + why);
            var settings = VineJobs20261019.Settings(job);
            var colours = VineJobs20261019.Colours(job);
            var changed = new List<string>();
            if (glow.HasValue) { settings.glow = glow.Value; changed.Add("Flower glow " + glow.Value); }
            if (leafHex != null) { settings.leafColor = VineJobs20261019.Parse(leafHex); changed.Add("leaf colour " + leafHex); }
            rep.Add("job " + jobFile.Name + ": " + string.Join(" ", job.flower_colors) + ", grow " + settings.grow + ", glow " + settings.glow);
            if (changed.Count > 0) rep.Add("changed in Unity at Astra's request: " + string.Join(", ", changed));

            var scene = EditorSceneManager.GetActiveScene();
            var backup = Path.Combine("Review/Backups/vines-20261019", Path.GetFileName(scene.path));
            Directory.CreateDirectory(Path.GetDirectoryName(backup));
            if (!File.Exists(backup)) File.Copy(scene.path, backup);
            rep.Add("scene backup " + backup);

            var frames = targets.Select(t => Frame(BoundsOf(Meshes(t)))).ToList();
            for (int i = 0; i < targets.Count; i++) Shot(tag + " " + (i + 1) + " before", frames[i]);

            var lines = new List<string>();
            var root = VineBuilder20261019.Grow(targets, settings, colours, lines);
            if (changed.Count > 0) lines.Add("Changed in Unity after the page's job: " + string.Join(", ", changed) + ".");
            VineBuilder20261019.WriteReceipt(lines, root ? "completed" : "failed", "From Unity Unicorn's page (" + jobFile.Name + "), grown through the agent queue");
            rep.AddRange(lines.Select(l => "grow: " + l));
            if (!root) throw new System.InvalidOperationException("The tool grew nothing.");
            for (int i = 0; i < targets.Count; i++) Shot(tag + " " + (i + 1) + " after", frames[i]);

            // One Undo must take the whole Grow away; then Redo puts it back for Astra to look at.
            int kids = root.transform.childCount;
            Undo.PerformUndo();
            bool gone = GameObject.Find(RootName) == null;
            Undo.PerformRedo();
            var back = GameObject.Find(RootName);
            rep.Add("undo check: " + kids + " vine objects; after one Undo gone=" + gone + "; after Redo back=" + (back != null) + (back ? " with " + back.transform.childCount + " children" : ""));
            rep.Add(gone && back ? "PASS one Undo removes the vines" : "FAIL undo check");
            EditorSceneManager.MarkSceneDirty(scene);
        }
        catch (System.Exception e) { rep.Add("ERROR " + e.Message); Debug.LogException(e); }
        File.WriteAllText(Path.Combine(Out, tag + "-report.txt"), string.Join("\n", rep) + "\n");
    }

    [MenuItem("Astra/Claude/Vines 4 close-ups")]
    public static void CloseUps()
    {
        Directory.CreateDirectory(Out);
        var root = GameObject.Find(RootName);
        if (!root) { File.WriteAllText(Path.Combine(Out, "close-ups.txt"), "no vines in the scene\n"); return; }
        int k = 0;
        foreach (var mr in root.GetComponentsInChildren<MeshRenderer>())
        {
            k++;
            var b = mr.bounds;
            Shot("close " + k + " " + Safe(mr.name) + " a", b, new Vector3(1f, 0.35f, 1f), 0.55f);
            Shot("close " + k + " " + Safe(mr.name) + " b", b, new Vector3(-1f, 0.2f, 0.4f), 0.55f);
        }
        File.WriteAllText(Path.Combine(Out, "close-ups.txt"), k + " vine meshes photographed\n");
    }

    [MenuItem("Astra/Claude/Vines 3 remove")]
    public static void Remove()
    {
        var root = GameObject.Find(RootName);
        if (root) { Undo.DestroyObjectImmediate(root); EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene()); }
        File.WriteAllText(Path.Combine(Out, "remove.txt"), (root ? "removed " : "nothing to remove: no ") + RootName + "\n");
    }

    // Everything that moves at runtime: AstraCloudOrbit's pivots and clouds (it only reads the stair turns), and the duck.
    static List<Transform> MovingRoots()
    {
        var list = new List<Transform>();
        foreach (var mb in Object.FindObjectsOfType<MonoBehaviour>().Where(m => m))
        {
            var type = mb.GetType().Name;
            if (type == "AstraDuckFloat") list.Add(mb.transform);
            if (type != "AstraCloudOrbit") continue;
            foreach (var f in mb.GetType().GetFields())
                if (f.FieldType == typeof(Transform[]) && (f.Name == "pivots" || f.Name == "clouds"))
                    list.AddRange(((Transform[])f.GetValue(mb) ?? new Transform[0]).Where(x => x));
        }
        return list.Distinct().ToList();
    }

    static List<MeshFilter> Meshes(GameObject go) => go.GetComponentsInChildren<MeshFilter>()
        .Where(m => m.sharedMesh && m.GetComponent<MeshRenderer>() && m.GetComponent<MeshRenderer>().enabled).ToList();

    static Bounds BoundsOf(List<MeshFilter> mfs)
    {
        var b = mfs[0].GetComponent<MeshRenderer>().bounds;
        foreach (var m in mfs) b.Encapsulate(m.GetComponent<MeshRenderer>().bounds);
        return b;
    }

    // Very tall things (the pole) are framed around the spawn height instead of their whole length.
    static Bounds Frame(Bounds b)
    {
        if (b.size.y <= 60f) return b;
        var spawn = GameObject.Find("Spawn - facing pole");
        float y = spawn ? spawn.transform.position.y + 1.5f : 2f;
        return new Bounds(new Vector3(b.center.x, y, b.center.z), new Vector3(Mathf.Max(b.size.x, 1f), 6f, Mathf.Max(b.size.z, 1f)));
    }

    static void Shot(string name, Bounds b) => Shot(name, b, new Vector3(1f, 0.45f, 1f), 1.05f);

    static void Shot(string name, Bounds b, Vector3 from, float distance)
    {
        float r = Mathf.Max(b.extents.magnitude, 0.6f);
        var dir = from.normalized;
        var pos = b.center + dir * (r / Mathf.Sin(25f * Mathf.Deg2Rad) * distance);
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

    static GameObject FindPath(string path)
    {
        foreach (var t in Object.FindObjectsOfType<Transform>()) if (PathOf(t) == path) return t.gameObject;
        return null;
    }

    static string Safe(string s) => Regex.Replace(s, @"[^A-Za-z0-9 _\-]", "").Trim();
}
