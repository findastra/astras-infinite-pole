using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UdonSharpEditor;
using UdonSharp.Video;
public static class AstraMagicRepair {
 public static void RepairAndCheck(){
  EditorSceneManager.OpenScene("Assets/Astra/Scenes/AstrasInfinitePole.unity");
  var p=Object.FindObjectOfType<USharpVideoPlayer>();
  // Record the serialized prefab override as well as the backing Udon variable.
  var so=new SerializedObject(p);so.FindProperty("shufflePlaylist").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();
  PrefabUtility.RecordPrefabInstancePropertyModifications(p);UdonSharpEditorUtility.CopyProxyToUdon(p);EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(p));
  EditorSceneManager.MarkSceneDirty(p.gameObject.scene);EditorSceneManager.SaveScene(p.gameObject.scene);AssetDatabase.SaveAssets();
  AstraPlayCheck.RunGlitter();
 }
}
