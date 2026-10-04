using VRC.Editor;
using VRC.SDKBase.Editor;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor.Api;
using Newtonsoft.Json.Linq;

public static class AstraReleaseCheck
{
    const string WorldId="wrld_0d424078-5852-497d-adc6-c97436052355";
    public static async void Inspect()
    {
        try {
            var world=await VRCApi.GetWorld(WorldId,true);
            string report="World: "+world.Name+"\nRelease: "+world.ReleaseStatus+"\nVersion: "+world.Version+"\nUpdated: "+world.UpdatedAt.ToString("O")+"\n";
            foreach(var package in world.UnityPackages){
                report+="Package: "+package.Platform+" / Unity "+package.UnityVersion+" / asset version "+package.AssetVersion+"\n";
                var match=Regex.Match(package.AssetUrl??"",@"file/([^/]+)/(\d+)/");
                if(match.Success){
                    try {
                        var status=await VRCApi.Get<JObject>("analysis/"+match.Groups[1].Value+"/"+match.Groups[2].Value+"/security",forceRefresh:true);
                        report+="Security analysis: "+status.ToString()+"\n";
                    }catch(ApiErrorException e){report+="Security analysis unavailable: "+e.StatusCode+" / "+e.ErrorMessage+"\n";}
                    catch(Exception e){report+="Security analysis unavailable: "+e.Message+"\n";}
                }
            }
            File.WriteAllText("Review/server-status.txt",report);Debug.Log("ASTRA_SERVER_INSPECTED");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText("Review/server-status.txt","Inspection failed: "+e.Message);if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
    static double deadline;
    static bool buildOnly; static bool localTest;
    public static void DesktopTest(){localTest=true;buildOnly=true;VRCSettings.ForceNoVR=true;VRCSettings.NumClients=1;UpdateWorld();}
    public static void BuildOnly(){localTest=false;buildOnly=true;UpdateWorld();}
    // For AstraAgentQueue: the flags above are static, so reset them when uploading from an editor that already ran a test/build.
    public static void UploadNow(){localTest=false;buildOnly=false;UpdateWorld();}
    public static void UpdateWorld(){
        EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");
        EditorApplication.ExecuteMenuItem("VRChat SDK/Show Control Panel");
        deadline=EditorApplication.timeSinceStartup+120;
        EditorApplication.update+=WaitForSdk;
    }
    static void WaitForSdk(){
        if(EditorApplication.timeSinceStartup>deadline){EditorApplication.update-=WaitForSdk;File.WriteAllText("Review/reupload-status.txt","SDK account/builder was not ready. Open SDK and sign in, then retry.");if(Application.isBatchMode)EditorApplication.Exit(1);return;}
        if((!buildOnly&&APIUser.CurrentUser==null)||!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var b))return;
        EditorApplication.update-=WaitForSdk;if(buildOnly)BuildBundle(b);else Upload(b);
    }
    static async void BuildBundle(IVRCSdkWorldBuilderApi b){
        try{if(localTest){await b.BuildAndTest();File.WriteAllText("Review/desktop-test-status.txt","SDK launched the local spiral world in desktop mode. Client loading still needs visual verification.");if(Application.isBatchMode)EditorApplication.Exit(0);return;}var path=await b.Build();File.WriteAllText("Review/windows-build-status.txt","Windows world bundle built successfully: "+path+"\nOnline join remains unverified.");if(Application.isBatchMode)EditorApplication.Exit(0);}
        catch(Exception e){File.WriteAllText("Review/windows-build-status.txt","FAILED: "+e.Message);Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
    static async void Upload(IVRCSdkWorldBuilderApi builder){
        try{
            AstraAtmosphereBuilder.Validate();
            var player=UnityEngine.Object.FindObjectOfType<UdonSharp.Video.USharpVideoPlayer>();
            if(player.playlist.Length==0)throw new Exception("User playlist is required before uploading this update.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.StandaloneWindows64)throw new Exception("Expected Windows 64-bit build target");
            var world=await VRCApi.GetWorld(WorldId,true);
            if(world.AuthorId!=APIUser.CurrentUser.id)throw new Exception("SDK account is not the world owner");
            world.Description="Astra's Infinite Pole v0.10.0 | A walkable rainbow glitter spiral round an endless pole over a pink cloud sea. Cloud lounge with smooth cloud furniture, swings, crystal chandeliers, phone booths, DJ cloud and a cloud movie theatre with seats on top of the screen. STORMSCAPE switch: storm clouds, rain, lightning and thunder. Menu: hands together, pause, pull apart; desktop M. PC VR / desktop.";
            File.WriteAllText("Review/reupload-status.txt","Building Windows update for existing world. Preserving release status: "+world.ReleaseStatus);
            await builder.BuildAndUpload(world,Path.GetFullPath("docs/images/spiral.png"),System.Threading.CancellationToken.None);
            // 2026-10-04 (Claude): BuildAndUpload keeps the old description on an existing world, so push the info too
            // (looked up by name so an SDK without this call still compiles and uploads).
            try{var m=typeof(VRCApi).GetMethods().FirstOrDefault(x=>x.Name=="UpdateWorldInfo"&&x.GetParameters().Length>=2);
                if(m!=null){var args=new object[m.GetParameters().Length];args[0]=WorldId;args[1]=world;for(int i=2;i<args.Length;i++)args[i]=m.GetParameters()[i].HasDefaultValue?m.GetParameters()[i].DefaultValue:null;
                    var task=m.Invoke(null,args) as System.Threading.Tasks.Task;if(task!=null)await task;File.AppendAllText("Review/reupload-status.txt","\nDescription updated.");}
                else File.AppendAllText("Review/reupload-status.txt","\nDescription NOT updated (SDK has no UpdateWorldInfo).");}
            catch(Exception de){File.AppendAllText("Review/reupload-status.txt","\nDescription update failed: "+de.Message);}
            var updated=await VRCApi.GetWorld(WorldId,true);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            File.WriteAllText("Review/reupload-status.txt","Uploaded existing world: https://vrchat.com/home/world/"+WorldId+"\nVersion: "+updated.Version+"\nRelease: "+updated.ReleaseStatus+"\nServer processing and client join must still be checked.");
            if(Application.isBatchMode)EditorApplication.Exit(0);
        }catch(Exception e){File.AppendAllText("Review/reupload-status.txt","\nFAILED: "+e.Message);Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
    }
}






