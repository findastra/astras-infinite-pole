# Claude in Astra's Infinite Pole

Read these first. They win over this file:
- Astra's global rules (`%USERPROFILE%\.claude\CLAUDE.md`): `git fetch` first, read the newest handoff (`docs/handoff-*.md`, recent commits), and never overwrite someone else's uncommitted work.
- The workspace `AGENTS.md` one folder up: the owner's boundaries; no spending; read and append `ACTIVITY.md`.
- `AGENTS.md`: the publication and version policy.
- `docs/agent-coordination.md`: the task board. Claim files before you edit them, and use one writer per file.
- Astra's PetOS (`findastra/astras-pet-os`): **Pole Puff** is this world's caretaker; **Unity Unicorn** (`findastra/unity-unicorn`) owns the reusable Unity/Blender tools.

Skills (on Astra's PC):
- `%USERPROFILE%\.claude\skills\vrchat-unity-world\SKILL.md`: agent queue, colliders, pickups, renders, desktop test.
- `%USERPROFILE%\.codex\skills\blender-unity-assets\SKILL.md`: Blender meshes. Run Blender with `--python-exit-code 1` before `--python`.

Folder (Astra, 2026-10-17): project folders live in `%USERPROFILE%\Documents\Astra's PetOS\<repo>` (for now a junction to `Astra's Pet Apps`). This world's copy still sits in `Documents\ChatGPT\Mommy's Discord\Astra-Infinite-Pole`; the move to `astras-infinite-pole` is an unapproved File Master plan row (2026-10-18). Don't move it yourself.

Git (Astra, 2026-10-12):
- Pull before you start.
- Push before you stop: commit to a branch named for the assistant and date (`claude/<topic>-YYYYMMDD`) and push it, even if unfinished. Work that is only on the PC counts as lost.
- `main` and releases change only with Astra's OK.
- This repo is public. Keep machine paths, legal names, keys and private reports (`Review/` is ignored) out of it.

Tool history: when you create or use a Unity/Blender tool, record it in Unity Unicorn's tool library. The last-use date comes from a completed run report, never from a file date. Missing evidence stays Unknown.

Project facts:
- **Unity:** 2022.3.22f1. Never close, kill or batchmode it; drive it through `Review/agent-queue/*.cmd`. Open it normally only if it's closed.
- **Git:** `git -c safe.directory=* ...`, because the repo belongs to another Windows account.
- **Clouds (2026-10-06 redo):**
  - Spiral orbit clouds use `Meshes/CloudRedo/Orbit Cloud 0-7`. Outer clouds use `Outer Cloud 0-5`, on materials `Glitter Cloud Fused` and `Glitter Cloud Fused Outer`.
  - The `Glitter Cloud 0-7` assets still build the lounge furniture. Don't overwrite them.
- **Cloud sea and banner mound (round 38):** they use `Sea billows fused` and `Banner mound fused` (from `Tools/Blender/fuse_sea.py`).
- **Cloud support colliders:** 0.06 m slabs inside the bird's-eye outline, on the same GameObject. The orbit carry needs a `"Cloud "` name.
  - Re-run `Astra/Claude/36h` (restore) before `36e` (apply).
  - For round 38, use `38r` before `38c`.
  - Validate with `36g` and `36i`.
- **Eggs and glow orbs are pickups:** audit them with `38b`. The particle ceiling in `AstraWorldBuilder.Verify` is 46,000.
- **The cloud shader's texture is opt-in** (keyword `_CLOUDTEX`). The DJ deck, booths and chairs share the shader without it.
- Upload or publish only after the owner says yes in chat. Never accept rights or terms for her.
