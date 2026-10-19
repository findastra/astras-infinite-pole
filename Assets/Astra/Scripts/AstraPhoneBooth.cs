using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// One of the linked phone booths: reports who steps in and out. (Claude round 3g; the banner booth joined 2026-09-26)
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraPhoneBooth : UdonSharpBehaviour {
 public AstraPhoneLine line;
 public int booth; // 0, 1, 2 ... one number per booth
 public override void OnPlayerTriggerEnter(VRCPlayerApi p){if(Utilities.IsValid(p))line.Enter(booth,p);}
 public override void OnPlayerTriggerExit(VRCPlayerApi p){if(Utilities.IsValid(p))line.Exit(booth,p);}
}
