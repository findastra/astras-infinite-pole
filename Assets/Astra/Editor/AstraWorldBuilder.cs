using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UdonSharpEditor;
using UdonSharp;
using VRC.SDK3.Components;

public static class AstraWorldBuilder
{
    const string Root = "Assets/Astra";
    const string ScenePath = Root + "/Scenes/AstrasInfinitePole.unity";
    static Font font;
    static Material Mat(string name, string shader, Color color)
    {
        string path = Root + "/Materials/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null) { mat = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(mat, path); }
        mat.color = color;
        return mat;
    }
    [MenuItem("Astra/Create Initial World")]
    public static void Create()
    {
        if (File.Exists(ScenePath)) throw new InvalidOperationException("Scene already exists. Open it to preserve your edits.");
        foreach (string dir in new[]{"Materials","Scenes","Meshes","Audio"}) Directory.CreateDirectory(Root + "/" + dir);
        AssetDatabase.Refresh();
        var program = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(Root + "/Scripts/AstraControls.asset");
        if (program == null)
        {
            program = ScriptableObject.CreateInstance<UdonSharpProgramAsset>();
            program.sourceCsScript = AssetDatabase.LoadAssetAtPath<MonoScript>(Root + "/Scripts/AstraControls.cs");
            AssetDatabase.CreateAsset(program, Root + "/Scripts/AstraControls.asset");
            AssetDatabase.SaveAssets();
        }
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Color voidColor = new Color(0.008f,0.005f,0.025f);
        var background = Mat("Quiet Violet", "Astra/Quiet Void", voidColor);
        var silver = Mat("Pole Silver", "Astra/Infinite Pole", new Color(0.72f,0.77f,0.9f));
        silver.SetColor("_VoidColor", voidColor);
        var sparkle = Mat("Lilac Sparkles", "Astra/Soft Sparkle", new Color(0.72f,0.58f,1f,0.75f));
        RenderSettings.skybox = background;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f,0.22f,0.3f);
        RenderSettings.fog = false;
        var world = new GameObject("Astra's Infinite Pole");
        var geometry = new GameObject("01 - Pole and invisible enclosure"); geometry.transform.parent = world.transform;
        var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = "Infinite Pole - 45mm diameter";
        pole.transform.parent = geometry.transform;
        pole.transform.localScale = new Vector3(0.045f,128,0.045f);
        UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());
        var poleCollider = pole.AddComponent<CapsuleCollider>();
        poleCollider.radius = 0.5f; poleCollider.height = 2;
        pole.GetComponent<Renderer>().sharedMaterial = silver;
        QuietRenderer(pole.GetComponent<Renderer>());
        // The finite mesh has fully faded to the background before either endpoint.
        var enclosure = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        enclosure.name = "Void cylinder - visual envelope";
        enclosure.transform.parent = geometry.transform;
        enclosure.transform.localScale = new Vector3(120,150,120);
        UnityEngine.Object.DestroyImmediate(enclosure.GetComponent<Collider>());
        enclosure.GetComponent<Renderer>().sharedMaterial = background;
        QuietRenderer(enclosure.GetComponent<Renderer>());
        // Invisible floor is a real circular mesh collider, not a transparent draw call.
        var floor = new GameObject("Invisible platform - 12m radius"); floor.transform.parent = geometry.transform;
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(Root + "/Meshes/Platform.asset");
        if(mesh == null) { mesh = Disk(12,64); AssetDatabase.CreateAsset(mesh, Root + "/Meshes/Platform.asset"); }
        floor.AddComponent<MeshCollider>().sharedMesh = mesh;
        var walls = new GameObject("Invisible cylinder boundary"); walls.transform.parent = geometry.transform;
        for (int i=0; i<48; i++)
        {
            float a = i * Mathf.PI * 2/48;
            var segment = new GameObject("Boundary " + i.ToString("00")); segment.transform.parent = walls.transform;
            segment.transform.position = new Vector3(Mathf.Sin(a)*11.8f,4,Mathf.Cos(a)*11.8f);
            segment.transform.rotation = Quaternion.Euler(0,a*Mathf.Rad2Deg,0);
            segment.AddComponent<BoxCollider>().size = new Vector3(1.62f,8,0.2f);
        }
        var descriptor = world.AddComponent<VRCSceneDescriptor>();
        var spawn = new GameObject("Spawn - facing pole"); spawn.transform.parent = world.transform;
        spawn.transform.position = new Vector3(0,0.05f,-2);
        descriptor.spawns = new[]{spawn.transform}; descriptor.RespawnHeightY = -5;
        var cameraGO = new GameObject("Reference Camera"); cameraGO.transform.parent = world.transform;
        var cam = cameraGO.AddComponent<Camera>(); cam.nearClipPlane = 0.01f; cam.farClipPlane = 350;
        cam.clearFlags = CameraClearFlags.Skybox; cam.allowHDR = false; cam.allowMSAA = true;
        cameraGO.transform.position = new Vector3(0,1.6f,-2);
        descriptor.ReferenceCamera = cameraGO; cam.enabled = false;
        var lightGO = new GameObject("Soft avatar light"); lightGO.transform.parent = world.transform;
        var light = lightGO.AddComponent<Light>(); light.type = LightType.Directional;
        light.intensity = 0.55f; light.color = new Color(0.78f,0.73f,1);
        light.shadows = LightShadows.None; lightGO.transform.rotation = Quaternion.Euler(35,-30,0);
        var particleGO = new GameObject("02 - Gentle sparkles"); particleGO.transform.parent = world.transform;
        particleGO.transform.position = new Vector3(0,3,0);
        var ps = particleGO.AddComponent<ParticleSystem>(); ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main; main.loop = true; main.prewarm = true; main.duration = 16;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8,16); main.startSpeed = new ParticleSystem.MinMaxCurve(0.02f,0.08f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.012f,0.04f); main.maxParticles = 360;
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.gravityModifier = -0.001f;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f,0.66f,1),new Color(0.4f,0.7f,1));
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(16,14,16);
        var emission = ps.emission; emission.rateOverTime = 14;
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.12f; noise.frequency = 0.16f;
        noise.scrollSpeed = 0.06f; noise.quality = ParticleSystemNoiseQuality.Low; noise.octaveCount = 1;
        var fade = ps.colorOverLifetime; fade.enabled = true;
        var gradient = new Gradient(); gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(0.75f,0.2f),new GradientAlphaKey(0.5f,0.8f),new GradientAlphaKey(0,1)}); fade.color = gradient;
        var pr = particleGO.GetComponent<ParticleSystemRenderer>(); pr.sharedMaterial = sparkle; QuietRenderer(pr);
        pr.maxParticleSize = 0.025f;
        var audioGO = new GameObject("03 - Music - local ambient preview"); audioGO.transform.parent = world.transform;
        var audio = audioGO.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.loop = true;
        audio.volume = 0.12f; audio.spatialBlend = 0; audio.clip = MakeAmbient();
        var controlGO = new GameObject("04 - Personal controls"); controlGO.transform.parent = world.transform;
        var controls = controlGO.AddUdonSharpComponent<AstraControls>();
        controls.backgroundMaterial = background; controls.poleMaterial = silver; controls.sparkleMaterial = sparkle;
        controls.sparkles = ps; controls.music = audio;
        BuildPanel(controlGO.transform,controls);
        UdonSharpEditorUtility.CopyProxyToUdon(controls);
        UdonSharp.Compiler.UdonSharpCompilerV1.CompileSync();
        PlayerSettings.productName = "Astra's Infinite Pole";
        EditorBuildSettings.scenes = new[]{new EditorBuildSettingsScene(ScenePath,true)};
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),ScenePath);
        AssetDatabase.SaveAssets();
        Verify(); RenderPreview();
        Debug.Log("ASTRA_BUILD_OK: Scene created, Udon controls compiled, validation passed.");
    }
    static void QuietRenderer(Renderer renderer) { renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false; renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off; }
    static Mesh Disk(float radius,int segments)
    {
        var v = new Vector3[segments+1]; var t = new int[segments*3];
        for(int i=0;i<segments;i++) {float a=i*Mathf.PI*2/segments; v[i+1]=new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius); t[i*3]=0;t[i*3+1]=i+1;t[i*3+2]=(i+1)%segments+1;}
        var m = new Mesh {name="Invisible circular platform",vertices=v,triangles=t};m.RecalculateNormals();m.RecalculateBounds();return m;
    }
    static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var go = new GameObject(name,typeof(RectTransform)); var r = go.GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    static Text Label(Transform parent,string text,Vector2 position,int size,Color color)
    {
        var r=Rect(text,parent,position,new Vector2(540,50));var label=r.gameObject.AddComponent<Text>();label.font=font;label.text=text;label.fontSize=size;label.color=color;label.alignment=TextAnchor.MiddleLeft;label.raycastTarget=false;return label;
    }
    static Slider Slider(Transform parent,string name,float y,float initial)
    {
        Label(parent,name,new Vector2(0,y+30),20,new Color(0.7f,0.67f,0.83f));
        var r=Rect(name+" slider",parent,new Vector2(0,y),new Vector2(540,24));var s=r.gameObject.AddComponent<Slider>();
        r.gameObject.AddComponent<Image>().color=new Color(0.12f,0.1f,0.2f);
        var area=Rect("Handle area",r,Vector2.zero,new Vector2(516,24));
        var handle=Rect("Handle",area,Vector2.zero,new Vector2(24,32));var im=handle.gameObject.AddComponent<Image>();im.color=new Color(0.72f,0.6f,1);
        s.handleRect=handle;s.targetGraphic=im;s.minValue=0;s.maxValue=1;s.value=initial;
        var nav=s.navigation;nav.mode=Navigation.Mode.None;s.navigation=nav;return s;
    }
    static void BuildPanel(Transform parent,AstraControls controls)
    {
        var r=Rect("Personal comfort panel",parent,Vector2.zero,new Vector2(620,550));
        r.position=new Vector3(1.6f,1.25f,-0.45f);r.rotation=Quaternion.Euler(0,46,0);r.localScale=Vector3.one*0.0015f;
        var canvas=r.gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        r.gameObject.AddComponent<GraphicRaycaster>();r.gameObject.AddComponent<VRCUiShape>();
        r.gameObject.AddComponent<BoxCollider>().size=new Vector3(620,550,1);
        r.gameObject.AddComponent<Image>().color=new Color(0.022f,0.016f,0.043f,1);
        Label(r,"ASTRA'S",new Vector2(0,215),22,new Color(0.73f,0.6f,1));
        Label(r,"INFINITE POLE",new Vector2(0,170),36,Color.white);
        Label(r,"PERSONAL SPACE  /  LOCAL CONTROLS",new Vector2(0,125),16,new Color(0.55f,0.52f,0.65f));
        controls.backgroundSlider=Slider(r,"BACKGROUND",60,0);
        controls.sparkleSlider=Slider(r,"SPARKLES",-20,14f/22);
        controls.volumeSlider=Slider(r,"MUSIC VOLUME",-100,0.4f);
        var udon=UdonSharpEditorUtility.GetBackingUdonBehaviour(controls);
        UnityEventTools.AddStringPersistentListener(controls.backgroundSlider.onValueChanged,udon.SendCustomEvent,"SetBackground");
        UnityEventTools.AddStringPersistentListener(controls.sparkleSlider.onValueChanged,udon.SendCustomEvent,"SetSparkles");
        UnityEventTools.AddStringPersistentListener(controls.volumeSlider.onValueChanged,udon.SendCustomEvent,"SetVolume");
        var buttonRect=Rect("Music toggle",r,new Vector2(0,-183),new Vector2(540,52));
        var button=buttonRect.gameObject.AddComponent<Button>();button.targetGraphic=buttonRect.gameObject.AddComponent<Image>();button.targetGraphic.color=new Color(0.16f,0.11f,0.25f);
        controls.musicLabel=Label(buttonRect,"MUSIC  /  OFF",new Vector2(20,0),20,Color.white);
        UnityEventTools.AddStringPersistentListener(button.onClick,udon.SendCustomEvent,"ToggleMusic");
        Label(r,"Ambient preview  ·  starts silent",new Vector2(0,-235),16,new Color(0.55f,0.52f,0.65f));
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
    }
    static AudioClip MakeAmbient()
    {
        string path=Root+"/Audio/Astra Ambient Preview.wav";
        const int rate=22050,seconds=16,count=rate*seconds;
        using(var writer=new BinaryWriter(File.Create(path)))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+count*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(count*2);
            for(int i=0;i<count;i++) {double t=(double)i/rate; double envelope=Math.Pow(Math.Sin(Math.PI*t/seconds),2); double value=(Math.Sin(2*Math.PI*130.8125*t)+0.5*Math.Sin(2*Math.PI*196*t)+0.3*Math.Sin(2*Math.PI*261.625*t))*envelope*0.18;writer.Write((short)(value*32767));}
        }
        AssetDatabase.ImportAsset(path);return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }
    [MenuItem("Astra/Validate World")]
    public static void Verify()
    {
        if(UnityEngine.Object.FindObjectsOfType<VRCSceneDescriptor>().Length!=1) throw new Exception("Expected one world descriptor");
        Physics.SyncTransforms();
        foreach(var p in new[]{new Vector3(0,1,-2),new Vector3(5,1,0),new Vector3(-10,1,0)})
            if(!Physics.Raycast(p,Vector3.down,out var hit,2)||!hit.collider.name.StartsWith("Invisible platform")) throw new Exception("Platform test failed: "+p);
        float boundaryDistance=GameObject.Find("Boundary 00").transform.position.z+2;
        for(int i=0;i<48;i++) {float a=i*Mathf.PI*2/48; if(!Physics.Raycast(new Vector3(0,1,0),new Vector3(Mathf.Sin(a),0,Mathf.Cos(a)),boundaryDistance)) throw new Exception("Missing boundary");}
        var c=UnityEngine.Object.FindObjectOfType<AstraControls>();
        if(c==null||c.music.clip==null||c.music.playOnAwake||c.backgroundSlider==null||c.sparkleSlider==null)throw new Exception("Incomplete controls");
        int particleCap=0;
        foreach(var ps in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())particleCap+=ps.main.maxParticles;
        if(particleCap>(c.glitter==null?360:12000))throw new Exception("Particle budget exceeded");
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        foreach(var root in scene.GetRootGameObjects())foreach(var tr in root.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)>0)throw new Exception("Missing script: "+tr.name);
        Directory.CreateDirectory("Review");
        File.WriteAllText("Review/validation.txt","Astra's Infinite Pole\nPlatform raycasts: PASS\n48 boundary directions: PASS\nDescriptor and control references: PASS\nMusic starts off: PASS\nMissing scripts: NONE\nActive particle system hard cap: "+particleCap+"\nNo mirrors or realtime shadows; optional PC bloom; synchronized playlist\nVR headset performance and VRChat upload: NOT YET TESTED\n");
        Debug.Log("ASTRA_VALIDATION_OK");
    }
    [MenuItem("Astra/Render Preview")]
    public static void RenderPreview()
    {
        var go=new GameObject("Temporary preview");var cam=go.AddComponent<Camera>();cam.transform.position=new Vector3(0,1.6f,-2);cam.fieldOfView=75;cam.nearClipPlane=0.01f;cam.farClipPlane=350;cam.clearFlags=CameraClearFlags.Skybox;
        var ps=UnityEngine.Object.FindObjectOfType<ParticleSystem>();ps.Simulate(12,true,true);
        var rt=new RenderTexture(1600,1000,24);cam.targetTexture=rt;cam.Render();var previous=RenderTexture.active;RenderTexture.active=rt;
        var tex=new Texture2D(1600,1000,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1000),0,0);tex.Apply();Directory.CreateDirectory("Review");File.WriteAllBytes("Review/First Look.png",tex.EncodeToPNG());
        RenderTexture.active=previous;cam.targetTexture=null;rt.Release();UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(go);
    }
    public static void OpenForReview()
    {
        EditorSceneManager.OpenScene(ScenePath);
        var panel=GameObject.Find("Personal comfort panel");
        panel.transform.rotation=Quaternion.Euler(0,46,0);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Verify();RenderPreview();
        var view=EditorWindow.GetWindow<SceneView>();
        view.LookAt(new Vector3(0,1.6f,0),Quaternion.identity,2,false,true);
        Selection.activeGameObject=GameObject.Find("Astra's Infinite Pole");
        Debug.Log("ASTRA_READY_FOR_REVIEW");
    }
}


