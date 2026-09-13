using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UdonSharpEditor;
public static class AstraMagicReview {
 public static void Finish(){
  EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");
  var m=Object.FindObjectOfType<AstraMagic>();var g=Object.FindObjectOfType<AstraGlitterControls>();var a=Object.FindObjectOfType<AstraAtmosphere>();
  var mats=g.materials.ToList();
  for(int i=0;i<m.trails.Length;i++){
   string path="Assets/Astra/Materials/Body Stardust "+i+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
   if(mat==null){mat=new Material(m.trails[i].GetComponent<Renderer>().sharedMaterial);AssetDatabase.CreateAsset(mat,path);}
   mat.SetFloat("_Flow",0);mat.SetFloat("_React",0);m.trails[i].GetComponent<Renderer>().sharedMaterial=mat;if(!mats.Contains(mat))mats.Add(mat);
  }
  g.materials=mats.ToArray();a.glitter=g.materials;UdonSharpEditorUtility.CopyProxyToUdon(g);UdonSharpEditorUtility.CopyProxyToUdon(a);
  var hm=Object.FindObjectOfType<AstraHandMenu>();if(hm.menuRoot is RectTransform){
   var oldRoot=hm.menuRoot;var anchor=new GameObject("Summoned spellbook anchor");
   while(oldRoot.childCount>0)oldRoot.GetChild(0).SetParent(anchor.transform,true);
   hm.menuRoot=anchor.transform;Object.DestroyImmediate(oldRoot.gameObject);UdonSharpEditorUtility.CopyProxyToUdon(hm);
  }
  hm.menuRoot.position=new Vector3(0,1.5f,-1);hm.menuRoot.rotation=Quaternion.identity;hm.menuRoot.gameObject.SetActive(true);
  var cinema=GameObject.Find("Cinema - synchronized playlist");
  if(cinema!=null&&PrefabUtility.IsPartOfPrefabInstance(cinema))PrefabUtility.UnpackPrefabInstance(cinema,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  var controls=Resources.FindObjectsOfTypeAll<Canvas>().FirstOrDefault(x=>x.gameObject.scene.IsValid()&&x.name=="ControlsUI");
  if(controls!=null){controls.transform.SetParent(hm.menuRoot,false);controls.transform.localScale=Vector3.one*.00045f;controls.transform.localRotation=Quaternion.identity;}
  foreach(var canvas in hm.menuRoot.GetComponentsInChildren<Canvas>()){
   if(canvas.transform.parent!=hm.menuRoot){
    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/USharpVideo/USharpVideo.prefab");var original=prefab.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(x=>x.name==canvas.name);var target=(RectTransform)canvas.transform;
    if(original!=null){target.anchorMin=original.anchorMin;target.anchorMax=original.anchorMax;target.pivot=original.pivot;target.sizeDelta=original.sizeDelta;target.anchoredPosition3D=original.anchoredPosition3D;target.localScale=original.localScale;target.localRotation=original.localRotation;}continue;
   }
   canvas.transform.localPosition=canvas.name=="ControlsUI"?new Vector3(.02f,-.53f,0):new Vector3(canvas.name=="Glitter studio"?.32f:-.24f,0,0);
  }
  foreach(var t in hm.menuRoot.GetComponentsInChildren<Text>()){
   if(t.text.StartsWith("ASTRA'S  /")){t.text="ASTRA'S  /  SPELLBOOK";t.fontSize=30;t.rectTransform.sizeDelta=new Vector2(720,60);t.rectTransform.anchoredPosition=new Vector2(-140,700);}
  }
  Directory.CreateDirectory("docs/images");
  Canvas.ForceUpdateCanvases();
  File.WriteAllLines("Review/menu-layout.txt",hm.menuRoot.GetComponentsInChildren<Canvas>().Select(x=>x.name+" position "+x.transform.position+" scale "+x.transform.lossyScale+" mode "+x.renderMode+" enabled "+x.enabled));
  var cameraObject=new GameObject("Spellbook review");var cam=cameraObject.AddComponent<Camera>();cam.transform.position=new Vector3(0,1.38f,-1.85f);cam.fieldOfView=85;cam.nearClipPlane=.02f;cam.farClipPlane=350;
  var rt=new RenderTexture(1600,1200,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(1600,1200,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,1600,1200),0,0);tex.Apply();File.WriteAllBytes("docs/images/spellbook.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);Object.DestroyImmediate(cameraObject);
  hm.menuRoot.gameObject.SetActive(false);EditorSceneManager.MarkSceneDirty(m.gameObject.scene);EditorSceneManager.SaveScene(m.gameObject.scene);AssetDatabase.SaveAssets();
  File.Copy("Review/Magic Preview.png","docs/images/living-magic.png",true);
  AstraAtmosphereBuilder.Validate();
 }
}




