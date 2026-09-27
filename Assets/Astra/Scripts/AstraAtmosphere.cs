using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

// Sky, sky brightness and slow evolution. 2026-09-26 (Claude): shared, so a sky picked by anyone changes it for everyone,
// and late joiners see the current sky. The world starts on the pink cloud sea from Astra's banner (startSky).
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AstraAtmosphere : UdonSharpBehaviour
{
    public Material[] skies;
    public Material[] glitter;
    public Text skyLabel, motionLabel;
    public Slider exposure;
    [Tooltip("Sky shown when the world loads (Claude 2026-09-26: the pink cloud sea from Astra's banner). Empty = Velvet Nebula.")]
    public Material startSky;
    [UdonSynced] private int sky = -1;            // -1 = start sky (pink cloud sea)
    [UdonSynced] private float exposureValue = -1;
    [UdonSynced] private bool evolving = true;
    private bool got, started;
    private void Start()
    {
        if (!got) { sky = startSky != null ? -1 : 0; exposureValue = exposure.value; if (Networking.IsOwner(gameObject)) RequestSerialization(); }
        started = true; Show();
    }
    public void Nebula() { Pick(0); }
    public void Aurora() { Pick(1); }
    public void RoseDusk() { Pick(2); }
    public void Midnight() { Pick(3); }
    public void QuietVoid() { Pick(4); }
    public void SunsetSky() { Pick(5); }
    public void MoonlitSky() { Pick(6); }
    public void PinkCloudSea() { Pick(-1); }
    private void Pick(int index) { sky = index; Share(); ShowSky(); ShowExposure(); }
    public void ApplyExposure()
    {
        if (started && Mathf.Abs(exposure.value - exposureValue) > .0001f) { exposureValue = exposure.value; Share(); }
        ShowExposure();
    }
    public void ToggleEvolution() { evolving = !evolving; Share(); ShowMotion(); }
    private void Share() { if (!started) return; Networking.SetOwner(Networking.LocalPlayer, gameObject); RequestSerialization(); }
    public override void OnDeserialization() { got = true; Show(); }
    private void Show()
    {
        if (exposureValue >= 0 && Mathf.Abs(exposure.value - exposureValue) > .0001f) exposure.SetValueWithoutNotify(exposureValue);
        ShowSky(); ShowExposure(); ShowMotion();
    }
    private void ShowSky()
    {
        if (sky < 0 && startSky != null) { RenderSettings.skybox = startSky; skyLabel.text = "PINK CLOUD SEA"; return; }
        int i = Mathf.Clamp(sky, 0, skies.Length - 1);
        string[] names = { "VELVET NEBULA", "ARCTIC AURORA", "ROSE DUSK", "MIDNIGHT STARS", "QUIET VOID", "BELFAST SUNSET", "MOONLIT SKY" };
        RenderSettings.skybox = skies[i]; skyLabel.text = names[i];
    }
    private void ShowExposure() { if (sky >= 0 && sky != 4 && sky < skies.Length) skies[sky].SetFloat("_Exposure", Mathf.Lerp(0.15f, 1.4f, exposure.value)); }
    private void ShowMotion()
    {
        for (int i = 0; i < glitter.Length; i++) { glitter[i].SetFloat("_DriftSpeed", evolving ? 0.004f : 0); glitter[i].SetFloat("_MorphSpeed", evolving ? 0.007f + i * 0.0004f : 0); }
        motionLabel.text = evolving ? "SLOW EVOLUTION  /  ON" : "SLOW EVOLUTION  /  OFF";
    }
}
