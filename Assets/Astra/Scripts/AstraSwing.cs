using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Sit-on cloud swing (Claude, 2026-09-26): click / grab the seat to sit. It sways gently and swings higher while someone rides.
// Motion runs on server time so everyone sees the same swing.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraSwing : UdonSharpBehaviour
{
    public Transform pivot;
    public float idleAmp = 5f, rideAmp = 22f, speed = 1.25f, phase;
    private float amp; private int riders; private Quaternion baseRot;
    private void Start() { amp = idleAmp; if (pivot != null) baseRot = pivot.localRotation; }
    public override void Interact() { Networking.LocalPlayer.UseAttachedStation(); }
    public override void OnStationEntered(VRCPlayerApi player) { riders++; }
    public override void OnStationExited(VRCPlayerApi player) { riders = Mathf.Max(0, riders - 1); }
    private void Update()
    {
        if (pivot == null) return;
        amp = Mathf.Lerp(amp, riders > 0 ? rideAmp : idleAmp, Time.deltaTime * .6f);
        float t = (float)(Networking.GetServerTimeInSeconds() % 3600.0);
        pivot.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t * speed + phase) * amp, 0, 0);
    }
}
