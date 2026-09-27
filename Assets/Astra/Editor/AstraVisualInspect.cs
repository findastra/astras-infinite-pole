using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using VRC.SDKBase.Editor.Api;
public static class AstraVisualInspect {
 public static async void Run(){
  EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");
  string s="RENDERERS\n";
  foreach(var r in UnityEngine.Object.FindObjectsOfType<Renderer>())if(!(r is ParticleSystemRenderer))s+=r.name+" enabled="+r.enabled+" material="+string.Join(",",r.sharedMaterials.Where(m=>m!=null).Select(m=>m.shader.name))+" position="+r.transform.position+" scale="+r.transform.lossyScale+"\n";
  foreach(var v in UnityEngine.Object.FindObjectsOfType<PostProcessVolume>())s+="PP "+v.name+" "+string.Join(",",v.sharedProfile.settings.Select(x=>x.name+" active="+x.active))+"\n";
  File.WriteAllText("Review/visual-inspection.txt",s);
  try {var w=await VRCApi.GetWorld("wrld_77ed3bc8-b75b-4dd0-93ec-666edfae8b58",true);File.WriteAllText("Review/stair-reference.txt",w.Name+"\n"+w.Description+"\n"+w.ImageUrl);}catch(Exception e){File.WriteAllText("Review/stair-reference.txt","Reference unavailable: "+e.Message);}
  if(Application.isBatchMode)EditorApplication.Exit(0);
 }
}
