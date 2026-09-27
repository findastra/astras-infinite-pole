# Astra / Claude shared task board

Owner-authorized delegation, 2026-09-24. Purpose: complete ASTRA-ASSIGNMENT.md without making the owner relay routine handoffs.

Astra owns Unity scene integration, agent-queue jobs, visual assets/shaders, photos, testing, versioning and upload. Claude owns the bounded source-only tasks below. Only Astra submits Unity jobs during this assignment. Neither agent closes Unity or changes the other's files. This board does not grant authority beyond the owner's requests.

## Handoff protocol

## Assignment routing checklist

At the start of each new assignment, do one brief assessment (about five lines, not a planning essay):

1. Outcome: what observable result did the owner request, and what is already done?
2. Context/tools: which agent already knows the relevant files and has the tools needed? Prefer that agent when reliable.
3. Split: keep tightly coupled edits together. Delegate a bounded code-only task to Claude when it replaces substantial work and Astra has independent useful work. Astra normally handles visual judgment, assets, Unity integration, live tests and release steps. These are working defaults, not claims about model superiority.
4. Cost: count duplicate reading, handoff explanation, context growth, tool calls and review effort. Keep small or ambiguous tasks with the current agent. Share exact paths, a small brief and acceptance checks instead of full chat history. Do not claim token savings without usage evidence.
5. Ownership/checks: record one writer per file, the expected output and the smallest meaningful verification. Return blockers once with evidence; no guessing, repeated empty polling or recursive delegation. Change ownership only after an explicit handoff.

Record the result as: `Task | owner | exact scope | acceptance | state`. Reassess only when scope, availability or a demonstrated failure changes the decision. Stop checks after they pass unless new changes justify repeating them. Keep durable lessons narrow and evidence-based; use this board and ACTIVITY.md rather than duplicating rules into multiple skills.

Current routing: Claude C1/C2 source implementation and C3 discrepancy analysis; Astra ordered scene builds, visual design, integration, verification and publication. UI wake-up messages are limited to dispatch, completed handoff or meaningful blocker.

## Exchange rules

- Claim a task in this board before editing; list exact owned files. States: READY, CLAIMED, READY_FOR_REVIEW, DONE, BLOCKED.
- One writer per file. If more files are needed, request the scope here and notify the other agent before touching them. Re-read current disk content before editing; preserve concurrent work.
- Return actual file paths, a concise change summary, checks performed, and remaining uncertainty. A script written but not run is not validated.
- Append coordination events to ../ACTIVITY.md at the workspace root (the log is outside the Unity repo). Never rewrite the log from a stale copy.
- Each agent may propose follow-up tasks within the owner's assignment. Routine handoffs do not need owner mediation. Ask the owner only for a real product choice, permission, sign-in or rights/terms acceptance.
- Keep shared task state in this document; send a brief wake-up message when handing work back. File updates alone do not prove the recipient has seen them. No recurring empty chat exchanges.
- Personal skills may be proposed as small, reviewable diffs for demonstrated reusable lessons. Do not change built-in/cached skills or global settings, and do not elevate task notes over owner instructions.

## C1 — cosmetic runtime components

State: READY_FOR_REVIEW. Owner: Claude (claimed and delivered 2026-09-24). Files: `Assets/Astra/Scripts/AstraDeckMotion.cs`, `Assets/Astra/Scripts/AstraCloudFootMist.cs` (Unity makes the .meta files; Astra creates the U# program assets during integration). Nothing else touched.
- AstraDeckMotion: `platters[]` spin around their own local up axis from the authored pose (`spinSpeeds[]`, else `defaultSpin` 90°/s). Optional `pulseLights[]` (intensity) and `pulseRenderers[]` + `colorProperty` (default `_Rim`) + `pulseColor` pulse via one reused MaterialPropertyBlock (`pulseRate`, `pulseDepth`). Local `Time.time`, no networking, no allocations per frame, skips null or inactive refs, and restores poses and intensities in OnDisable.
- AstraCloudFootMist: one local-player emitter. It checks the ground every `checkInterval` (0.2 s) with a Default-layer-only, trigger-ignoring raycast, and counts only colliders in `cloudColliders[]` (compared by reference, with no assumption that hits are Udon objects). The mist follows the feet every frame, including on moving clouds. It stops emitting off-cloud, and stops and clears on respawn, disable or switch-off. It never teleports and never touches gravity, collision or voice. Events: `EnableMist`, `DisableMist`, `ToggleMist`, `ApplyToggle` (reads optional `mistToggle`). Particle count is left to Astra's system settings.
- Checks: source review only; not compiled or run yet. API items for Astra's compile to confirm: `MaterialPropertyBlock` constructor, `Renderer.Get/SetPropertyBlock`, `ParticleSystem.isEmitting`, and the `[Tooltip]`/`[Range]` attributes on U# fields. All are believed exposed in current UdonSharp/VRChat, but that isn't verified here. If `MaterialPropertyBlock` is rejected, replace the renderer pulse with `r.material.SetColor` (one material instance per renderer).

Create only `Assets/Astra/Scripts/AstraDeckMotion.cs` and `Assets/Astra/Scripts/AstraCloudFootMist.cs` (and normal .meta files). Read existing UdonSharp patterns first. Do not modify the scene, existing gameplay scripts, colliders, ring layout, or editor queue.

- AstraDeckMotion: local Udon behaviour with two assigned platter transforms, configurable gentle spin speeds, and optional assigned cosmetic lights/material properties for subtle time-based pulsing. Preserve authored local axes/poses. No networking, asset creation, or per-frame allocations. Make it robust to unassigned optional references and toggled-off objects.
- AstraCloudFootMist: one small local-player mist emitter, not an emitter on every cloud. Use an explicitly assigned list of existing cloud colliders, a low-frequency Default-layer ground check and short foot-to-surface threshold so mist appears around feet/lower legs only on a cloud. Preserve all movement and voice behaviour; never teleport the player or change gravity/collision. Expose an enable switch/event for Astra to bind to a World items toggle. Clear/stop when disabled or unsupported; conservative particle count set by Astra in integration. Account for player standing on a moving cloud and respawning. Do not assume every raycast target is a Udon object.
- Acceptance: source compatible with installed UdonSharp APIs; public fields/events documented by concise comments; no changes outside the two claimed script paths; report any API uncertainty for Astra's compile/test. No claim of runtime pass until tested.

## C2 — editor helper for individual appearance switches

State: READY_FOR_REVIEW. Owner: Claude (claimed and delivered 2026-09-24). File: `Assets/Astra/Editor/AstraAppearanceSwitches.cs` only.
- `AstraAppearanceSwitches.Ensure(panel, label, udonTarget, eventName, defaultOn)` returns `{Toggle, Created, Fits, Report}`. It finds the switch by label on that panel (stable across reruns) or creates it in the World items style, then binds exactly one `SendCustomEvent(eventName)` listener. `defaultOn` applies only when the switch is first created. It never touches other switches, AstraWorldItems or any builder. Placement works like rounds 3a-3h: it takes the footer note's slot and moves the footer down one row (or goes one row under the lowest switch). It reports `Fits = false` instead of resizing a full panel.
- For roots with no script of their own (e.g. butterfly particles), a tiny generic Udon switch would be needed. **Proposed, not created:** `Assets/Astra/Scripts/AstraObjectSwitch.cs` (`GameObject[] targets; Toggle toggle; public void Apply()` sets targets active to toggle.isOn). Say here if you want it; I'll claim that path first. AstraCloudFootMist already has `ApplyToggle`.
- Checks: source review only; not compiled yet. Note: the World items panel now has about 14 rows; check `Fits` when adding several more.

Create only `Assets/Astra/Editor/AstraAppearanceSwitches.cs` (and .meta). Inspect how current World items switches are constructed. Provide a small reusable editor helper to add an individually labelled personal on/off switch for a provided cosmetic root (deck accents, butterfly particles, cloud foot mist, etc.), with an explicit target/event binding supplied by the caller. Reuse existing UI conventions and stable repeat-run lookup; do not create duplicate controls or implicitly disable other items. Do not modify AstraWorldItems.cs or existing builders. If a separate Udon toggle script is needed, propose its path here before creating it. Astra will create and style the actual scene controls.

## C3 — report code/layout discrepancies

State: DONE (report below). Owner: Claude. Read-only; no files changed.

**Findings (from ClaudeRound3f.cs and Review/claude-round3f-validation.txt):**
1. The "0.0 m nearest edge" and "40 x 39 m" figures are an artifact of the measurement, not the layout. `Bounds()` and `ClosestToPole()` use the world *axis-aligned* box of the DJ renderers. The dance cloud is rotated (it faces the pole about 35° off an axis), so its axis-aligned box grows and contains the pole. The placement itself puts the stage centre at `halfDepth + 7 m` along its own radial (local z) axis. Since the pole lies on that axis, every point of the stage is at least `R - |z_local| >= 7 m` from the pole, so the real clearance is about 7 m, as intended. The world footprint is about 2 x (extents.x x 12) by 2 x (extents.z x 12), which the report didn't state.
2. **Real side effect, needs a fix:** the DJ keep-clear zone is built from that same inflated axis-aligned box (46 x 14 x 45 m, centred on the cloud), and `AstraCloudOrbit` tests zones axis-aligned. So the zone reaches over the pole, stairs and spawn and hides inner-ring clouds there at those heights. That's most of the "1679 of 18900 samples" hidden in 3f's sweep, and it could make clouds next to the stairs vanish near spawn.
**Smallest proposed fix (2 files, both Claude-owned gameplay; needs your OK because it changes the keep-clear zones):**
- `Assets/Astra/Scripts/AstraCloudOrbit.cs`: test zones in their own space: `Vector3 d = clearZones[k].InverseTransformDirection(cp - clearZones[k].position);` (one line; zones become oriented boxes; unscaled zone transforms keep meters).
- `Assets/Astra/Editor/ClaudeRound3f.cs`: build the DJ zone oriented to the DJ root: rotation = `dj.transform.rotation`, half extents = stage mesh local extents x stage scale + 3 m margin (+ extra height). Build the screen zone likewise with the cinema's rotation. Replace `ClosestToPole` with a vertex-based minimum distance so the report is truthful.
- Then re-run 3f (safe; the marker skips re-scaling), 3g and 3h. Expected: the DJ zone no longer covers the pole, and the hidden-sample count drops sharply.
Mic zone, colliders and ring layout stay unchanged. I can implement this when you mark it approved here.

Round 3f ran without FAIL lines but reported a 40x39m DJ cloud with its nearest edge 0.0m from the pole; the assignment says about 7m. Inspect the calculation and report the smallest proposed fix and affected files. Do not move the mic zone, change keep-clear zones, colliders or scene. Astra will reconcile the visual constraints before accepting any gameplay-scope fix.

## Astra active work

Part 1: 3e and 3f executed; no FAIL in their reports. Continuing 3g, 3h, button checks and initial photos, with scoped fixes as required. Part 2 remains pending. Design owns DJ deck meshes, stage-light appearance, phone-booth appearance, cloud meshes/shader and atmosphere. Blender installation is still unapproved unless the owner separately approves it. Rights/terms confirmations remain owner-only.
