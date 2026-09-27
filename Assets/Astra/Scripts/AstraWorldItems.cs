using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
// One personal on/off switch per world item. (Claude, round 1b; cloud platforms added round 3a)
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraWorldItems : UdonSharpBehaviour {
 public GameObject[] objects; public Toggle[] objectToggles;
 public Renderer[] stairRenderers; public Toggle stairToggle;
 public Renderer[] screenRenderers; public Toggle screenToggle;
 public GameObject[] cloudObjects; public Toggle cloudToggle;
 private void Start(){Apply();}
 public void Apply(){
  for(int i=0;i<objects.Length;i++) if(objects[i]!=null&&objectToggles[i]!=null) objects[i].SetActive(objectToggles[i].isOn);
  if(stairToggle!=null) for(int i=0;i<stairRenderers.Length;i++) if(stairRenderers[i]!=null) stairRenderers[i].enabled=stairToggle.isOn;
  if(screenToggle!=null) for(int i=0;i<screenRenderers.Length;i++) if(screenRenderers[i]!=null) screenRenderers[i].enabled=screenToggle.isOn;
  if(cloudToggle!=null&&cloudObjects!=null) for(int i=0;i<cloudObjects.Length;i++) if(cloudObjects[i]!=null) cloudObjects[i].SetActive(cloudToggle.isOn);
 }
}
