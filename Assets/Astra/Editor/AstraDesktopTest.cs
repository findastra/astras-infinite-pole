using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;
using VRC.Editor;
using VRC.SDKBase.Editor;
using VRC.SDKBase.Editor.BuildPipeline;

public static class AstraDesktopTest
{
    [MenuItem("Astra/Prepare Desktop Test")]
    public static void Prepare()
    {
        var descriptor = Object.FindObjectOfType<VRC.SDK3.Components.VRCSceneDescriptor>();
        if(descriptor == null) throw new System.Exception("Open Astra's scene first");
        if(descriptor.GetComponent<PipelineManager>() == null) descriptor.gameObject.AddComponent<PipelineManager>();
        UpdateLayers.SetupEditorLayers();
        UpdateLayers.SetupCollisionLayerMatrix();
        VRCSettings.ForceNoVR = true;
        VRCSettings.NumClients = 1;
        VRCSettings.SDKWorldBuildType = "BuildAndTest";
        EditorSceneManager.MarkSceneDirty(descriptor.gameObject.scene);
        EditorSceneManager.SaveScene(descriptor.gameObject.scene);
        Debug.Log("ASTRA_DESKTOP_PREPARED");
    }
}
