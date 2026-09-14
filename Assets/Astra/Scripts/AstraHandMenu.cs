using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraHandMenu : UdonSharpBehaviour {
    public Transform menuRoot;
    private bool leftGrip,rightGrip,armed;
    private float cooldown, closeSince = -1;
    public override void InputGrab(bool value,UdonInputEventArgs args) {
        if(args.handType==HandType.LEFT)leftGrip=value;else rightGrip=value;
        // Gesture also works without controller grip buttons.
    }
    private void Update() {
        var p=Networking.LocalPlayer;if(!Utilities.IsValid(p))return;
        if(!p.IsUserInVR()){if(Input.GetKeyDown(KeyCode.M))ToggleMenu();return;}
        if(Time.time<cooldown)return;
        var l=p.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
        var r=p.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
        float distance=Vector3.Distance(l,r);
        var head=p.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        bool inReach=Vector3.Distance((l+r)*.5f,head)<.8f;
        if(distance<.22f && inReach){if(closeSince<0)closeSince=Time.time;if(Time.time-closeSince>.35f || (leftGrip&&rightGrip))armed=true;}
        else closeSince=-1;
        if(!inReach)armed=false;
        if(armed&&distance>.65f){armed=false;cooldown=Time.time+1.5f;closeSince=-1;ToggleMenu();}
    }
    public void HideMenu(){menuRoot.gameObject.SetActive(false);}
    public void ToggleMenu(){
        if(menuRoot.gameObject.activeSelf){HideMenu();return;}
        var p=Networking.LocalPlayer;if(!Utilities.IsValid(p))return;
        var head=p.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        Vector3 forward=head.rotation*Vector3.forward;forward.y=0;if(forward.sqrMagnitude<.01f)forward=p.GetRotation()*Vector3.forward;forward.Normalize();
        menuRoot.position=head.position+forward*.8f+Vector3.down*.18f;
        menuRoot.rotation=Quaternion.LookRotation(forward,Vector3.up);
        menuRoot.gameObject.SetActive(true);
    }
}

