using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
// Crystal chandelier strands sway like avatar hair: each strand is a 3-link chain, lower links lag and swing wider,
// with slow wind gusts. Pauses when you are far away (Claude, 2026-09-26).
// 2026-10-01 (Claude): `wind` (set by the Stormscape) speeds the sway up and swings it wider. The sway clock starts from
// server time so everyone sees the same motion, then runs on its own so changing the wind never makes strands jump.
[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class AstraVineSway : UdonSharpBehaviour
{
    public Transform[] segs;
    public float[] phase;
    public int[] depth;
    public float amp = 7f, freq = .7f, activeRange = 55f;
    [HideInInspector] public float wind = 1f;
    private VRCPlayerApi player; private int frame; private float clock; private bool clockSet;
    private void Start() { player = Networking.LocalPlayer; }
    private void Update()
    {
        if (!clockSet) { clock = (float)(Networking.GetServerTimeInSeconds() % 3600.0); clockSet = true; }
        float w = Mathf.Max(1f, wind);
        clock += Time.deltaTime * w;
        if (clock > 3600f) clock -= 3600f;
        if (Utilities.IsValid(player) && Vector3.Distance(player.GetPosition(), transform.position) > activeRange) return;
        frame++; if ((frame & 1) == 1) return;
        float t = clock;
        float gust = 1f + .45f * Mathf.Sin(t * .21f) + .25f * Mathf.Sin(t * .57f + 1.3f);
        float wa = 1f + (w - 1f) * .55f;                       // storm: wider swings as well as faster
        for (int i = 0; i < segs.Length; i++)
        {
            int d = depth[i]; float p = phase[i] - d * .8f; float a = amp * (.45f + .4f * d) * gust * wa;
            segs[i].localRotation = Quaternion.Euler(Mathf.Sin(t * freq * (1f + .12f * d) + p) * a, 0, Mathf.Cos(t * freq * .83f + p * 1.3f) * a * .8f);
        }
    }
}
