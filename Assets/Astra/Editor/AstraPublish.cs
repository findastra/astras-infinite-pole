using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Editor;
using VRC.SDKBase.Editor.Api;

public static class AstraPublish
{
    [InitializeOnLoadMethod]
    static void QueueRequestedPublish() { EditorApplication.update += CheckRequestedPublish; }
    static void CheckRequestedPublish() { if(!File.Exists("Review/publish-request.flag") || APIUser.CurrentUser==null || !VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var b)) return; File.Delete("Review/publish-request.flag"); EditorApplication.update -= CheckRequestedPublish; ResumePublication(); }
    static async void ResumePublication() {
        try {
            var pm=UnityEngine.Object.FindObjectOfType<PipelineManager>();
            var id=pm.blueprintId;
            if(string.IsNullOrEmpty(id)) throw new Exception("No uploaded ID found in current scene.");
            var world=await VRCApi.GetWorld(id,true);
            if(world.AuthorId!=APIUser.CurrentUser.id)throw new Exception("Uploaded world owner mismatch.");
            EditorSceneManager.MarkSceneDirty(pm.gameObject.scene); EditorSceneManager.SaveScene(pm.gameObject.scene);
            File.WriteAllText("Review/publish-status.txt","Verified upload: "+world.Name+" by "+world.AuthorName+"\nhttps://vrchat.com/home/world/"+id);
            if(await VRCApi.GetCanPublishWorld(id)) await VRCApi.PublishWorld(id);
            world=await VRCApi.GetWorld(id,true);
            File.AppendAllText("Review/publish-status.txt","\nRelease status: "+world.ReleaseStatus+"\nLabs publication: "+world.LabsPublicationDate+"\nTags: "+string.Join(",",world.Tags));
        }catch(Exception e){File.AppendAllText("Review/publish-status.txt","\nERROR: "+e.Message);Debug.LogException(e);}
    }
    [MenuItem("Astra/Publish World to Community Labs")]
    public static async void Publish()
    {
        try
        {
            if(APIUser.CurrentUser==null)throw new Exception("Sign in to the VRChat SDK first.");
            if(!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder))throw new Exception("Open the SDK Builder tab first.");
            var pm=UnityEngine.Object.FindObjectOfType<PipelineManager>();
            if(pm==null)throw new Exception("Missing Pipeline Manager");
            Directory.CreateDirectory("Review");
            File.WriteAllText("Review/publish-status.txt","Building for account: "+APIUser.CurrentUser.displayName);
            Debug.Log("ASTRA_PUBLISH_ACCOUNT: "+APIUser.CurrentUser.displayName);
            if(string.IsNullOrWhiteSpace(pm.blueprintId)) pm.AssignId(PipelineManager.ContentType.world);
            var world=new VRCWorld{
                ID=pm.blueprintId,AuthorId=APIUser.CurrentUser.id,AuthorName=APIUser.CurrentUser.displayName,
                Name="Astra's Infinite Pole",
                Description="An endless silver pole in a glittering void. Stand on an invisible platform surrounded by 13 toggleable particle effects, adjustable color gradients, palette presets and optional ambient music. Personal controls include Low, Lush and Max density. PC VR / desktop.",
                Capacity=16,RecommendedCapacity=8,ReleaseStatus="private",
                Tags=new List<string>{"author_tag_glitter","author_tag_particles","author_tag_dance","author_tag_chill"},
                PreviewYoutubeId="",UdonProducts=new List<string>()
            };
            await builder.BuildAndUpload(world,Path.GetFullPath("Review/Glitter Preview.png"),System.Threading.CancellationToken.None);
            pm=UnityEngine.Object.FindObjectOfType<PipelineManager>();
            EditorSceneManager.MarkSceneDirty(pm.gameObject.scene);EditorSceneManager.SaveScene(pm.gameObject.scene);
            string id=pm.blueprintId;
            File.WriteAllText("Review/publish-status.txt","Uploaded: https://vrchat.com/home/world/"+id+"\nChecking Community Labs eligibility.");
            if(!await VRCApi.GetCanPublishWorld(id))throw new Exception("Upload succeeded, but VRChat currently reports this world cannot be published to Community Labs. World ID: "+id);
            var result=await VRCApi.PublishWorld(id);
            File.WriteAllText("Review/publish-status.txt","Community Labs publication request succeeded.\nhttps://vrchat.com/home/world/"+id+"\n"+result.ToString());
            Debug.Log("ASTRA_PUBLISHED: "+id);
        }
        catch(Exception e)
        {
            File.AppendAllText("Review/publish-status.txt","\nERROR: "+e.Message);
            Debug.LogException(e);
        }
    }
}





