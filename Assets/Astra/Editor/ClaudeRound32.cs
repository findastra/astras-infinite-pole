// Claude, 2026-10-04. Menu: Astra > Claude > 32 Theatre seats, DJ clear, glow orbs, no breakfast nook
//  1. Breakfast nook (the "Breakfast cloud" with the waffle table) is deleted.
//  2. DJ booth: the cloud in FRONT of and beside the booth is pressed down to the booth's base (round 30 only cleared the
//     footprint), so the MOMMY'S front, decks and speakers are no longer cut by cloud. Smooth falloff, colliders untouched.
//  3. Glow orbs: every pickup is checked. Orbs buried in another collider or out of reach are lifted/moved to a free
//     spot at hand height over the surface under them; pickupable, kinematic and the grab collider are made consistent.
//  4. Movie theatre in front of the big screen, built from the classic VRChat chair (SDK VRCChair3 prefab):
//     - a base row of chairs on the floor, curved to face the screen (spawn kept clear)
//     - private chairs above: pairs of chairs on floating clouds behind the base row
//     - a cloud along the top of the screen with seats you can sit on (click it from the floor; you get off in front)
//  5. Photos: Review/Photos/Round32/.
// Safe to rerun (the theatre is rebuilt). Backup: Review/Backups/BeforeRound32.unity.txt. Report: Review/claude-round32-report.txt.
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VRC.SDK3.Components;
using Object = UnityEngine.Object;

public static class ClaudeRound32
{
    const string Root = "Assets/Astra/";
    const string ChairPrefab = "Packages/com.vrchat.worlds/Samples/UdonExampleScene/Prefabs/VRCChair/VRCChair3.prefab";
    const string TheatreName = "17 - Cloud theatre (Claude)";
    static List<string> report;
    static System.Random rng;
    static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

    [MenuItem("Astra/Claude/32 Theatre seats, DJ clear, glow orbs, no breakfast nook")]
    public static void Run()
    {
        report = new List<string>(); rng = new System.Random(3232);
        try
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene);
            Directory.CreateDirectory("Review/Backups"); File.Copy(scene.path, "Review/Backups/BeforeRound32.unity.txt", true);
            Step("breakfast", Breakfast);
            Step("dj", DjFront);
            Step("orbs", Orbs);
            Step("theatre", Theatre);
            AssetDatabase.SaveAssets(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            Step("photos", Photos);
        }
        catch (Exception e) { report.Add("FAILED: " + e); }
        File.WriteAllText("Review/claude-round32-report.txt", string.Join("\n", report));
        Debug.Log("CLAUDE_ROUND32 " + string.Join(" | ", report));
    }
    static void Step(string name, Action a) { try { a(); } catch (Exception e) { report.Add("STEP FAILED (" + name + "): " + e.Message + "\n" + e.StackTrace); } }
    static string PathOf(Transform t) { var s = t.name; while (t.parent != null) { t = t.parent; s = t.name + "/" + s; } return s; }
    static IEnumerable<Transform> SceneTransforms() { return Resources.FindObjectsOfTypeAll<Transform>().Where(t => t != null && t.gameObject.scene.IsValid() && t.hideFlags == HideFlags.None); }

    // ---------------------------------------------------------------- 1
    static void Breakfast()
    {
        var hits = SceneTransforms().Where(t => t.name == "Breakfast cloud").ToList();
        foreach (var t in hits) { report.Add("breakfast: deleted " + PathOf(t)); Object.DestroyImmediate(t.gameObject); }
        if (hits.Count == 0) report.Add("breakfast: no Breakfast cloud in the scene (already gone)");
    }

    // ---------------------------------------------------------------- 2
    static void DjFront()
    {
        var dj = GameObject.Find("12 - DJ cloud"); var booth = dj != null ? dj.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DJ booth") : null;
        if (booth == null) { report.Add("dj: DJ booth not found"); return; }
        var rs = booth.GetComponentsInChildren<Renderer>(true).Where(r => !(r is ParticleSystemRenderer) && r.GetComponent<MeshFilter>() != null && r.GetComponent<MeshFilter>().sharedMesh != null
                    && !r.GetComponent<MeshFilter>().sharedMesh.name.StartsWith("Glitter Cloud")).ToList();
        if (rs.Count == 0) { report.Add("dj: no booth parts"); return; }
        var lo = Vector3.one * float.MaxValue; var hi = -lo;
        foreach (var r in rs)
        {
            var mb = r.GetComponent<MeshFilter>().sharedMesh.bounds; var m = booth.worldToLocalMatrix * r.transform.localToWorldMatrix;
            for (int k = 0; k < 8; k++) { var c = mb.center + Vector3.Scale(mb.extents, new Vector3((k & 1) == 0 ? -1 : 1, (k & 2) == 0 ? -1 : 1, (k & 4) == 0 ? -1 : 1)); var p = m.MultiplyPoint3x4(c); lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
        }
        // which local side faces the crowd: the one pointing at the pole (world origin)
        var toPole = booth.InverseTransformDirection(new Vector3(-booth.position.x, 0, -booth.position.z).normalized);
        bool frontIsZ = Mathf.Abs(toPole.z) >= Mathf.Abs(toPole.x); float sign = frontIsZ ? Mathf.Sign(toPole.z) : Mathf.Sign(toPole.x);
        float s = Mathf.Max(.01f, booth.lossyScale.x);
        float front = 2.2f / s, side = .7f / s, back = .2f / s, fall = .8f / s;          // metres turned into booth units
        float floorY = lo.y + .02f / Mathf.Max(.01f, booth.lossyScale.y);
        // clear box in booth space (before falloff)
        Vector3 cLo = lo, cHi = hi;
        if (frontIsZ) { cLo.x -= side; cHi.x += side; if (sign > 0) { cHi.z += front; cLo.z -= back; } else { cLo.z -= front; cHi.z += back; } }
        else { cLo.z -= side; cHi.z += side; if (sign > 0) { cHi.x += front; cLo.x -= back; } else { cLo.x -= front; cHi.x += back; } }
        Directory.CreateDirectory(Root + "Meshes/Lounge/DJ carved");
        int meshes = 0, moved = 0;
        foreach (var mf in dj.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf.transform.IsChildOf(booth) || mf.sharedMesh == null) continue;
            var mr = mf.GetComponent<MeshRenderer>(); if (mr == null || mr.sharedMaterial == null || mr.sharedMaterial.shader == null || mr.sharedMaterial.shader.name != "Astra/Glitter Cloud") continue;
            var toB = booth.worldToLocalMatrix * mf.transform.localToWorldMatrix; var toM = toB.inverse;
            var v = mf.sharedMesh.vertices; var n = mf.sharedMesh.normals; bool hasN = n != null && n.Length == v.Length; int hit = 0;
            for (int i = 0; i < v.Length; i++)
            {
                var p = toB.MultiplyPoint3x4(v[i]); if (p.y <= floorY) continue;
                float dx = Mathf.Max(0, Mathf.Max(cLo.x - p.x, p.x - cHi.x)), dz = Mathf.Max(0, Mathf.Max(cLo.z - p.z, p.z - cHi.z));
                float d = Mathf.Sqrt(dx * dx + dz * dz); if (d >= fall) continue;
                float t = Mathf.SmoothStep(0, 1, 1f - d / fall);                // 1 inside the clear box, easing to 0 at the edge
                float ny = Mathf.Lerp(p.y, floorY, t); if (ny >= p.y - 1e-5f) continue;
                p.y = ny; v[i] = toM.MultiplyPoint3x4(p);
                if (hasN) n[i] = Vector3.Slerp(n[i], toM.MultiplyVector(Vector3.up).normalized, t * .8f).normalized;
                hit++;
            }
            if (hit == 0) continue;
            string src = AssetDatabase.GetAssetPath(mf.sharedMesh);
            var copy = Object.Instantiate(mf.sharedMesh); copy.vertices = v; if (hasN) copy.normals = n; copy.RecalculateBounds();
            string path = src.Contains("/DJ carved/") ? src : Root + "Meshes/Lounge/DJ carved/" + string.Join("_", (mf.name + " " + mf.GetInstanceID()).Split(Path.GetInvalidFileNameChars())) + ".asset";
            copy.name = "DJ carved " + mf.name;
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(copy, path); mf.sharedMesh = copy; EditorUtility.SetDirty(mf);
            meshes++; moved += hit;
        }
        report.Add("dj: front of booth = local " + (frontIsZ ? "z" : "x") + (sign > 0 ? "+" : "-") + "; cloud pressed down in front (2.2 m), at the sides (0.7 m) with a 0.8 m soft edge: " + moved + " vertices in " + meshes + " cloud meshes");
    }

    // ---------------------------------------------------------------- 3
    static void Orbs()
    {
        var pickups = Object.FindObjectsOfType<VRCPickup>(true);
        int movedN = 0; var notes = new List<string>();
        foreach (var pk in pickups)
        {
            var t = pk.transform; var why = new List<string>();
            var col = pk.GetComponent<Collider>();
            if (col == null) { var sc = pk.gameObject.AddComponent<SphereCollider>(); col = sc; why.Add("had no collider"); }
            var rb = pk.GetComponent<Rigidbody>(); if (rb == null) { rb = pk.gameObject.AddComponent<Rigidbody>(); why.Add("had no rigidbody"); }
            if (!rb.isKinematic || rb.useGravity) { rb.isKinematic = true; rb.useGravity = false; why.Add("made kinematic (floats, no falling)"); }
            if (!pk.pickupable) { pk.pickupable = true; why.Add("was not pickupable"); }
            if (pk.DisallowTheft) { pk.DisallowTheft = false; why.Add("theft allowed so anyone can take it"); }
            if (!pk.gameObject.activeInHierarchy) why.Add("CHECK: object or a parent is switched off");
            var sph = col as SphereCollider; if (sph != null && sph.radius * t.lossyScale.x < .16f) { sph.radius = .16f / Mathf.Max(.01f, t.lossyScale.x); why.Add("grab sphere enlarged"); }
            Physics.SyncTransforms();
            float rad = col.bounds.extents.magnitude * .7f;
            bool buried = Physics.OverlapSphere(col.bounds.center, rad, ~0, QueryTriggerInteraction.Ignore).Any(c => c != col && !c.transform.IsChildOf(t) && c.bounds.size.magnitude > col.bounds.size.magnitude);
            RaycastHit floor; bool hasFloor = Physics.Raycast(col.bounds.center + Vector3.up * .05f, Vector3.down, out floor, 30f, ~0, QueryTriggerInteraction.Ignore);
            if (hasFloor && floor.collider == col) hasFloor = Physics.Raycast(col.bounds.center - Vector3.up * (col.bounds.extents.y + .02f), Vector3.down, out floor, 30f, ~0, QueryTriggerInteraction.Ignore);
            float height = hasFloor ? col.bounds.center.y - floor.point.y : -1f;
            bool tooHigh = hasFloor && height > 1.9f, tooLow = hasFloor && height < .55f;
            if (buried || tooHigh || tooLow)
            {
                Vector3 start = hasFloor ? new Vector3(col.bounds.center.x, floor.point.y + 1.15f, col.bounds.center.z) : col.bounds.center;
                Vector3 best = start; bool found = false;
                for (int ring = 0; ring < 8 && !found; ring++)
                    for (int a = 0; a < (ring == 0 ? 1 : 10) && !found; a++)
                    {
                        float ang = a * Mathf.PI * 2 / 10f, d = ring * .3f;
                        var cand = start + new Vector3(Mathf.Cos(ang) * d, 0, Mathf.Sin(ang) * d);
                        RaycastHit f2; if (!Physics.Raycast(cand + Vector3.up * 1.5f, Vector3.down, out f2, 6f, ~0, QueryTriggerInteraction.Ignore)) continue;
                        cand.y = f2.point.y + 1.15f;
                        if (Physics.OverlapSphere(cand, rad, ~0, QueryTriggerInteraction.Ignore).Any(c => c != col && !c.transform.IsChildOf(t))) continue;
                        best = cand; found = true;
                    }
                t.position += best - col.bounds.center; movedN++;
                why.Add((buried ? "was inside a bigger collider" : tooHigh ? "was " + height.ToString("0.0") + " m up, out of reach" : "was too low") + " -> moved " + (found ? "to a free spot at hand height" : "to hand height (no fully free spot nearby)"));
            }
            EditorUtility.SetDirty(pk); EditorUtility.SetDirty(t);
            if (why.Count > 0) notes.Add(t.name + ": " + string.Join("; ", why));
        }
        report.Add("orbs: " + pickups.Length + " pickups checked, " + movedN + " moved");
        foreach (var n in notes) report.Add("  " + n);
    }

    // ---------------------------------------------------------------- 4
    static Mesh cloudMesh; static Material cloudMat;
    static void CloudPieces()
    {
        var lounge = GameObject.Find("13 - Cloud lounge (Claude)");
        var mf = (lounge != null ? lounge.GetComponentsInChildren<MeshFilter>(true) : Object.FindObjectsOfType<MeshFilter>(true))
            .FirstOrDefault(f => f.sharedMesh != null && f.sharedMesh.name.StartsWith("Glitter Cloud") && f.GetComponent<MeshRenderer>() != null);
        if (mf == null) throw new Exception("no Glitter Cloud puff found to copy");
        cloudMesh = mf.sharedMesh; cloudMat = mf.GetComponent<MeshRenderer>().sharedMaterial;
    }
    // one combined cloud mesh: puffs along a line (width w, depth dep) with a flat-ish top at local y = 0
    static GameObject CloudSlab(string name, Transform parent, float w, float dep, float puffW, int count)
    {
        var g = new GameObject(name); g.transform.SetParent(parent, false);
        var list = new List<CombineInstance>(); var mb = cloudMesh.bounds;
        for (int i = 0; i < count; i++)
        {
            float k = puffW * R(.85f, 1.2f) / mb.size.x;
            var rot = Quaternion.Euler(0, R(0, 360), 0);
            var pos = new Vector3(R(-w / 2f + puffW * .3f, w / 2f - puffW * .3f), R(-.12f, .05f), R(-dep / 2f + puffW * .25f, dep / 2f - puffW * .25f));
            var top = (mb.center + Vector3.up * mb.extents.y) * k;                               // put each puff's top at pos.y
            list.Add(new CombineInstance { mesh = cloudMesh, transform = Matrix4x4.TRS(pos - rot * top, rot, Vector3.one * k) });
        }
        var m = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 }; m.CombineMeshes(list.ToArray(), true, true); m.RecalculateBounds();
        Directory.CreateDirectory(Root + "Meshes/Theatre"); string path = Root + "Meshes/Theatre/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
        m.name = name; AssetDatabase.CreateAsset(m, path);
        g.AddComponent<MeshFilter>().sharedMesh = m; var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = cloudMat;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
        return g;
    }
    static GameObject Chair(Transform parent, Vector3 pos, Vector3 lookAt, Vector3? exitAt, float proximity, bool showChair, string name)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChairPrefab);
        if (prefab == null) throw new Exception("VRCChair3 prefab not found at " + ChairPrefab);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        go.name = name; go.transform.SetParent(parent, true); go.transform.position = pos;
        var flat = lookAt - pos; flat.y = 0; if (flat.sqrMagnitude > .001f) go.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
        foreach (var ub in go.GetComponentsInChildren<VRC.Udon.UdonBehaviour>(true)) { ub.proximity = proximity; ub.interactText = "Sit"; EditorUtility.SetDirty(ub); }
        if (!showChair) foreach (var r in go.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        else foreach (var r in go.GetComponentsInChildren<MeshRenderer>(true)) { r.sharedMaterial = ChairMat(); EditorUtility.SetDirty(r); }
        if (exitAt.HasValue)
        {
            var st = go.GetComponentInChildren<VRCStation>(true);
            if (st != null) { var ex = new GameObject("Exit to floor").transform; ex.SetParent(go.transform, false); ex.position = exitAt.Value; ex.rotation = go.transform.rotation; st.stationExitPlayerLocation = ex; EditorUtility.SetDirty(st); }
        }
        return go;
    }
    static Material chairMat;
    static Material ChairMat()
    {
        if (chairMat != null) return chairMat;
        string path = Root + "Materials/Theatre Bubble Chair.mat"; chairMat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (chairMat == null) { chairMat = new Material(Shader.Find("Astra/Glitter Cloud")); AssetDatabase.CreateAsset(chairMat, path); }
        chairMat.shader = Shader.Find("Astra/Glitter Cloud");
        chairMat.SetColor("_Top", new Color(1.05f, .86f, .98f)); chairMat.SetColor("_Bottom", new Color(.86f, .55f, .82f)); chairMat.SetColor("_Rim", new Color(1.4f, 1.1f, 1.5f));
        chairMat.SetFloat("_Glitter", 1.2f); chairMat.SetFloat("_Pastel", .12f); chairMat.enableInstancing = true; EditorUtility.SetDirty(chairMat);
        return chairMat;
    }
    static bool FloorAt(Vector3 p, out float y)
    {
        RaycastHit h; y = 0;
        if (!Physics.Raycast(p + Vector3.up * 3f, Vector3.down, out h, 8f, ~0, QueryTriggerInteraction.Ignore)) return false;
        y = h.point.y; return true;
    }
    static void Theatre()
    {
        CloudPieces();
        var scene = EditorSceneManager.GetActiveScene();
        foreach (var old in SceneTransforms().Where(t => t.name == TheatreName).ToList()) Object.DestroyImmediate(old.gameObject);
        Physics.SyncTransforms();
        var cinema = GameObject.Find("Cinema - synchronized playlist");
        if (cinema == null) { report.Add("theatre: big screen (Cinema - synchronized playlist) not found"); return; }
        var screenR = cinema.GetComponentsInChildren<MeshRenderer>(true).Where(r => r.enabled).OrderByDescending(r => r.bounds.size.x * r.bounds.size.y + r.bounds.size.z * r.bounds.size.y).FirstOrDefault();
        if (screenR == null) { report.Add("theatre: no screen renderer"); return; }
        var sb = screenR.bounds;
        var desc = Object.FindObjectOfType<VRCSceneDescriptor>();
        Vector3 spawn = desc != null && desc.spawns != null && desc.spawns.Length > 0 && desc.spawns[0] != null ? desc.spawns[0].position : Vector3.zero;
        // the screen faces whichever way its thin axis points toward the spawn
        Vector3 normal = sb.size.x < sb.size.z ? Vector3.right : Vector3.forward;
        if (Vector3.Dot(spawn - sb.center, normal) < 0) normal = -normal;
        Vector3 across = Vector3.Cross(Vector3.up, normal).normalized;
        float screenW = Vector3.Scale(sb.size, new Vector3(Mathf.Abs(across.x), 0, Mathf.Abs(across.z))).magnitude;
        float floorY; if (!FloorAt(new Vector3(sb.center.x, sb.min.y + 1f, sb.center.z) + normal * 3f, out floorY)) floorY = spawn.y;
        var root = new GameObject(TheatreName).transform; UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root.gameObject, scene);
        var focus = new Vector3(sb.center.x, sb.center.y, sb.center.z);
        float dist = Mathf.Clamp(screenW * .95f, 6f, 14f);
        report.Add("theatre: screen " + screenW.ToString("0.0") + " m wide, centre " + sb.center.ToString("0.0") + ", facing " + normal.ToString("0.0") + "; base row " + dist.ToString("0.0") + " m out");

        // --- base row on the floor
        var baseRow = new GameObject("Base row").transform; baseRow.SetParent(root, false);
        int placed = 0, skipped = 0; int seats = 9; float span = Mathf.Min(70f, Mathf.Rad2Deg * (screenW * 1.1f) / dist);
        var centreFloor = new Vector3(sb.center.x, floorY, sb.center.z);
        for (int i = 0; i < seats; i++)
        {
            float a = Mathf.Lerp(-span / 2f, span / 2f, i / (seats - 1f)) * Mathf.Deg2Rad;
            var dir = normal * Mathf.Cos(a) + across * Mathf.Sin(a);
            var p = centreFloor + dir * dist; float fy;
            if (!FloorAt(p, out fy) || Mathf.Abs(fy - floorY) > .6f) { skipped++; continue; }          // stair opening or a drop
            p.y = fy;
            if (Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(spawn.x, 0, spawn.z)) < 1.6f) { skipped++; continue; }   // keep the spawn clear
            if (Physics.OverlapBox(p + Vector3.up * .8f, new Vector3(.45f, .6f, .45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore).Length > 0) { skipped++; continue; }
            Chair(baseRow, p, new Vector3(focus.x, p.y, focus.z), null, 2.5f, true, "Theatre chair " + (i + 1)); placed++;
        }
        report.Add("theatre: base row " + placed + " chairs on the floor (" + skipped + " spots skipped: stair opening, spawn or something already there)");

        // --- private chairs above: pairs on floating clouds behind the base row
        var boxes = new GameObject("Private cloud boxes").transform; boxes.SetParent(root, false);
        int pairs = 0;
        foreach (float deg in new[] { -24f, 0f, 24f })
        {
            float a = deg * Mathf.Deg2Rad; var dir = normal * Mathf.Cos(a) + across * Mathf.Sin(a);
            var centre = centreFloor + dir * (dist + 4.5f) + Vector3.up * 2.8f;
            if (Physics.OverlapBox(centre + Vector3.up * .8f, new Vector3(1.8f, 1f, 1.3f), Quaternion.LookRotation(-dir), ~0, QueryTriggerInteraction.Ignore).Length > 0) continue;
            var box = new GameObject("Private box " + (pairs + 1)).transform; box.SetParent(boxes, false); box.position = centre; box.rotation = Quaternion.LookRotation(-dir, Vector3.up);
            CloudSlab("Theatre box cloud " + (pairs + 1), box, 3.4f, 2.3f, 1.25f, 14);
            var col = new GameObject("Cloud box floor"); col.transform.SetParent(box, false); col.transform.localPosition = new Vector3(0, -.03f, 0);
            col.AddComponent<BoxCollider>().size = new Vector3(3.0f, .06f, 2.0f);
            float fy = centre.y; var below = new Vector3(centre.x, floorY, centre.z) - dir * 1.2f;
            for (int k = 0; k < 2; k++)
            {
                var p = box.TransformPoint(new Vector3(k == 0 ? -.65f : .65f, 0, 0));
                Chair(box, p, new Vector3(focus.x, p.y, focus.z), below, 9f, true, "Private chair " + (k + 1));
            }
            pairs++;
        }
        report.Add("theatre: " + pairs + " private cloud boxes with 2 chairs each, 2.8 m up (click a chair from the floor to sit; you step off back on the floor)");

        // --- clouds along the top of the screen you can sit on
        var topCloud = new GameObject("Screen top cloud").transform; topCloud.SetParent(root, false);
        topCloud.position = new Vector3(sb.center.x, sb.max.y + .05f, sb.center.z) + normal * .15f;
        topCloud.rotation = Quaternion.LookRotation(normal, Vector3.up);
        CloudSlab("Theatre screen-top cloud", topCloud, screenW + 1.2f, 1.8f, 1.9f, Mathf.Clamp(Mathf.RoundToInt(screenW * 3.2f), 10, 110));
        var tcol = new GameObject("Screen top cloud floor"); tcol.transform.SetParent(topCloud, false); tcol.transform.localPosition = new Vector3(0, -.03f, 0);
        tcol.AddComponent<BoxCollider>().size = new Vector3(screenW + .8f, .06f, 1.3f);
        int topSeats = Mathf.Clamp(Mathf.RoundToInt(screenW / 1.4f), 3, 9);
        var exitFloor = centreFloor + normal * 3f;
        for (int i = 0; i < topSeats; i++)
        {
            float x = Mathf.Lerp(-screenW / 2f + .5f, screenW / 2f - .5f, topSeats == 1 ? .5f : i / (topSeats - 1f));
            var p = topCloud.TransformPoint(new Vector3(x, .1f, .25f));
            // sitting on the edge, facing the audience
            Chair(topCloud, p, p + normal * 5f, exitFloor + across * x * .3f, 80f, false, "Screen top seat " + (i + 1));
        }
        report.Add("theatre: cloud on top of the screen with " + topSeats + " seats (click the cloud from the floor to sit on it; you get off in front of the screen)");
    }

    // ---------------------------------------------------------------- 5
    static void Photos()
    {
        string dir = "Review/Photos/Round32"; Directory.CreateDirectory(dir);
        var theatre = GameObject.Find(TheatreName); var cinema = GameObject.Find("Cinema - synchronized playlist");
        if (theatre != null && cinema != null)
        {
            var rs = theatre.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Concat(cinema.GetComponentsInChildren<Renderer>().Where(r => r.enabled)).ToArray();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var scr = cinema.GetComponentsInChildren<MeshRenderer>().OrderByDescending(r => r.bounds.size.sqrMagnitude).First().bounds;
            var outv = new Vector3(b.center.x - scr.center.x, 0, b.center.z - scr.center.z).normalized;
            Shot(dir, "1 Theatre from behind the seats", b.center + outv * b.extents.magnitude * 1.1f + Vector3.up * 5f, scr.center, 60, 1920, 1080);
            Shot(dir, "2 Theatre from the side", b.center + Vector3.Cross(Vector3.up, outv) * b.extents.magnitude * 1.2f + Vector3.up * 4f, b.center, 60, 1920, 1080);
            var row = theatre.transform.Find("Base row");
            if (row != null && row.childCount > 0)
            {
                var mid = row.GetChild(row.childCount / 2).position; var away = new Vector3(mid.x - scr.center.x, 0, mid.z - scr.center.z).normalized;
                Shot(dir, "1b Base row", mid + away * 5f + Vector3.up * 2.2f, mid + Vector3.up * .6f - away * 2f, 60, 1920, 1080);
                Shot(dir, "1c Chair close", mid + away * 1.6f + Vector3.Cross(Vector3.up, away) * .9f + Vector3.up * 1.1f, mid + Vector3.up * .5f, 50, 1280, 900);
            }
            Shot(dir, "3 Screen top cloud", scr.center + outv * 9f + Vector3.up * (scr.extents.y + 2f), new Vector3(scr.center.x, scr.max.y, scr.center.z), 50, 1920, 1080);
        }
        var dj = GameObject.Find("12 - DJ cloud");
        var booth = dj != null ? dj.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "DJ booth") : null;
        if (booth != null)
        {
            var rs = booth.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var toPole = new Vector3(-b.center.x, 0, -b.center.z).normalized;
            Shot(dir, "4 DJ booth front", b.center + toPole * 6.5f + Vector3.up * 1.2f, b.center, 50, 1920, 1080);
        }
        int n = 0;
        foreach (var pk in Object.FindObjectsOfType<VRCPickup>().Take(3))
        {
            var c = pk.GetComponent<Collider>().bounds.center;
            Shot(dir, (5 + n++) + " Orb " + pk.name, c + new Vector3(2.2f, .6f, 2.2f), c, 50, 1280, 800);
        }
        report.Add("photos: " + dir);
    }
    static void Shot(string dir, string name, Vector3 pos, Vector3 look, float fov, int w, int h)
    {
        var go = new GameObject("Claude photo camera"); var cam = go.AddComponent<Camera>();
        cam.transform.position = pos; cam.transform.LookAt(look); cam.fieldOfView = fov; cam.allowHDR = true; cam.nearClipPlane = .05f; cam.farClipPlane = 350f;
        var pp = go.AddComponent<PostProcessLayer>();
        pp.Init(AssetDatabase.LoadAssetAtPath<PostProcessResources>("Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset"));
        pp.volumeLayer = ~0; pp.volumeTrigger = go.transform; pp.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
        var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGBHalf) { antiAliasing = 1 };
        cam.targetTexture = rt; cam.Render(); cam.Render(); RenderTexture.active = rt;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false); tex.ReadPixels(new Rect(0, 0, w, h), 0, 0); tex.Apply();
        File.WriteAllBytes(dir + "/" + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null; RenderTexture.active = null; rt.Release(); Object.DestroyImmediate(go); Object.DestroyImmediate(tex);
    }
}
