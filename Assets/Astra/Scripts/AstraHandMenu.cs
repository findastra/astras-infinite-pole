using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraHandMenu : UdonSharpBehaviour {
    public Transform menuRoot;
    private bool leftGrip,rightGrip,armed;
    private float cooldown;
    public override void InputGrab(bool value,UdonInputEventArgs args) {
        if(args.handType==HandType.LEFT)leftGrip=value;else rightGrip=value;
        if(!leftGrip||!rightGrip)armed=false;
    }
    private void Update() {
        var p=Networking.LocalPlayer;if(!Utilities.IsValid(p))return;
        if(!p.IsUserInVR()){if(Input.GetKeyDown(KeyCode.M))ToggleMenu();return;}
        if(Time.time<cooldown||!leftGrip||!rightGrip)return;
        var l=p.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
        var r=p.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
        float distance=Vector3.Distance(l,r);
        if(distance<.3f)armed=true;
        if(armed&&distance>.65f){armed=false;cooldown=Time.time+1;ToggleMenu();}
    }
    public void HideMenu(){menuRoot.gameObject.SetActive(false);}
    public void ToggleMenu(){
        if(menuRoot.gameObject.activeSelf){HideMenu();return;}
        var p=Networking.LocalPlayer;if(!Utilities.IsValid(p))return;
        var head=p.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 forward=head.rotation*Vector3.forward;forward.y=0;forward.Normalize();
        menuRoot.position=head.position+forward*.8f+Vector3.down*.18f;
        menuRoot.rotation=Quaternion.LookRotation(forward,Vector3.up);
        menuRoot.gameObject.SetActive(true);
    }
}
