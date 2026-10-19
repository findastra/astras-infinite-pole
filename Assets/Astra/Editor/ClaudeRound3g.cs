// Claude round 3g (2026-09-24): two linked phone booths on inner-ring clouds, 7 stair levels apart.
// Talk in one and you're heard in the other (and the other way around). Placeholder look; Astra may restyle.
// Run after rounds 3b/3d/3f. Menu: Astra > Claude Round 3g - Phone Booths. Safe to rerun.
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharp;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ClaudeRound3g
{
    const string Scene = "Assets/Astra/Scenes/AstrasInfinitePole.unity";
    const string Root = "Assets/Astra/";
    const string BoothName = "Phone booth";
    static readonly int[] Levels = { 3, 10 }; // 7 stair turns apart (~45 m); both high enough to stay clear of the screen and DJ zones

    [MenuItem("Astra/Claude Round 3g - Phone Booths")]
    public static void Run()
    {
        Debug.Log("CLAUDE_ROUND3G start");
        EnsureProgram("AstraPhoneLine"); EnsureProgram("AstraPhoneBooth");
        var scene = EditorSceneManager.OpenScene(Scene);
        File.Copy(Scene, "Review/Backups/BeforeClaudeRound3g.unity.txt", true);
        var report = new List<string>();
        foreach (var t in Resources.FindObjectsOfTypeAll<Transform>().Where(t => t.gameObject.scene == scene && (t.name.StartsWith(BoothName) || t.name == "Phone line")).ToList())
            if (t != null) Object.DestroyImmediate(t.gameObject);

        var lineGo = new GameObject("Phone line"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lineGo, scene);
        var line = lineGo.AddUdonSharpComponent<AstraPhoneLine>();
        var mats = BoothMaterials();
        var spiral = Object.FindObjectOfType<AstraSpiral>();
        int n = spiral.turns.Length;
        var booths = new List<GameObject>();
        for (int b = 0; b < 2; b++)
        {
            var turn = spiral.turns[((Levels[b] % n) + n) % n];
            var ring = turn.Cast<Transform>().First(t => t.name.StartsWith("Cloud ring 1"));
            var cloud = ring.Cast<Transform>().First(t => t.name.StartsWith("Cloud "));
            var s = cloud.localScale; float big = Mathf.Max(1.3f, Mathf.Max(s.x, s.z));
            cloud.localScale = new Vector3(big, s.y, big); // roomy enough to stand in a booth
            var booth = new GameObject(BoothName + " " + (b + 1)); booth.transform.SetParent(cloud, false);
            booth.transform.localScale = new Vector3(1 / cloud.localScale.x, 1 / cloud.localScale.y, 1 / cloud.localScale.z); // real-world size
            BuildBooth(booth.transform, mats);
            var zone = new GameObject("Phone booth voice zone"); zone.transform.SetParent(booth.transform, false); zone.transform.localPosition = new Vector3(0, 1.15f, 0);
            var trig = zone.AddComponent<BoxCollider>(); trig.isTrigger = true; trig.size = new Vector3(1.2f, 2.3f, 1.2f); // inside the booth walls
            var pb = zone.AddUdonSharpComponent<AstraPhoneBooth>(); pb.line = line; pb.booth = b; UdonSharpEditorUtility.CopyProxyToUdon(pb);
            booths.Add(booth);
            report.Add("Booth " + (b + 1) + ": level " + Levels[b] + " on " + ring.name + " / " + cloud.name + " (inner ring, one hop from the stairs), cloud widened to " + big.ToString("0.0") + "x");
        }
        UdonSharpEditorUtility.CopyProxyToUdon(line);
        var orbitB = Object.FindObjectOfType<AstraCloudOrbit>();
        if (orbitB != null && orbitB.clouds != null)
            for (int i = 0; i < orbitB.clouds.Length; i++)
                if (orbitB.clouds[i] != null && orbitB.clouds[i].Find(BoothName + " 1") != null || orbitB.clouds[i] != null && orbitB.clouds[i].Find(BoothName + " 2") != null) orbitB.cloudSpin[i] = 0;
        if (orbitB != null) UdonSharpEditorUtility.CopyProxyToUdon(orbitB);

        // World items switch
        var items = Object.FindObjectOfType<AstraWorldItems>(); var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(items);
        Transform panel = null; foreach (var r in scene.GetRootGameObjects()) foreach (var t in r.GetComponentsInChildren<Transform>(true)) if (t.name == "World items") panel = t;
        const string label = "PHONE BOOTHS";
        var oldT = panel.Find(label);
        var footer = panel.GetComponentsInChildren<Text>(true).First(t => t.text.StartsWith("Glitter, petals")).GetComponent<RectTransform>();
        float y; if (oldT != null) { y = oldT.GetComponent<RectTransform>().anchoredPosition.y; Object.DestroyImmediate(oldT.gameObject); } else { y = footer.anchoredPosition.y + 10; footer.anchoredPosition += new Vector2(0, -62); }
        var toggle = MakeToggle(panel, label, 0, y, true, udon);
        var objs = new List<GameObject>(); var togs = new List<Toggle>();
        for (int i = 0; i < items.objects.Length; i++) if (items.objects[i] != null && !items.objects[i].name.StartsWith(BoothName) && items.objectToggles[i] != null) { objs.Add(items.objects[i]); togs.Add(items.objectToggles[i]); }
        foreach (var g in booths) { objs.Add(g); togs.Add(toggle); }
        items.objects = objs.ToArray(); items.objectToggles = togs.ToArray(); UdonSharpEditorUtility.CopyProxyToUdon(items);

        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);

        // validation
        spiral.Recenter(0); Physics.SyncTransforms();
        var p0 = booths[0].transform.position; var p1 = booths[1].transform.position;
        report.Add("Distance between booths: " + Vector3.Distance(p0, p1).ToString("0") + " m (" + ((p1.y - p0.y) / spiral.pitch).ToString("0") + " levels); linked voices carry " + line.linkedNear + " m at full volume " + (Vector3.Distance(p0, p1) < line.linkedNear ? "PASS" : "FAIL"));
        foreach (var bt in booths)
        {
            bool floor = Physics.Raycast(bt.transform.position + Vector3.up * .5f, Vector3.down, out var h, 1f, ~0, QueryTriggerInteraction.Ignore) && h.collider.name.StartsWith("Cloud ");
            report.Add(bt.name + " floor is the cloud: " + (floor ? "PASS" : "FAIL"));
        }
        var zones = Object.FindObjectOfType<AstraCloudOrbit>();
        bool clear = true;
        if (zones != null && zones.clearZones != null) foreach (var bt in booths) for (int k = 0; k < zones.clearZones.Length; k++) if (Mathf.Abs(bt.transform.position.y - zones.clearZones[k].position.y) < zones.clearHalf[k].y + 1.5f) clear = false;
        report.Add("Booth clouds never pass through a keep-clear zone: " + (clear ? "PASS" : "FAIL"));
        File.WriteAllText("Review/claude-round3g-validation.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND3G_OK " + string.Join(" | ", report));
    }


    // ---------- the booth: a classic red phone box in pink-red, built as 4 merged meshes (one per material) ----------
    static Material[] BoothMaterials()
    {
        Material Make(string name, string shader) { string path = Root + "Materials/" + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path); if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, path); } m.shader = Shader.Find(shader); m.enableInstancing = true; EditorUtility.SetDirty(m); return m; }
        var body = Make("Phone Booth Body", "Astra/Glitter Cloud");
        body.SetColor("_Top", new Color(1f, .3f, .47f)); body.SetColor("_Bottom", new Color(.78f, .16f, .36f)); body.SetColor("_Rim", new Color(1.8f, .5f, 1.1f)); body.SetFloat("_Glitter", .8f); body.SetFloat("_Pastel", 0f);
        var sign = Make("Phone Booth Sign Glow", "Astra/Glitter Cloud");
        sign.SetColor("_Top", new Color(2.6f, 1.0f, 2.2f)); sign.SetColor("_Bottom", new Color(2.3f, .8f, 2.0f)); sign.SetColor("_Rim", new Color(1.5f, .8f, 1.4f)); sign.SetFloat("_Glitter", .3f); sign.SetFloat("_Pastel", 0f);
        var glass = Make("Phone Booth Glass", "Astra/Prismatic Stair");
        glass.SetColor("_Color", new Color(1f, .55f, .75f)); glass.SetFloat("_Opacity", .18f); glass.SetFloat("_Glitter", .6f); glass.SetFloat("_RainbowRate", 0f); glass.SetFloat("_Rainbow", 0f); glass.SetFloat("_Hue", 0f);
        var phone = Make("Phone Booth Payphone", "Astra/Glitter Cloud");
        phone.SetColor("_Top", new Color(.34f, .3f, .4f)); phone.SetColor("_Bottom", new Color(.16f, .13f, .2f)); phone.SetColor("_Rim", new Color(1.2f, .6f, 1.1f)); phone.SetFloat("_Glitter", .5f); phone.SetFloat("_Pastel", 0f);
        return new[] { body, sign, glass, phone };
    }

    class Parts { public List<Vector3> v = new List<Vector3>(); public List<Vector3> n = new List<Vector3>(); public List<int> t = new List<int>(); }
    static void Box(Parts p, Vector3 c, Vector3 size, Quaternion rot, Vector3 pivot)
    {
        var h = size / 2;
        Vector3[] dirs = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        foreach (var d in dirs)
        {
            Vector3 a = Mathf.Abs(d.y) > .5f ? Vector3.right : Vector3.up, b = Vector3.Cross(d, a);
            int k = p.v.Count;
            foreach (var (sa, sb) in new[] { (-1, -1), (-1, 1), (1, 1), (1, -1) })
            {
                var local = Vector3.Scale(d + a * sa + b * sb, h);
                p.v.Add(pivot + rot * (c - pivot + local)); p.n.Add(rot * d);
            }
            p.t.AddRange(new[] { k, k + 2, k + 1, k, k + 3, k + 2 }); // Unity front faces: clockwise
        }
    }
    static void Box(Parts p, Vector3 c, Vector3 size) => Box(p, c, size, Quaternion.identity, Vector3.zero);

    static void BuildBooth(Transform root, Material[] mats)
    {
        Parts body = new Parts(), sign = new Parts(), glass = new Parts(), phone = new Parts();
        const float W = 1.3f, H = 2.55f, post = .12f, half = W / 2 - post / 2;
        // corner posts, roof, cap
        foreach (var x in new[] { -half, half }) foreach (var z in new[] { -half, half }) Box(body, new Vector3(x, H / 2, z), new Vector3(post, H, post));
        Box(body, new Vector3(0, H + .06f, 0), new Vector3(W + .12f, .12f, W + .12f));
        Box(body, new Vector3(0, H + .16f, 0), new Vector3(W - .1f, .08f, W - .1f));
        // sign band with two glowing panels on every side
        for (int side = 0; side < 4; side++)
        {
            var rot = Quaternion.Euler(0, side * 90, 0);
            Box(body, rot * new Vector3(0, H - .12f, W / 2 - .03f), Abs(rot * new Vector3(W, .2f, .06f)));
            foreach (var x in new[] { -.28f, .28f }) Box(sign, rot * new Vector3(x, H - .12f, W / 2 + .005f), Abs(rot * new Vector3(.46f, .1f, .02f)));
        }
        // three paned walls (back, left, right) and an open door on the front
        float paneTop = H - .24f, paneBottom = .3f;
        void Wall(Parts bodyP, Parts glassP, Quaternion rot, Vector3 pivot, float width)
        {
            Box(glassP, new Vector3(0, (paneTop + paneBottom) / 2, 0), new Vector3(width, paneTop - paneBottom, .015f), rot, pivot);
            Box(bodyP, new Vector3(0, paneBottom / 2 + .02f, 0), new Vector3(width, paneBottom, .05f), rot, pivot);        // kick panel
            for (float y = paneBottom + .32f; y < paneTop - .1f; y += .32f) Box(bodyP, new Vector3(0, y, 0), new Vector3(width, .035f, .04f), rot, pivot); // glazing bars
            Box(bodyP, new Vector3(0, (paneTop + paneBottom) / 2, 0), new Vector3(.035f, paneTop - paneBottom, .04f), rot, pivot);          // middle bar
        }
        float inner = W - 2 * post;
        foreach (var side in new[] { 90, 180, 270 })
        {
            var rot = Quaternion.Euler(0, side, 0);
            Parts b2 = new Parts(), g2 = new Parts(); Wall(b2, g2, Quaternion.identity, Vector3.zero, inner);
            Append(body, b2, rot, rot * new Vector3(0, 0, W / 2 - .03f)); Append(glass, g2, rot, rot * new Vector3(0, 0, W / 2 - .03f));
        }
        { // front door, hinged on the left post and swung open ~105 degrees
            Parts b2 = new Parts(), g2 = new Parts(); Wall(b2, g2, Quaternion.identity, Vector3.zero, inner);
            var hinge = new Vector3(-inner / 2, 0, 0);
            var swing = Quaternion.Euler(0, -105, 0);
            Parts b3 = new Parts(), g3 = new Parts();
            Append(b3, b2, Quaternion.identity, -hinge); Append(g3, g2, Quaternion.identity, -hinge);
            Append(body, b3, swing, new Vector3(-inner / 2, 0, W / 2 - .03f)); Append(glass, g3, swing, new Vector3(-inner / 2, 0, W / 2 - .03f));
        }
        // payphone on the back wall, handset and a coiled cord hanging down
        var back = new Vector3(0, 0, -(W / 2 - .03f) + .1f);
        Box(phone, back + new Vector3(.1f, 1.38f, 0), new Vector3(.3f, .48f, .12f));
        Box(phone, back + new Vector3(.1f, 1.72f, .01f), new Vector3(.16f, .16f, .1f));        // top box
        Box(sign, back + new Vector3(.12f, 1.4f, .065f), new Vector3(.12f, .16f, .01f));       // lit keypad
        Box(phone, back + new Vector3(-.11f, 1.44f, .04f), new Vector3(.06f, .28f, .07f));     // handset
        Coil(phone, back + new Vector3(-.11f, 1.29f, .05f), .028f, .006f, .55f, 16);
        Box(body, new Vector3(0, .02f, 0), new Vector3(W, .04f, W));                              // floor plate
        string[] names = { "Booth body", "Booth sign glow", "Booth glass", "Booth payphone" };
        var parts = new[] { body, sign, glass, phone };
        for (int i = 0; i < 4; i++)
        {
            var mesh = new Mesh { name = "Phone booth " + names[i] }; mesh.SetVertices(parts[i].v); mesh.SetNormals(parts[i].n); mesh.SetTriangles(parts[i].t, 0); mesh.RecalculateBounds();
            string path = Root + "Meshes/Phone Booth " + i + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path); else { existing.Clear(); existing.SetVertices(parts[i].v); existing.SetNormals(parts[i].n); existing.SetTriangles(parts[i].t, 0); existing.RecalculateBounds(); mesh = existing; EditorUtility.SetDirty(existing); }
            var go = new GameObject(names[i]); go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>(); r.sharedMaterial = mats[i]; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        }
    }
    static Vector3 Abs(Vector3 v) => new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
    static void Append(Parts into, Parts from, Quaternion rot, Vector3 offset)
    {
        int k = into.v.Count;
        foreach (var x in from.v) into.v.Add(rot * x + offset);
        foreach (var x in from.n) into.n.Add(rot * x);
        foreach (var i in from.t) into.t.Add(k + i);
    }
    // coiled phone cord: a thin tube wound in a helix, hanging straight down
    static void Coil(Parts p, Vector3 top, float coilR, float wireR, float length, int turns)
    {
        int steps = turns * 12, sides = 5; int k0 = p.v.Count;
        for (int s = 0; s <= steps; s++)
        {
            float t = s / (float)steps, a = t * turns * Mathf.PI * 2;
            var c = top + new Vector3(Mathf.Cos(a) * coilR, -t * length, Mathf.Sin(a) * coilR);
            var tangent = new Vector3(-Mathf.Sin(a) * coilR * turns * Mathf.PI * 2, -length, Mathf.Cos(a) * coilR * turns * Mathf.PI * 2).normalized;
            var nrm = Vector3.Cross(tangent, Vector3.up).normalized; var bin = Vector3.Cross(tangent, nrm);
            for (int j = 0; j < sides; j++) { float b = j * Mathf.PI * 2 / sides; var d = nrm * Mathf.Cos(b) + bin * Mathf.Sin(b); p.v.Add(c + d * wireR); p.n.Add(d); }
        }
        for (int s = 0; s < steps; s++) for (int j = 0; j < sides; j++)
        {
            int a0 = k0 + s * sides + j, a1 = k0 + s * sides + (j + 1) % sides, b0 = a0 + sides, b1 = a1 + sides;
            p.t.AddRange(new[] { a0, b0, a1, a1, b0, b1, a0, a1, b0, a1, b1, b0 }); // both sides, so the thin cord always shows
        }
    }
    static void Prim(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
    {
        var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent, false);
        go.transform.localPosition = pos; go.transform.localScale = scale; go.GetComponent<MeshRenderer>().sharedMaterial = m;
        go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        Object.DestroyImmediate(go.GetComponent<Collider>()); // nothing to bump into on a spinning cloud
    }
    static RectTransform Rect(string name, Transform parent, Vector2 pos, Vector2 size) { var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); r.SetParent(parent, false); r.anchoredPosition = pos; r.sizeDelta = size; return r; }
    static Toggle MakeToggle(Transform p, string text, float x, float y, bool value, VRC.Udon.UdonBehaviour u)
    {
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var r = Rect(text, p, new Vector2(x, y), new Vector2(520, 50));
        var im = r.gameObject.AddComponent<Image>(); im.color = new Color(.2f, .07f, .18f);
        var t = r.gameObject.AddComponent<Toggle>(); t.targetGraphic = im;
        var mark = Rect("Crystal switch", r, new Vector2(-235, 0), new Vector2(22, 22)); mark.localRotation = Quaternion.Euler(0, 0, 45);
        var check = mark.gameObject.AddComponent<Image>(); check.color = new Color(1f, .5f, .82f); t.graphic = check; t.isOn = value;
        var lr = Rect(text, r, new Vector2(20, 0), new Vector2(460, 38));
        var lab = lr.gameObject.AddComponent<Text>(); lab.font = font; lab.text = text; lab.fontSize = 18; lab.color = new Color(1f, .86f, .96f); lab.alignment = TextAnchor.MiddleCenter; lab.raycastTarget = false;
        UnityEventTools.AddStringPersistentListener(t.onValueChanged, u.SendCustomEvent, "Apply");
        return t;
    }
    static void EnsureProgram(string name)
    {
        string path = Root + "Scripts/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path) == null)
        {
            var a = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            a.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "Scripts/" + name + ".cs");
            AssetDatabase.CreateAsset(a, path); AssetDatabase.SaveAssets();
        }
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
    }
}
