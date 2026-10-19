using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;
// Grabbable floating glow orb (Claude, 2026-09-26): sparkles burst out while anyone holds it.
[UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
public class AstraGlowOrb : UdonSharpBehaviour
{
    public ParticleSystem sparkles;
    public override void OnPickup() { SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SparkleOn)); }
    public override void OnDrop() { SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SparkleOff)); }
    public void SparkleOn() { if (sparkles == null) return; sparkles.Play(); sparkles.Emit(40); }
    public void SparkleOff() { if (sparkles == null) return; sparkles.Stop(true, ParticleSystemStopBehavior.StopEmitting); }
}
