# FrontierAges (Working Title)

3D real‑time strategy game inspired by classic large‑scale historical RTS titles, built with Unity (URP) targeting scalable simulation, readable battle clarity, and moddable data.

## Chosen Foundations (Confirmed)
- Engine: Unity LTS + URP.
- Style: Low‑poly readable silhouettes (flat shaded accents, light PBR for hero props).
- Ambition: Long‑term stretch 1000 units per player (deferred). Staged targets: Phase A 300 total, Phase B 300 per player (4 players), later optimization toward higher counts.
- Map Sizes: Start 256×256 heightmap (playable core ~128×128). Add 512×512 mid, experiment 768–1024 later.
- Ages (initial 3): Era I, Era II, Era III (placeholder naming).
- Resources (4): Food, Wood, Stone, Metal.
- Multiplayer: Defer to Milestone 5 (SP first) but maintain deterministic‑friendly simulation (fixed tick 20 Hz, isolated logic assemblies).
- Minimum Hardware Target: GTX 1050 / RX 560 @ 60 FPS medium settings; integrated GPU fallback 30 FPS.
- Differentiators:
  1. Seasonal & weather cycle affecting vision/movement/economy.
  2. Territory influence + supply efficiency (soft logistics bonuses, not hard punishment).
  3. Data‑driven moddable pipeline from day one.

## Repository Layout
```
/docs
  architecture.md
  milestones.md
  data_schemas.md
  design_pillars.md
  networking.md
/data
  units.json
  buildings.json
  resources.json
  techs.json
/prototype
  Simulation/ (early pure-C# deterministic core sketches prior to Unity integration)
unity_project_placeholder/README.md
```

Unity project itself will be created inside `unity_project/` (not yet generated) to keep design docs separate.

## Quick Start (after creating Unity project)
1. Install Unity LTS (e.g., 2023.2/2023.3 or 2024.x when stable) with Windows + WebGL modules (future optional).
2. Create project (3D URP) in `./unity_project` named `FrontierAges`.
3. Add assembly definitions:
   - `Gameplay.Simulation` (no UnityEngine dependencies for determinism core where possible).
   - `Gameplay.Presentation` (depends on Simulation + UnityEngine).
4. Import data JSON under `Assets/Data/` (will later move to Addressables).
5. Implement bootstrap: Load data -> Build prototype scene -> Spawn test units.

## Simulation Loop Concept
```
Render (variable frame) → Interpolate from last 2 fixed states
FixedTick (20 Hz):
  1. Collect + queue player/AI commands (timestamped frame)
  2. Process production & research
  3. Assign tasks (gather, build, move, attack)
  4. Pathfinding batch + movement integration
  5. Interactions (gathering, combat resolution)
  6. Economy & territory update
  7. Vision / Fog of War update
  8. Weather / season progression events
  9. AI strategic + tactical layers (staggered frames)
 10. Event dispatch (UI / FX consume)
```

## Contribution Next Steps
- Implement Unity project scaffolding
- Create ScriptableObject adapters reading `/data/*.json`
- Prototype Systems: Camera, Selection, Command, Pathfinding, Resource Loop

See `/docs/milestones.md` for milestone definitions.
