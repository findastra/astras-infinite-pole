using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
// Stormscape (Claude, 2026-10-01). One shared switch: everything fades to a dark storm over a few seconds. Clouds turn
// to a dark rainbow gradient of greys and blacks, the sky becomes rolling storm clouds, rain falls around you, lightning
// strikes with thunder, and the crystal chandeliers blow faster. Flip it again and it all fades back.
// The shaders read two global values: _UdonStorm (0 calm .. 1 storm) and _UdonFlash (lightning brightness).
// Lightning runs on server time, so everyone in the instance sees and hears the same strikes.
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class AstraStorm : UdonSharpBehaviour
{
    public Material stormSky;
    public Transform follower;
    public ParticleSystem rain;
    public GameObject[] bolts;
    public AudioSource thunder;
    public AudioClip[] thunderClips;
    public AstraVineSway[] sways;
    public Light[] lights;
    public Text label;
    public float fadeSeconds = 3f, windInStorm = 2.6f, boltEvery = 6.5f;
    [UdonSynced] private bool on;
    private bool got, started, raining;
    private float level;
    private Material savedSky;
    private float[] savedIntensity;
    private int stormId, flashId;
    private int lastSlot = -1, thunderSlot = -1;
    private float thunderAt = -1;
    private VRCPlayerApi player;

    private void Start()
    {
        player = Networking.LocalPlayer;
        stormId = VRCShader.PropertyToID("_UdonStorm"); flashId = VRCShader.PropertyToID("_UdonFlash");
        savedIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) if (lights[i] != null) savedIntensity[i] = lights[i].intensity;
        VRCShader.SetGlobalFloat(stormId, 0); VRCShader.SetGlobalFloat(flashId, 0);
        for (int i = 0; i < bolts.Length; i++) if (bolts[i] != null) bolts[i].SetActive(false);
        if (rain != null) rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (!got && Networking.IsOwner(gameObject)) RequestSerialization();
        started = true; ShowLabel();
    }

    public void Toggle()
    {
        if (!started) return;
        on = !on; Networking.SetOwner(Networking.LocalPlayer, gameObject); RequestSerialization(); ShowLabel();
    }
    public override void OnDeserialization() { got = true; ShowLabel(); }
    private void ShowLabel() { if (label != null) label.text = on ? "STORMSCAPE  /  ON" : "STORMSCAPE  /  OFF"; }

    private float Hash(float n) { float s = Mathf.Sin(n * 127.1f + 311.7f) * 43758.5453f; return s - Mathf.Floor(s); }

    private void Update()
    {
        float target = on ? 1f : 0f;
        if (level == 0f && target == 0f) return;                 // calm: nothing to do
        float prev = level;
        level = Mathf.MoveTowards(level, target, Time.deltaTime / Mathf.Max(.1f, fadeSeconds));
        if (prev == 0f && level > 0f) savedSky = RenderSettings.skybox;

        // the sky swaps at the half-way point; if someone picks another sky mid-storm, remember it for afterwards
        if (level > .5f) { if (RenderSettings.skybox != stormSky) { savedSky = RenderSettings.skybox; RenderSettings.skybox = stormSky; } }
        else if (RenderSettings.skybox == stormSky && savedSky != null) RenderSettings.skybox = savedSky;

        if (Utilities.IsValid(player) && follower != null) follower.position = player.GetPosition();
        if (rain != null)
        {
            bool want = level > .25f;
            if (want && !raining) { rain.Play(); raining = true; }
            else if (!want && raining) { rain.Stop(true, ParticleSystemStopBehavior.StopEmitting); raining = false; }
        }
        for (int i = 0; i < sways.Length; i++) if (sways[i] != null) sways[i].wind = 1f + (windInStorm - 1f) * level;
        for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = savedIntensity[i] * (1f - .7f * level);

        // lightning: one strike per slot, at a random moment inside it, the same for everyone
        float flash = 0f;
        if (level > .8f)
        {
            double t = Networking.GetServerTimeInSeconds();
            int slot = (int)(t / boltEvery);
            float strikeAt = (float)(slot * boltEvery) + Hash(slot) * (boltEvery - 1.2f);
            float since = (float)(t - strikeAt);
            int which = bolts.Length > 0 ? Mathf.Clamp((int)(Hash(slot + 17.3f) * bolts.Length), 0, bolts.Length - 1) : -1;
            if (since >= 0f && since < .7f)
            {
                // a bright crack, a dip, a second flicker, then a fade
                flash = since < .08f ? 1f : since < .16f ? .25f : since < .26f ? .85f : Mathf.Max(0f, .6f * (1f - (since - .26f) / .44f));
                if (lastSlot != slot)
                {
                    lastSlot = slot;
                    for (int i = 0; i < bolts.Length; i++) if (bolts[i] != null) bolts[i].SetActive(i == which);
                    thunderSlot = slot; thunderAt = Time.time + .6f + Hash(slot + 5.1f) * 1.6f;
                }
            }
            else if (since >= .7f) { for (int i = 0; i < bolts.Length; i++) if (bolts[i] != null && bolts[i].activeSelf) bolts[i].SetActive(false); }
            if (thunderAt > 0f && Time.time >= thunderAt)
            {
                thunderAt = -1f;
                if (thunder != null && thunderClips.Length > 0) { thunder.volume = .55f * level; thunder.PlayOneShot(thunderClips[thunderSlot % thunderClips.Length]); }
            }
        }
        else for (int i = 0; i < bolts.Length; i++) if (bolts[i] != null && bolts[i].activeSelf) bolts[i].SetActive(false);

        VRCShader.SetGlobalFloat(stormId, level);
        VRCShader.SetGlobalFloat(flashId, flash * level);

        if (level == 0f)                                          // fully calm again: put everything back exactly
        {
            for (int i = 0; i < lights.Length; i++) if (lights[i] != null) lights[i].intensity = savedIntensity[i];
            for (int i = 0; i < sways.Length; i++) if (sways[i] != null) sways[i].wind = 1f;
            if (RenderSettings.skybox == stormSky && savedSky != null) RenderSettings.skybox = savedSky;
        }
    }
}
