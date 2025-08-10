# Milestones Roadmap

## Overview
We stage ambitious scale (1000 units per player) into progressive, risk-reducing milestones. Each milestone has an exit criteria and instrumentation tasks (profiling hooks) to prevent unseen technical debt.

## Milestone 0 – Repo & Data Seed (Week 0)
- [x] Design docs skeleton
- [ ] Unity project creation (`unity_project/`)
- [ ] Assembly Definitions (Simulation vs Presentation)
- [ ] JSON data stubs & importer script
- Exit: Can load data assets in empty scene & spawn one placeholder unit prefab.

## Milestone 1 – Vertical Slice Core (Weeks 1–3)
Gameplay:
- Camera (RTS: edge pan, rotate, zoom, height clamp)
- Unit selection (click + drag marquee)
- Basic command: Move
- Resource node + worker gather/deposit loop (1 resource: Wood)
- Building placement (Town Center) with footprint validation
- Production queue (train worker)
Tech:
- FixedTick simulation loop (20 Hz) decoupled from render
- Simple A* / NavMesh pathfinding integration
- Event dispatch to UI (selection changed, resources updated)
- UI: Resource bar, selection panel (HP, build queue)
Non-Functional:
- Basic profiling instrumentation (tick time, unit count)
Exit: Can play a 5-minute loop of gathering, training, moving without errors at 60 FPS with 100 units.

## Milestone 2 – Combat & Expansion (Weeks 4–7)
Gameplay:
- Additional resources (Food, Stone, Metal) + deposit buildings
- Barracks + military units (melee, ranged)
- Combat system (attack cooldown, projectile, damage types draft)
- Simple AI (auto-target, unit stances: passive/defensive/aggressive)
- Fog of War (vision update every N ticks) + hidden enemies
- Save/Load snapshot
Systems:
- Territory influence prototype (outpost building spreads influence ring)
- Weather cycle skeleton (season progression variable, no deep effects yet)
Exit: Skirmish against scripted AI with 150 units total; fog working; save/load round-trip.

## Milestone 3 – Tech & Era Progression (Weeks 8–11)
Gameplay:
- 3 Ages progression (unlock new unit tier + buildings)
- Research UI & queue
- Siege unit + armor/damage type table iteration
- Advanced worker tasks (repair, build multiple structures)
Systems:
- Optimization pass: move movement integration & combat loops into Burst Jobs OR jobified structure (if DOTS adoption begins)
- Weather impact (movement, vision)
- Territory logistics efficiency affecting gather rates
Exit: Full match loop from Age I to III under 25 minutes with 300+ units @ >55 FPS.

## Milestone 4 – Scale & Polish (Weeks 12–16)
Gameplay:
- Formation movement refinement (cohesion, facing)
- Basic tactical AI groups (defend chokepoints, raid)
- Additional map size (512×512) test
Systems:
- Batch rendering (instancing) & LOD
- Flow field experiments for large group pathing
- Determinism audit (hash state per tick)
Exit: 600 units stable, average sim tick < 6 ms on target hardware.

## Milestone 5 – Multiplayer Foundations (Weeks 17–20)
Systems:
- Lockstep command buffer implementation (single machine test harness)
- Deterministic replays (serialize command list & replay)
- OOS detection (state hash diff)
Exit: Two local clients replay same command script identically through 10k ticks.

## Stretch / Post
- Full network layer (relay, lobby, NAT traversal)
- Mod support (data overrides + new unit definitions)
- Scenario editor
- Ranked AI improvements

## Instrumentation & QA Gates
- Each milestone adds: UnitCount, AvgTickMS, P95TickMS, MemoryFootprint, DrawCalls, BatchCount metrics logged.
- Weekly performance baseline saved for regression tracking.

## Risk Mitigation Summary
- Early separation of sim/presentation prevents lockstep rewrite.
- Staged unit count ceilings avoid premature optimization while keeping perf visible.
- JSON-first data simplifies modding & diff reviews.
