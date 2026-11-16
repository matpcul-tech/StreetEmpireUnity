# Street Empire (Unity)

The Unity 6 port of Street Empire, an open city crime game: turf, crews, jobs, cars and police.

**The current game is the Three.js version at [matpcul-tech/StreetEmpire](https://github.com/matpcul-tech/StreetEmpire), playable at https://matpcul-tech.github.io/StreetEmpire/. It stays live and stays the game people play until this port matches it.** Nothing here changes that repo.

Status: scaffold only. No gameplay yet. See [docs/PLAN.md](docs/PLAN.md) for the full plan and roadmap.

## Stack

| | |
| --- | --- |
| Editor | Unity 6.3 LTS, 6000.3.28f1 |
| Rendering | URP 17.3.0 |
| Input | Input System 1.20.1 |
| Camera | Cinemachine 3.1.7 |
| Models | glTFast 6.21.0 (GLB) |
| Multiplayer | Netcode for GameObjects 2.13.3, Unity Transport 2.7.5 |
| CI | GameCI (unity-test-runner v4, unity-builder v6), Android and WebGL |

## Layout

```
Assets/
  Data/                 ported game data as JSON (source of truth for now)
  Scripts/
    Sim/                pure C# simulation, no UnityEngine (StreetEmpire.Sim.asmdef)
    Runtime/            Unity side: data loading, ScriptableObject definitions, Boot (StreetEmpire.Runtime.asmdef)
    Editor/             project bootstrap, data asset builder, CI build entry (StreetEmpire.Editor.asmdef)
  Tests/EditMode/       parity tests against the JS game
  Art/Models|Textures|Audio, Scenes/, Settings/
Packages/manifest.json
ProjectSettings/ProjectVersion.txt
tools/                  the JS to JSON export scripts
docs/                   plan, CI license setup, porting notes
```

The simulation mirrors `src/state.js`: `Simulation.CreateState` is `createState`, `Simulation.Step` is `step`, and `LocalSession` is the seam from `net.js`. Keeping the sim free of UnityEngine lets the same code run in the client, on a headless NGO server, and in tests.

## Data

`Assets/Data/*.json` was generated from StreetEmpire `src/state.js` at commit a915d2b (v0.29):

- `crews.json`: 7 crews with colors
- `weapons.json`: 5 weapons and aim assist
- `vehicles.json`: 9 car types, 3 bike models, garages, ramps, wreck and blast numbers
- `jobs.json`: 4 contacts, 19 story jobs with briefs, fares, courier, 3 races, street calls, stashes, dice
- `police.json`: 5 star levels, helicopter, heat decay, heat sources, bust and bail
- `world.json`: grid and block sizes, districts, 81 hustles, costs, shops, safehouses

Regenerate after the JS changes:

```
node tools/dump_state.mjs ../StreetEmpire/src/state.js > /tmp/se.json
python3 tools/port_data.py /tmp/se.json Assets/Data
```

Positions stay in the source three.js space. Convert through `WorldSpace.FromSource` (X is negated, the same way glTFast imports GLB).

In the Editor, **Street Empire > Build Data Assets** writes one ScriptableObject per crew, weapon, vehicle, job and contact under `Assets/Data/Definitions`.

## First open

This project was scaffolded by hand, without running the Editor, so there are no `.meta` files, scenes or pipeline assets yet. On first open in Unity Hub (Add project from disk), `ProjectBootstrap` creates the URP asset, `GameDatabase.asset` and `Scenes/Boot.unity`, and sets Input System as the active input handler. Commit the generated `.meta` files, `Packages/packages-lock.json` and `ProjectSettings/` after that first open.

## CI

`.github/workflows/unity-ci.yml` runs EditMode tests, then builds Android (APK) and WebGL. It needs two repo secrets, UNITY_EMAIL and UNITY_PASSWORD, before it can pass; see [docs/CI-LICENSE.md](docs/CI-LICENSE.md). Until then it fails at license activation, which is expected.

## Git LFS

Art, audio and video go through Git LFS (`.gitattributes`). Run `git lfs install` once before adding any model, texture or sound.
