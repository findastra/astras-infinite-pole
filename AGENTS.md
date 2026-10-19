# Publication version policy

For every project going forward:

1. Keep each independently published product in a clearly named GitHub repository under findastra unless the user requests a different owner.
2. Before publishing, commit and push the exact source and reproducible dependency versions. Create a unique release tag; never move or reuse a published tag.
3. Include the version and exact GitHub tag or commit link in the published product's description/about metadata. Never link only to a moving branch as the source version.
4. Record release date, commit, tag, destination URL and platform version in docs/publications.md. Distinguish built, uploaded, and verified-live states.
5. Create a GitHub Release with plain-language changes, validation, remaining limitations, and a link back to the publication.
6. Preserve rollback information. Do not claim an old publication maps to a new commit if its original source was not captured.
7. Keep caches, credentials, account logs and temporary upload flags out of Git. Respect third-party asset licenses.
8. Keep repositories private unless the user authorizes public source. Explain that private version links are visible only to collaborators.

9. After each world update, launch VRChat in desktop mode into the updated world or its local SDK test build, and report which version is running.

# Clouds and walkable colliders (owner request 2026-10-06)

- Clouds should look connected and organic, not like stacked round spheres.
- Every solid cloud support collider is a thin horizontal slab (0.06 m). It sits under the visible top and inside the cloud's outline seen from above: no invisible side walls, nothing sticking out.
- Seats, tables, booths, triggers and stations are not support colliders.
- Tooling: `Assets/Astra/Editor/ClaudeCloudRedo.cs` (menu Astra/Claude/36*). Notes: `CLAUDE.md`, `Review/cloud-redo/result.md`.
