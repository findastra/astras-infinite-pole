using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
// One on/off switch per world item. (Claude, round 1b; cloud platforms added round 3a)
// 2026-09-26: shared. A switch flipped by anyone changes the world for everyone; late joiners get the current state.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AstraWorldItems : UdonSharpBehaviour {
 public GameObject[] objects; public Toggle[] objectToggles;
 public Renderer[] stairRenderers; public Toggle stairToggle;
 public Renderer[] screenRenderers; public Toggle screenToggle;
 public GameObject[] cloudObjects; public Toggle cloudToggle;
 [UdonSynced] private bool[] objectOn=new bool[0];
 [UdonSynced] private bool stairOn=true,screenOn=true,cloudOn=true;
 private bool got,started;
 private void Start(){
  if(!got){Read();if(Networking.IsOwner(gameObject))RequestSerialization();}
  started=true;Show();
 }
 private void Read(){
  objectOn=new bool[objects.Length];
  for(int i=0;i<objects.Length;i++)objectOn[i]=objectToggles[i]==null||objectToggles[i].isOn;
  if(stairToggle!=null)stairOn=stairToggle.isOn;
  if(screenToggle!=null)screenOn=screenToggle.isOn;
  if(cloudToggle!=null)cloudOn=cloudToggle.isOn;
 }
 private bool Differs(){
  if(objectOn.Length!=objects.Length)return true;
  for(int i=0;i<objects.Length;i++)if(objectToggles[i]!=null&&objectToggles[i].isOn!=objectOn[i])return true;
  if(stairToggle!=null&&stairToggle.isOn!=stairOn)return true;
  if(screenToggle!=null&&screenToggle.isOn!=screenOn)return true;
  if(cloudToggle!=null&&cloudToggle.isOn!=cloudOn)return true;
  return false;
 }
 public void Apply(){
  if(!started)return;
  if(Differs()){Read();Networking.SetOwner(Networking.LocalPlayer,gameObject);RequestSerialization();}
  Show();
 }
 public override void OnDeserialization(){
  got=true;
  for(int i=0;i<objects.Length&&i<objectOn.Length;i++)if(objectToggles[i]!=null&&objectToggles[i].isOn!=objectOn[i])objectToggles[i].SetIsOnWithoutNotify(objectOn[i]);
  if(stairToggle!=null)stairToggle.SetIsOnWithoutNotify(stairOn);
  if(screenToggle!=null)screenToggle.SetIsOnWithoutNotify(screenOn);
  if(cloudToggle!=null)cloudToggle.SetIsOnWithoutNotify(cloudOn);
  Show();
 }
 private void Show(){
  for(int i=0;i<objects.Length&&i<objectOn.Length;i++)if(objects[i]!=null&&objects[i].activeSelf!=objectOn[i])objects[i].SetActive(objectOn[i]);
  if(stairToggle!=null)for(int i=0;i<stairRenderers.Length;i++)if(stairRenderers[i]!=null)stairRenderers[i].enabled=stairOn;
  if(screenToggle!=null)for(int i=0;i<screenRenderers.Length;i++)if(screenRenderers[i]!=null)screenRenderers[i].enabled=screenOn;
  if(cloudToggle!=null&&cloudObjects!=null)for(int i=0;i<cloudObjects.Length;i++)if(cloudObjects[i]!=null)cloudObjects[i].SetActive(cloudOn);
 }
}
