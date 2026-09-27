using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
// Generic personal on/off switch for cosmetic objects that have no script of their own (Claude, 2026-09-24).
// Bind the toggle's On Value Changed to Apply (AstraAppearanceSwitches.Ensure does this).
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraObjectSwitch : UdonSharpBehaviour {
 [Tooltip("Objects shown while the switch is on.")] public GameObject[] targets;
 [Tooltip("The menu switch.")] public Toggle toggle;
 private void Start(){Apply();}
 public void Apply(){
  if(toggle==null||targets==null)return;
  bool on=toggle.isOn;
  for(int i=0;i<targets.Length;i++)if(targets[i]!=null&&targets[i].activeSelf!=on)targets[i].SetActive(on);
 }
}
