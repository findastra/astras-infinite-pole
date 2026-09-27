using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Rubber ducky drifting and bobbing around the bath (Claude, 2026-09-26). Server time keeps everyone in step.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraDuckFloat : UdonSharpBehaviour
{
    public float rx = 3f, rz = 2.5f, speed = .12f, bob = .06f;
    private Vector3 center;
    private void Start() { center = transform.localPosition; }
    private Vector2 P(float a) { return new Vector2(Mathf.Cos(a) * rx * .6f + Mathf.Sin(a * 2.3f) * rx * .18f, Mathf.Sin(a) * rz * .6f + Mathf.Cos(a * 1.7f) * rz * .18f); }
    private void Update()
    {
        float t = (float)(Networking.GetServerTimeInSeconds() % 3600.0);
        float a = t * speed; Vector2 p = P(a), q = P(a + .03f);
        transform.localPosition = center + new Vector3(p.x, Mathf.Sin(t * 1.4f) * bob, p.y);
        float heading = Mathf.Atan2(q.x - p.x, q.y - p.y) * Mathf.Rad2Deg;
        transform.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.1f) * 4f, heading, Mathf.Sin(t * .9f) * 3f);
    }
}
