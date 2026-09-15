using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AstraPosterBackdrops {
 public static void RenderTwelve(){
  EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");
  string output=@"C:\Users\audra\Documents\ChatGPT\Mommy's 2\Poster Backdrops";
  Directory.CreateDirectory(output);
  var glitter=UnityEngine.Object.FindObjectOfType<AstraGlitterControls>();
  var magic=UnityEngine.Object.FindObjectOfType<AstraMagic>();
  foreach(var canvas in UnityEngine.Object.FindObjectsOfType<Canvas>())canvas.gameObject.SetActive(false);
  foreach(var r in UnityEngine.Object.FindObjectsOfType<Renderer>())r.enabled=false;
  foreach(var p in UnityEngine.Object.FindObjectsOfType<ParticleSystem>())p.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
  string[] names={"01-Amethyst-Dream","02-Rosewater-Nebula","03-Arctic-Opal","04-Peach-Sunset","05-Emerald-Enchantment","06-Midnight-Sapphire","07-Lavender-Mist","08-Ruby-Stardust","09-Golden-Hour","10-Turquoise-Twilight","11-Cotton-Candy","12-Cosmic-Iris"};
  string[] ca={"8549DC","F075AC","52B6DD","F49760","2DAA88","365ED9","A286DC","CB326D","DDA454","289CAB","E880C3","6F55BA"};
  string[] cb={"236DA4","8F4DCA","778DCA","A459A0","4B578F","773AC2","647CA4","5632AD","A76578","5741A6","679DDD","AB4E97"};
  var cameraObject=new GameObject("Poster camera");var cam=cameraObject.AddComponent<Camera>();
  cam.clearFlags=CameraClearFlags.Skybox;cam.nearClipPlane=.03f;cam.farClipPlane=350;cam.fieldOfView=78;cam.allowHDR=false;
  var sky=new Material(Shader.Find("Astra/Celestial Sky"));RenderSettings.skybox=sky;
  var cloud=new Material(magic.cloudMaterial);magic.clouds.GetComponent<Renderer>().sharedMaterial=cloud;magic.clouds.GetComponent<Renderer>().enabled=true;
  Material[] mats=new Material[glitter.effects.Length];
  for(int i=0;i<mats.Length;i++){mats[i]=new Material(glitter.materials[i]);var r=glitter.effects[i].GetComponent<Renderer>();r.sharedMaterial=mats[i];r.enabled=true;}
  var rt=new RenderTexture(3840,2400,24){antiAliasing=4};var thumb=new RenderTexture(640,400,0);
  var sheet=new Texture2D(2560,1200,TextureFormat.RGB24,false);
  for(int k=0;k<12;k++){
   Color a,b;ColorUtility.TryParseHtmlString("#"+ca[k],out a);ColorUtility.TryParseHtmlString("#"+cb[k],out b);
   sky.SetColor("_Zenith",Color.Lerp(a,b,.5f)*.055f);sky.SetColor("_NebulaA",a*.48f);sky.SetColor("_NebulaB",b*.5f);sky.SetColor("_Horizon",b*.32f);
   sky.SetFloat("_Exposure",1.05f);sky.SetFloat("_Clouds",1.8f);sky.SetFloat("_Stars",.65f);sky.SetFloat("_Seed",k*1.73f+.8f);
   cloud.SetColor("_ColorA",a);cloud.SetColor("_ColorB",b);
   magic.clouds.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);magic.clouds.useAutoRandomSeed=false;magic.clouds.randomSeed=(uint)(420+k*71);magic.clouds.Simulate(15+k*.43f,true,true);
   for(int i=0;i<mats.Length;i++){
    var ps=glitter.effects[i];ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(1000+k*137+i*17);
    mats[i].SetColor("_ColorA",Color.Lerp(a,Color.white,.32f));mats[i].SetColor("_ColorB",Color.Lerp(b,Color.white,.42f));mats[i].SetFloat("_Brightness",1.7f);
    mats[i].SetFloat("_Twinkle",.85f);mats[i].SetFloat("_FlowSeed",k*11.7f+i);mats[i].SetFloat("_React",0);
    var emission=ps.emission;emission.rateOverTime=glitter.maximumRates[i]*(.72f+(k%3)*.12f);
    var main=ps.main;main.startSizeMultiplier*=1.6f;
    ps.Simulate(8+k*.31f,true,true);
    main.startSizeMultiplier/=1.6f;
   }
   cam.transform.position=new Vector3(Mathf.Sin(k)*.8f,1.65f+(k%3)*.3f,-2.2f);
   cam.transform.rotation=Quaternion.Euler(-8+(k%4)*5,k*29,0);
   cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;
   var image=new Texture2D(3840,2400,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,3840,2400),0,0);image.Apply();
   File.WriteAllBytes(Path.Combine(output,names[k]+".png"),image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
   Graphics.Blit(rt,thumb);RenderTexture.active=thumb;
   var small=new Texture2D(640,400,TextureFormat.RGB24,false);small.ReadPixels(new Rect(0,0,640,400),0,0);small.Apply();
   sheet.SetPixels((k%4)*640,(2-k/4)*400,640,400,small.GetPixels());UnityEngine.Object.DestroyImmediate(small);
   Debug.Log("POSTER_RENDERED "+names[k]);
  }
  sheet.Apply();File.WriteAllBytes(Path.Combine(output,"Contact-Sheet.png"),sheet.EncodeToPNG());
  RenderTexture.active=null;cam.targetTexture=null;rt.Release();thumb.Release();
  File.WriteAllText(Path.Combine(output,"README.txt"),"Astra's Infinite Pole — 12 poster backdrops\n3840 x 2400 PNG, landscape, rendered directly in Unity from Fine Magic.\nOnly particle glitter, particle clouds and sky are rendered. No pole, screens, menus, text or avatars.\nContact sheet reads left to right, top to bottom, 01 through 12.\nRender-only palette, seed and camera variations; the world scene was not saved or changed.\nSource world: v0.4.0-fine-magic, commit 0eb35a0.\n");
  Debug.Log("POSTERS_COMPLETE");
 }
}
