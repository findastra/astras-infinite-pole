# Handoff: the first squishy jelly bed in Astra's Infinite Pole (Claude, 2026-10-26)

status: READY_FOR_REVIEW. Placed, compiles, desktop test entered the world with no Udon errors. Not felt with a body in
the client yet, not uploaded.

## What Astra asked

After a long search for "the VRChat bubble bed that molds to your body", she downloaded DynamicMochiMochiShader (free,
Booth 3129610, MIT, by OmochiNobiru; the shader Dream Waves' jelly waterbeds use) and said "lets make our first chair in
astras infinite pole".

## What I did

- **Package:** `Assets/DynamicMochiMochi/` copied from DynamicMochiMochiShader12.unitypackage with its own GUIDs, unchanged.
  Its two UdonSharp scripts compiled; their programs are the two new files in `Assets/SerializedUdonPrograms/`.
- **Helper:** `Assets/Astra/Editor/ClaudeJellyBed20261026.cs`, menus Astra/Claude/Jelly bed 1 survey, 2 place, 3 remove.
  Reports and photos go to `Review/jelly-bed-20261026/` (private).
- **The bed:** scene root "Jelly bed (Claude)" at (-2.98, 8.20, 30.01), 15.8 m from the Sleepy nook, facing it. The
  helper picked the nearest spot with nothing inside a 6 x 6 x 2.2 m box, outside every orbiting cloud's path, and with
  walkable ground at the same height within 1.5 m.
  - `MochiMochiJelly` from the package, 5 x 5 x 1.3 m. Its dent camera already sees the Player, PlayerLocal and Pickup
    layers, so bodies press in.
  - "Jelly floor": its own 0.06 m walk slab, top flush with the jelly's bottom, under a glossy deep-blue base
    (`Assets/Astra/Materials/Jelly bed base.mat`).
  - "Jelly shadow sun": the jelly needs a shadow-casting directional light, and the world's "Soft avatar light" has none.
    This one has hard shadows, intensity 0.01 and lights only layer 31 (empty), so the world's look stays the same.
- **Desktop test** (2026-10-26 23:59): fresh `.vrcw`, client `--url=` names it, "Finished entering world", no Udon
  exceptions. The first try failed in the TypeDb step on a stale path to the project's old folder
  (`Documents\ChatGPT\Mommy's Discord\Astra-Infinite-Pole`); Unity rebuilt its Bee cache and the second try built.

## Open

1. **Not felt yet.** Only a body in the client shows the dent; editor photos show a dark box because the jelly's cameras
   run only in the game.
2. **Colour:** the package's jelly is green-tinted; Astra's reference clip is blue with pool tiles under it.
3. **One per world:** the package's render textures are shared, so a second bed needs its own copies.
4. The helper's spot is a guess; Astra may want it elsewhere (move the root; keep the floor and sun with it).
5. Other uncommitted files in this folder (`ClaudeLiquidFlowPhotos.cs`, `Assets/UnityUnicorn/` liquid-flow files, the
   regenerated Udon programs) are not mine and are not in this branch.

Made with Claude Opus 5.5 (`claude-opus-5-5`), 2026-10-26.
