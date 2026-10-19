using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
// World on/off switch for cosmetic objects that have no script of their own (Claude, 2026-09-24).
// 2026-09-26: shared. Whoever flips it changes it for everyone in the instance, and late joiners get the current state.
// Bind the toggle's On Value Changed to Apply (AstraAppearanceSwitches.Ensure does this).
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AstraObjectSwitch : UdonSharpBehaviour {
 [Tooltip("Objects shown while the switch is on.")] public GameObject[] targets;
 [Tooltip("The menu switch.")] public Toggle toggle;
 [UdonSynced] private bool on=true;
 private bool got,started;
 private void Start(){
  if(!got&&toggle!=null){on=toggle.isOn;if(Networking.IsOwner(gameObject))RequestSerialization();}
  started=true;Show();
 }
 public void Apply(){
  if(toggle==null||!started)return;
  if(toggle.isOn!=on){on=toggle.isOn;Networking.SetOwner(Networking.LocalPlayer,gameObject);RequestSerialization();}
  Show();
 }
 public override void OnDeserialization(){got=true;if(toggle!=null&&toggle.isOn!=on)toggle.SetIsOnWithoutNotify(on);Show();}
 private void Show(){
  if(targets==null)return;
  for(int i=0;i<targets.Length;i++)if(targets[i]!=null&&targets[i].activeSelf!=on)targets[i].SetActive(on);
 }
}
