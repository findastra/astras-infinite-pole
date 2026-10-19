using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Keeps an object level with the local player (height only), so an effect around the pole is always around you.
// (Claude, 2026-10-01: used by the rainbow flow.)
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraFollowHeight : UdonSharpBehaviour {
 public Transform target;
 private VRCPlayerApi player;
 private void Start(){player=Networking.LocalPlayer;}
 private void LateUpdate(){
  if(target==null)return;
  if(!Utilities.IsValid(player)){player=Networking.LocalPlayer;return;}
  Vector3 p=target.position;p.y=player.GetPosition().y;target.position=p;
 }
}
