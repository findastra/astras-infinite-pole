using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
// Living magic. 2026-09-26 (Claude): every setting (trails, reactions, flow, drifting clouds and their palette, spell pattern,
// pole look) is shared with everyone in the instance; late joiners get the current settings. The glitter, cloud volume and
// trails still follow each player's own body, so everyone sees the effect around themselves.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AstraMagic : UdonSharpBehaviour
{
    public Material[] materials;
    public Transform glitterVolume;
    public Transform cloudVolume;
    public ParticleSystem[] trails;
    public Toggle trailToggle, reactionToggle;
    public Slider flowSlider;
    public ParticleSystem clouds; public Material cloudMaterial; public Toggle cloudToggle;
    public Text patternLabel; public Material poleMaterial; public Toggle translucentToggle; public Slider poleOpacity,poleSparkle;
    [UdonSynced] private bool trailsOn, reactOn, cloudsOn, translucentOn;
    [UdonSynced] private float flow, opacity, sparkle, seed;
    [UdonSynced] private int palette = -1;   // -1 as authored, 0 peach, 1 rose, 2 twilight
    private bool got, started, pushing;
    private VRCPlayerApi player;
    private Vector3 lastPosition, wake;
    private float nextUpdate;
    private void Start()
    {
        player = Networking.LocalPlayer;
        if (!got) { Read(); seed = Random.Range(0f, 1000f); if (Networking.IsOwner(gameObject)) RequestSerialization(); }
        started = true; ShowAll();
    }
    private void Read()
    {
        trailsOn = trailToggle.isOn; reactOn = reactionToggle.isOn; cloudsOn = cloudToggle.isOn; translucentOn = translucentToggle.isOn;
        flow = flowSlider.value; opacity = poleOpacity.value; sparkle = poleSparkle.value;
    }
    private bool Near(float a, float b) { return Mathf.Abs(a - b) < .0001f; }
    private bool Differs()
    {
        return trailsOn != trailToggle.isOn || reactOn != reactionToggle.isOn || cloudsOn != cloudToggle.isOn || translucentOn != translucentToggle.isOn
            || !Near(flow, flowSlider.value) || !Near(opacity, poleOpacity.value) || !Near(sparkle, poleSparkle.value);
    }
    private void Share() { if (!started || pushing || !Differs()) return; Read(); Send(); }
    private void Send() { if (!started) return; Networking.SetOwner(Networking.LocalPlayer, gameObject); RequestSerialization(); }
    public override void OnDeserialization()
    {
        got = true; pushing = true;
        trailToggle.SetIsOnWithoutNotify(trailsOn); reactionToggle.SetIsOnWithoutNotify(reactOn); cloudToggle.SetIsOnWithoutNotify(cloudsOn); translucentToggle.SetIsOnWithoutNotify(translucentOn);
        flowSlider.SetValueWithoutNotify(flow); poleOpacity.SetValueWithoutNotify(opacity); poleSparkle.SetValueWithoutNotify(sparkle);
        pushing = false; ShowAll();
    }
    private void ShowAll() { ShowPattern(); ShowPalette(); ShowFlow(); ShowTrails(); ShowClouds(); ShowPole(); }
    public void NewPattern() { seed = Random.Range(0f, 1000f); Send(); ShowPattern(); }
    public void ApplyClouds() { Share(); ShowClouds(); }
    public void PeachClouds() { palette = 0; Send(); ShowPalette(); }
    public void RoseClouds() { palette = 1; Send(); ShowPalette(); }
    public void TwilightClouds() { palette = 2; Send(); ShowPalette(); }
    public void ApplyFlow() { Share(); ShowFlow(); }
    public void ApplyTrails() { Share(); ShowTrails(); }
    public void ApplyPole() { Share(); ShowPole(); }
    private void ShowPattern()
    {
        for (int i = 0; i < materials.Length; i++) materials[i].SetFloat("_FlowSeed", seed + i * 3.71f);
        patternLabel.text = "SPELL PATTERN  /  " + Mathf.FloorToInt(seed);
    }
    private void ShowPalette()
    {
        if (palette == 0) { cloudMaterial.SetColor("_ColorA", new Color(1, .32f, .16f)); cloudMaterial.SetColor("_ColorB", new Color(1, .65f, .38f)); }
        else if (palette == 1) { cloudMaterial.SetColor("_ColorA", new Color(1, .16f, .47f)); cloudMaterial.SetColor("_ColorB", new Color(.55f, .3f, 1)); }
        else if (palette == 2) { cloudMaterial.SetColor("_ColorA", new Color(.2f, .5f, 1)); cloudMaterial.SetColor("_ColorB", new Color(.8f, .3f, 1)); }
    }
    private void ShowClouds() { if (cloudToggle.isOn) { if (!clouds.isPlaying) clouds.Play(); } else clouds.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); }
    private void ShowFlow() { for (int i = 0; i < materials.Length; i++) materials[i].SetFloat("_Flow", flowSlider.value); }
    private void ShowTrails()
    {
        for (int i = 0; i < trails.Length; i++)
        {
            if (trailToggle.isOn) { if (!trails[i].isPlaying) trails[i].Play(); }
            else trails[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
    private void ShowPole() { poleMaterial.SetFloat("_Opacity", translucentToggle.isOn ? Mathf.Lerp(.08f, .75f, poleOpacity.value) : 1); poleMaterial.SetFloat("_Sparkle", poleSparkle.value * 4); }
    private void LateUpdate()
    {
        if (!Utilities.IsValid(player)) { player = Networking.LocalPlayer; return; }
        Vector3 feet = player.GetPosition();
        Vector3 head = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        Vector3 left = player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand).position;
        Vector3 right = player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand).position;
        Vector3 torso = Vector3.Lerp(feet, head, .52f);
        // Local simulation keeps a bounded, dense atmosphere throughout the large map.
        if (glitterVolume != null) glitterVolume.position = feet;
        if (cloudVolume != null) cloudVolume.position = feet + Vector3.down * 2f;
        if (Vector3.Distance(feet, lastPosition) > 3f) for (int i = 0; i < trails.Length; i++) trails[i].Clear();
        lastPosition = feet;
        trails[0].transform.position = left; trails[1].transform.position = right; trails[2].transform.position = torso;
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + .05f;
        if (started && !pushing && reactOn != reactionToggle.isOn) Share();   // the reaction switch has no event of its own
        wake = Vector3.Lerp(wake, Vector3.ClampMagnitude(player.GetVelocity(), 3f), .2f);
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetVector("_Body", new Vector4(torso.x, torso.y, torso.z, Mathf.Max(.4f, Vector3.Distance(feet, head) * .45f)));
            materials[i].SetVector("_HandL", new Vector4(left.x, left.y, left.z, 0));
            materials[i].SetVector("_HandR", new Vector4(right.x, right.y, right.z, 0));
            materials[i].SetVector("_Wake", new Vector4(wake.x, wake.y, wake.z, 0));
            materials[i].SetFloat("_React", reactionToggle.isOn ? 1 : 0);
        }
    }
}
