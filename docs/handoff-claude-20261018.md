# Handoff — Claude, 2026-10-18: realistic booth payphones (round 39)

Branch `claude/realistic-payphone-20261018`, built on `claude/cloud-rounds-36-38-20261017`. Pushed, **not merged**, **not uploaded**.

## What I did
- **Astra's request:** make the booth payphone look realistic, and research what an AI needs to do that.
  - Approach: Unity Unicorn's rule for realistic props: start from something real, never from primitives.
  - Guide: `docs/realistic-props-20261018.md` in findastra/unity-unicorn, branch `claude/realistic-props-20261018`.
- **Base model:** Poly Haven "Korean Public Payphone 01" by UM JOORIN, CC0. I downloaded the 1k glTF (5.6 MB) with Astra's OK, into a private temp folder; the download itself is not in Git.
- **`Tools/Blender/restyle_payphone.py`** (Blender 4.5.9, `--python-exit-code 1`):
  - Steel becomes rose-pink metal, with its scratches and grime kept. The orange card unit becomes hot pink.
  - The company logo and a branded transit card are painted out of the albedo, normal, occlusion and gloss maps.
  - The mesh is decimated from 48,132 to 16,000 triangles with UVs kept.
  - It writes the mesh JSON and a Standard texture set into `Assets/Astra/Textures/Payphone/`.
- **`Assets/Astra/Editor/ClaudeRound39.cs`:**
  - **39 (apply):** builds the mesh asset `Meshes/Payphone/Payphone pink.asset` and the material `Materials/Payphone Pink.mat` (Standard; albedo, metallic-smoothness, normal, occlusion; instanced). In the three booths (Banner booth, Phone booth 1, Phone booth 2):
    - the old flat **Payphone detail** is switched off (kept);
    - **Payphone (CC0 pink)** is mounted flush on the back wall at true size (0.74 m), centred on the old face;
    - the booths get a copy of the sign-glow mesh without the lit backboard the old flat phone used to cover (12 of 108 triangles; the shared original is unchanged).
  - **39r** restores the scene. **39p** takes photos. **39s** runs a read-only survey.
  - Phone triggers, `AstraPhoneBooth` and the phone line are untouched.
- **`docs/third-party.md`:** credit added.
- **Checks:**
  - Before and after photos in Astra's ignored `Review/round39/`.
  - The restyle check render.
  - Desktop test: fresh bundle `…1f036999….vrcw` entered at 04:23:37 with no Udon errors.

## Open
- Re-running `AstraClaudeLounge` would rebuild the old flat payphone detail. Run 39 again afterwards.
- **In world light:** in the booth's strong pink light the phone's shaded side reads very dark. A reflection probe or a small fill light per booth would help. Stormscape doesn't dim it, because it uses the Standard shader.
- **Remaining text:** one line of small Korean print on the instruction card still names the operator. It's readable instruction text, not a logo, so it was left.
- **Source resolution:** the textures are 1k. The 2k set from the same CC0 source would be sharper close up in VR; downloading it needs Astra's OK.
- **Not checked yet:** I haven't looked inside VRChat myself, and frame time hasn't been checked in a headset.
