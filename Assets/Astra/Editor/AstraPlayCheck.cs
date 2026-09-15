using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UdonSharpEditor;

// Runs actual Udon events in ClientSim; never uploads or launches VRChat.
[InitializeOnLoad]
public static class AstraPlayCheck
{
    const string Flag = "Astra.PlayCheck";
    static double ready;
    static AstraPlayCheck()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Flag,false)) return;
            if(state == PlayModeStateChange.EnteredPlayMode) {ready = EditorApplication.timeSinceStartup + 8;EditorApplication.update += Tick;}
            if(state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(Flag,false);
                if(Application.isBatchMode) EditorApplication.Exit(SessionState.GetInt("Astra.PlayResult",1));
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");
        SessionState.SetBool(Flag,true);SessionState.SetInt("Astra.PlayResult",1);
        EditorApplication.isPlaying = true;
    }
    [MenuItem("Astra/Test Glitter Controls")]
    public static void RunGlitter() { SessionState.SetBool("Astra.TestGlitter",true);Run(); }
    static void Require(bool value,string message) {if(!value) throw new Exception(message);}
    static void Tick()
    {
        if(EditorApplication.timeSinceStartup < ready) return;
        EditorApplication.update -= Tick;
        try
        {
            var c = UnityEngine.Object.FindObjectOfType<AstraControls>();
            var udon = UdonSharpEditorUtility.GetBackingUdonBehaviour(c);
            Require(udon != null,"No Udon behavior");
            Require(!c.music.isPlaying,"Music must initially be off");
            c.backgroundSlider.SetValueWithoutNotify(1);udon.SendCustomEvent("SetBackground");
            Require(c.backgroundMaterial.GetColor("_Color").r > 0.07f,"Background event failed");
            Require(Mathf.Abs(c.poleMaterial.GetColor("_VoidColor").r-c.backgroundMaterial.GetColor("_Color").r)<0.001f,"Pole fade/background mismatch");
            c.sparkleSlider.SetValueWithoutNotify(0);udon.SendCustomEvent("SetSparkles");
            if(c.glitter==null)Require(c.sparkles.emission.rateOverTime.constant==0 && c.sparkles.particleCount==0,"Sparkles off failed");
            c.sparkleSlider.SetValueWithoutNotify(1);udon.SendCustomEvent("SetSparkles");
            if(c.glitter==null)Require(Mathf.Abs(c.sparkles.emission.rateOverTime.constant-22)<0.01f,"Sparkles max failed");
            else CheckGlitter(c.glitter);
            var atmosphere=UnityEngine.Object.FindObjectOfType<AstraAtmosphere>();
            if(atmosphere!=null) {
                var au=UdonSharpEditorUtility.GetBackingUdonBehaviour(atmosphere);
                string[] skyEvents={"Nebula","Aurora","RoseDusk","Midnight","QuietVoid","SunsetSky","MoonlitSky"};
                for(int i=0;i<skyEvents.Length;i++){au.SendCustomEvent(skyEvents[i]);Require(RenderSettings.skybox==atmosphere.skies[i],"Sky selection failed: "+skyEvents[i]);}
                au.SendCustomEvent("Nebula");
                atmosphere.exposure.value=0;Require(Mathf.Abs(RenderSettings.skybox.GetFloat("_Exposure")-.15f)<.001f,"Sky brightness control failed");
                atmosphere.exposure.value=.52f;
                au.SendCustomEvent("ToggleEvolution");foreach(var mat in atmosphere.glitter)Require(mat.GetFloat("_DriftSpeed")==0&&mat.GetFloat("_MorphSpeed")==0,"Evolution disable failed");
                au.SendCustomEvent("ToggleEvolution");foreach(var mat in atmosphere.glitter)Require(mat.GetFloat("_DriftSpeed")>0&&mat.GetFloat("_MorphSpeed")>0,"Evolution enable failed");
                var player=UnityEngine.Object.FindObjectOfType<UdonSharp.Video.USharpVideoPlayer>();
                Require(player!=null&&player.loopPlaylist,"Playlist player missing");
                File.WriteAllText("Review/celestial-play-check.txt","PASS: all seven sky Udon events, actual brightness UI event, evolution off/on events, video player initialization.\nOnline YouTube playback and multiple-client synchronization require live client verification.\n");
            }
            var magic=UnityEngine.Object.FindObjectOfType<AstraMagic>();
            if(magic!=null){
                var mu=UdonSharpEditorUtility.GetBackingUdonBehaviour(magic);
                Require(magic.glitterVolume!=null && Vector3.Distance(magic.glitterVolume.position,VRC.SDKBase.Networking.LocalPlayer.GetPosition())<.1f,"Glitter volume did not follow player");
                Require(magic.cloudVolume!=null && Vector3.Distance(magic.cloudVolume.position,VRC.SDKBase.Networking.LocalPlayer.GetPosition()+Vector3.up*3.5f)<.1f,"Cloud volume did not follow player");
                Require(magic.materials[0].GetFloat("_React")==1,"Player wake runtime update failed");
                magic.trailToggle.isOn=true;foreach(var trail in magic.trails)Require(trail.isPlaying,"Trail toggle failed");
                magic.trailToggle.isOn=false;foreach(var trail in magic.trails)Require(!trail.isPlaying&&trail.particleCount==0,"Trail clear failed");
                magic.cloudToggle.isOn=false;Require(!magic.clouds.isPlaying,"Cloud toggle off failed");magic.cloudToggle.isOn=true;Require(magic.clouds.isPlaying,"Cloud on failed");
                mu.SendCustomEvent("RoseClouds");Require(magic.cloudMaterial.GetColor("_ColorA").b>.4f,"Rose cloud palette failed");mu.SendCustomEvent("PeachClouds");
                magic.translucentToggle.isOn=true;Require(magic.poleMaterial.GetFloat("_Opacity")<.76f,"Translucency toggle failed");magic.translucentToggle.isOn=false;Require(magic.poleMaterial.GetFloat("_Opacity")==1,"Solid pole failed");
                float seed=magic.materials[0].GetFloat("_FlowSeed");mu.SendCustomEvent("NewPattern");Require(seed!=magic.materials[0].GetFloat("_FlowSeed"),"New pattern failed");
                var hm=UnityEngine.Object.FindObjectOfType<AstraHandMenu>();var hu=UdonSharpEditorUtility.GetBackingUdonBehaviour(hm);
                Require(!hm.menuRoot.gameObject.activeSelf,"Menu must initially be hidden");hu.SendCustomEvent("ToggleMenu");Require(hm.menuRoot.gameObject.activeSelf,"Menu summon failed");hu.SendCustomEvent("HideMenu");Require(!hm.menuRoot.gameObject.activeSelf,"Menu close failed");
                Require(UnityEngine.Object.FindObjectOfType<UdonSharp.Video.USharpVideoPlayer>().shufflePlaylist,"Shuffle not enabled");
                File.WriteAllText("Review/magic-play-check.txt","PASS: actual Udon player wake updates; body trails on/off and clear; cloud on/off and palette; translucent/solid pole; random pattern; initially hidden menu, summon and dismiss; playlist shuffle configuration.\nPhysical hand gesture, headset targeting, framerate, and live online shuffled playback remain unverified.");
            }
            var pink=UnityEngine.Object.FindObjectOfType<AstraPinkscape>();
            if(pink!=null){
                var pu=UdonSharpEditorUtility.GetBackingUdonBehaviour(pink);
                var lp=VRC.SDKBase.Networking.LocalPlayer;float gravity=lp.GetGravityStrength();
                pu.SendCustomEvent("ToggleHover");Require(lp.GetGravityStrength()==0,"Hover did not release gravity");
                pu.SendCustomEvent("ToggleHover");Require(Mathf.Abs(lp.GetGravityStrength()-gravity)<.001f,"Hover did not restore gravity");
                pu.SendCustomEvent("TogglePetals");Require(!pink.petals.isPlaying&&pink.petals.particleCount==0,"Petals did not clear");
                pu.SendCustomEvent("Pinkscape");Require(pink.petals.isPlaying&&RenderSettings.skybox==pink.pinkSky,"Pinkscape preset failed");
                pu.SendCustomEvent("ToggleBloom");Require(!pink.bloomVolume.activeSelf,"Bloom off failed");
                pu.SendCustomEvent("ToggleBloom");Require(pink.bloomVolume.activeSelf,"Bloom on failed");
                Require(GameObject.Find("Infinite Pole - 45mm diameter").GetComponentsInChildren<Collider>(true).Length==0,"Pole collider remains");
                File.WriteAllText("Review/pinkscape-play-check.txt","PASS: actual Udon hover gravity release/restoration, petals clear and preset restart, Pinkscape sky selection, bloom off/on, pole collider removal. Headset motion comfort and performance require live testing.");
            }
            c.volumeSlider.SetValueWithoutNotify(1);udon.SendCustomEvent("SetVolume");
            Require(Mathf.Abs(c.music.volume-0.3f)<0.001f,"Volume event failed");
            udon.SendCustomEvent("ToggleMusic");Require(c.music.isPlaying,"Music on failed");
            udon.SendCustomEvent("ToggleMusic");Require(!c.music.isPlaying,"Music off failed");
            // Restore material asset values, since materials persist outside Play mode.
            c.backgroundSlider.SetValueWithoutNotify(0);udon.SendCustomEvent("SetBackground");
            Directory.CreateDirectory("Review");
            File.WriteAllText("Review/play-check.txt","ClientSim / actual Udon events: PASS\nMusic initially off, on, then off: PASS\nBackground and matching pole fade: PASS\nSparkles zero/clear and maximum: PASS\nVolume limit: PASS\nVR controller interaction and headset timing: still require headset test\n");
            SessionState.SetInt("Astra.PlayResult",0);Debug.Log("ASTRA_PLAY_OK");
        }
        catch(Exception e) {Debug.LogException(e);File.WriteAllText("Review/play-check.txt","FAILED: "+e);}
        finally {EditorApplication.isPlaying=false;}
    }
    static void CheckGlitter(AstraGlitterControls g)
    {
        var u=UdonSharpEditorUtility.GetBackingUdonBehaviour(g);
        u.SendCustomEvent("Maximum");u.SendCustomEvent("AllOn");
        for(int i=0;i<g.effects.Length;i++)
        {
            Require(g.effects[i].isPlaying,"Effect failed to start: "+i);
            Require(Mathf.Abs(g.effects[i].emission.rateOverTime.constant-g.maximumRates[i])<0.01f,"Wrong maximum rate");
            g.effectToggles[i].isOn=false; // Exercise the serialized Unity UI event.
            Require(g.effects[i].isStopped && g.effects[i].particleCount==0,"Individual toggle failed: "+i);
            g.effectToggles[i].isOn=true;Require(g.effects[i].isPlaying,"Individual toggle restart failed");
        }
        u.SendCustomEvent("AllOff");foreach(var ps in g.effects)Require(ps.isStopped&&ps.particleCount==0,"All off failed");
        g.densitySlider.value=0;u.SendCustomEvent("AllOn");foreach(var ps in g.effects)Require(ps.isStopped,"Zero density overridden by all on");
        u.SendCustomEvent("Low");Require(Mathf.Abs(g.densitySlider.value-0.12f)<0.001f,"Low preset failed");
        g.hueA.value=0.2f;g.hueB.value=0.8f;g.saturation.value=1;
        foreach(var mat in g.materials)Require(Vector4.Distance(mat.GetColor("_ColorA"),Color.HSVToRGB(0.2f,1,1))<0.01f,"Color A UI event failed");
        foreach(var mat in g.materials)Require(Vector4.Distance(mat.GetColor("_ColorB"),Color.HSVToRGB(0.8f,1,1))<0.01f,"Color B UI event failed");
        g.brightness.value=0;foreach(var mat in g.materials)Require(mat.GetFloat("_Brightness")==0,"Brightness zero failed");
        g.twinkle.value=0;foreach(var mat in g.materials)Require(mat.GetFloat("_Twinkle")==0,"Twinkle off failed");
        foreach(string preset in new[]{"RoseGold","Ice","Sunset","Aurora"}) {u.SendCustomEvent(preset);Require(Vector4.Distance(g.materials[0].GetColor("_ColorA"),Color.HSVToRGB(g.hueA.value,g.saturation.value,1))<0.01f,"Palette preset failed");}
        g.brightness.value=0.65f;g.twinkle.value=0.55f;u.SendCustomEvent("Lush");
        File.WriteAllText("Review/glitter-play-check.txt","PASS: all 13 individual UI toggles, All On/Off, zero-density interaction, Low/Lush/Max, both hue UI sliders, saturation, brightness zero, twinkle off, all four palette presets. Actual Udon events ran in ClientSim.\nHeadset performance and VR controller targeting still require user testing.\n");
        Debug.Log("ASTRA_GLITTER_PLAY_OK");
    }
}



