// Inspection photos (Claude, 2026-09-26). Menu: Astra > Claude > 14 Inspection photos.
// Close-ups of everything the polish + banner passes touched, rendered from the open scene (nothing is changed or saved).
// Output: Review/Inspect/*.jpg plus Review/Inspect/notes.txt (what overlaps the breakfast / banner booth spots).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public static class AstraClaudeInspect
{
    const string Dir = "Review/Inspect/";

    [MenuItem("Astra/Claude/14 Inspection photos")]
    public static void Run()
    {
        Directory.CreateDirectory(Dir);
        foreach (var f in Directory.GetFiles(Dir, "*.jpg")) File.Delete(f);
        var notes = new StringBuilder();
        foreach (var ps in Object.FindObjectsOfType<ParticleSystem>()) ps.Simulate(12, true, true);
        var desc = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
        Vector3 spawn = desc != null && desc.spawns != null && desc.spawns.Length > 0 && desc.spawns[0] != null ? desc.spawns[0].position : Vector3.zero;
        Vector3 eye = spawn + Vector3.up * 1.6f;

        Shot("00 top down", new Vector3(0, 95, -.01f), Vector3.zero, 62);
        Shot("01 spawn view toward sunset", eye, eye + new Vector3(0, -.05f, 1), 75);
        Shot("02 spawn view back", eye, eye + new Vector3(0, -.05f, -1), 75);

        var dish = Find("Bath dish");
        if (dish != null)
        {
            var b = Bounds(dish); var o = Out(b.center);
            Shot("03 bath", b.center + o * (b.extents.magnitude * .9f) + Vector3.up * (b.size.y + 5), b.center, 60);
            Shot("04 bath side", b.center + o * (b.extents.magnitude * 1.1f) + Vector3.up * 1.6f, b.center + Vector3.up * 1.5f, 60);
            var duck = dish.Find("Rubber ducky"); if (duck != null) Shot("05 duck", duck.position + new Vector3(1.2f, .8f, 1.2f), duck.position, 50);
        }
        foreach (var n in new[] { "Phone booth 1", "Phone booth 2" })
        {
            var bt = Find(n); if (bt == null) continue; var bp = bt.position; var fwd = bt.forward; fwd.y = 0; fwd.Normalize();
            Shot("06 " + n + " front", bp + fwd * 4f + Vector3.up * 1.5f, bp + Vector3.up * 1.1f, 55);
            Shot("07 " + n + " low side", bp + bt.right * 3f + Vector3.up * .5f, bp + Vector3.up * .5f, 55);
        }
        var dj = Find("12 - DJ cloud");
        if (dj != null)
        {
            var st = dj.Find("Stage lights"); var b = Bounds(dj, 30);
            var o = Out(b.center); Shot("08 DJ cloud", b.center + o * 9f + Vector3.up * 3.5f, b.center + Vector3.up * .5f, 58);
            if (st != null && st.childCount > 0) { var t = st.GetChild(0); Shot("09 DJ light close", t.position + (t.position - b.center).normalized * 1.6f + Vector3.up * .7f, t.position + Vector3.up * .15f, 50); }
        }
        var vine = Object.FindObjectsOfType<Transform>().FirstOrDefault(t => t.name == "Chandelier strands");
        if (vine != null) { var b = Bounds(vine.parent); var o = Out(b.center); Shot("10 vines under a cloud", new Vector3(b.center.x, b.min.y - 1.2f, b.center.z) + o * (b.extents.x + 5), new Vector3(b.center.x, b.min.y + .6f, b.center.z), 55); }
        var outer = Find("14 - Outer big clouds (Claude)");
        if (outer != null && outer.childCount > 1)
        {
            var seg = outer.Cast<Transform>().First(t => t.name.StartsWith("Outer cloud")); var p = seg.position;
            Shot("11 outer track", p * .62f + Vector3.up * 7f, p + Vector3.up * .5f, 70);
        }
        var swing = Find("Cloud swing");
        if (swing != null) { var b = Bounds(swing); Shot("12 swing", b.center + swing.right * (b.size.y * .9f + 3) + Vector3.up * 1f, b.center, 60); }
        var bf = Find("Breakfast cloud");
        if (bf != null)
        {
            var b = Bounds(bf); var o = Out(b.center);
            Shot("13 breakfast cloud", b.center + o * 7f + Vector3.up * 3f, b.center, 55);
            var table = bf.Find("Breakfast table"); if (table != null) Shot("14 breakfast table", table.position + table.right * 1.1f + Vector3.up * 1.5f, table.position + Vector3.up * .74f, 50);
            var his = bf.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "His breakfast");
            if (his != null) { var w = his.Find("Glitter waffle"); if (w != null) Shot("15 waffle text", w.position + Vector3.up * .32f - his.forward * .08f, w.position, 40); }
            notes.Append(Overlaps("Breakfast cloud", bf));
        }
        var hero = Find("16 - Banner phone booth (Claude)");
        if (hero != null)
        {
            var hp = hero.position; var f = hero.forward;
            Shot("16 banner booth", hp + f * 7.5f + Vector3.up * 1.4f, hp + Vector3.up * 1.2f, 50);
            Shot("17 banner booth from spawn", eye, hp + Vector3.up * 1.2f, 45);
            Shot("18 banner booth close", hp + f * 3f + hero.right * 1.2f + Vector3.up * .9f, hp + Vector3.up * .9f, 60);
            notes.Append(Overlaps("Banner booth", hero));
        }
        // round 2 checks
        if (dish != null) { var b = Bounds(dish); var o = Out(b.center); Shot("20 bath water", b.center + o * (b.extents.x * .9f) + Vector3.up * (b.max.y - b.center.y + 2.5f), b.center + Vector3.up * 1f, 60); }
        var console = Find("Deck console");
        if (console != null)
        {
            var step = Find("DJ step cloud"); var cp = console.position; var toCrowd = step != null ? step.position - cp : console.forward; toCrowd.y = 0; toCrowd.Normalize();
            Shot("21 DJ console from the DJ side", cp - toCrowd * 2.6f + Vector3.up * 1.5f, cp + Vector3.up * .5f, 60);
            Shot("22 DJ console from the crowd", cp + toCrowd * 4f + Vector3.up * 1.6f, cp + Vector3.up * .6f, 55);
        }
        foreach (var n in new[] { "Heart cloud", "Star cloud" })
        {
            var h = Object.FindObjectsOfType<Transform>().FirstOrDefault(t => t.name == n); if (h == null) continue; var hp = h.position;
            Shot("23 " + n + " from above", hp + Vector3.up * 11f + Out(hp) * -4f, hp, 50);
        }
        Shot("24 hearts and stars from the stairs", new Vector3(0, 30, 0) + Vector3.back * 3f, new Vector3(0, 14, 18), 80);
        var trio = Find("Cloud swing trio");
        if (trio != null) { var b = Bounds(trio, 40); Shot("25 swing trio", b.center + trio.forward * 13f + Vector3.up * -2f, b.center + Vector3.down * 2.5f, 62); notes.Append(Overlaps("Swing trio", trio)); }
        var single = Find("Cloud swing single 1");
        if (single != null) { var b = Bounds(single, 40); Shot("26 single swing", b.center + single.forward * 10f + Vector3.up * -2f, b.center + Vector3.down * 2.5f, 62); notes.Append(Overlaps("Single swing 1", single)); }
        File.WriteAllText(Dir + "notes.txt", notes.ToString());
        Debug.Log("ASTRA_INSPECT " + Directory.GetFiles(Dir, "*.jpg").Length + " photos\n" + notes);
    }

    static Transform Find(string n) { return Resources.FindObjectsOfTypeAll<Transform>().FirstOrDefault(t => t.gameObject.scene.IsValid() && t.name == n); }
    static Vector3 Out(Vector3 c) { var d = new Vector3(c.x, 0, c.z); return d.sqrMagnitude < .01f ? Vector3.back : d.normalized; }
    static Bounds Bounds(Transform t, float maxSize = 200)
    {
        var rs = t.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer) && r.bounds.size.x < maxSize).ToArray();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.one); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds); return b;
    }
    static string Overlaps(string label, Transform t)
    {
        var mine = new HashSet<Renderer>(t.GetComponentsInChildren<Renderer>(true)); var b = Bounds(t, 60);
        var hits = Object.FindObjectsOfType<Renderer>().Where(r => !(r is ParticleSystemRenderer) && !mine.Contains(r) && r.bounds.Intersects(b))
            .Select(r => Path(r.transform) + "  " + r.bounds.size.ToString("F1")).Take(25).ToList();
        return label + " bounds " + b.center.ToString("F1") + " size " + b.size.ToString("F1") + " overlaps " + hits.Count + ":\n  " + string.Join("\n  ", hits) + "\n";
    }
    static string Path(Transform t) { var s = t.name; for (int i = 0; i < 4 && t.parent != null; i++) { t = t.parent; s = t.name + "/" + s; } return s; }

    static void Shot(string name, Vector3 pos, Vector3 look, float fov)
    {
        const int w = 1280, h = 720;
        var go = new GameObject("Claude inspect camera");
        try
        {
            var cam = go.AddComponent<Camera>();
            cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .02f; cam.farClipPlane = 2000;
            var res = AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset");
            if (res != null) { var pp = go.AddComponent<PostProcessLayer>(); pp.Init(res); pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; }
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
            cam.targetTexture = rt; cam.Render(); cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
            File.WriteAllBytes(Dir + name + ".jpg", tex.EncodeToJPG(88));
            cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(tex);
        }
        finally { Object.DestroyImmediate(go); }
    }
}
