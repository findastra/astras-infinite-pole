# Validation: Living Magic

2026-09-12, Unity 2022.3.22f1 / VRChat Worlds SDK 3.10.5, Windows.

- Shader and Udon compilation passed.
- Scene references, preserved world identity and platform/boundary raycasts passed.
- Hard particle capacity: 11,980, including 240 cloud particles and 240 body-trail particles.
- Actual ClientSim Udon events passed: all 13 effect toggles, density, color presets, player wake updates, cloud palettes and enable/disable, trails and clearing, pole transparency, random pattern, hidden menu summon/dismiss, saved playlist shuffle setting.
- Local audio preview controls and seven sky selection events passed.

Not yet verified: physical grip-spread gesture in a headset; menu readability and targeting in PC VR; frame time/overdraw; live streaming track selection and multi-client playlist synchronization. No claim of headset performance is made.

Particle reactions are local visual displacement around the user's body and hands, not networked rigidbody simulation. Body trails are personal and off by default. Swirl paths vary by local seed and continuous time. Native USharpVideo shuffles its synchronized playlist so startup is randomized as well as later order.

Historical scenes have incomplete source history and must be treated as reconstructed snapshots rather than fully reproducible historical builds.

Final regression after moving video controls: PASS. Windows SDK world bundle build: PASS (2026-09-12). Live upload/join is still pending.

