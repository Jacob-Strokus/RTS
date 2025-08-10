# Architecture Overview

Working Title: FrontierAges

## Chosen Parameters (User + Expert Synthesis)
- Engine: Unity LTS (URP)
- Visual Style: Low-poly readable silhouettes + flat lit accents (hybrid URP with simple PBR for important props)
- Max Units Target: Ambition 1000 per player is very high (classic large-scale RTS usually 200–300). Strategy: Phase targets:
  - Phase A (Prototype): 300 total active mobile units.
  - Phase B (Optimization): 300 per player (up to 4 players = 1200) using batching + job system.
  - Stretch: 1000 per player only in late optimization with DOTS/Burst migration.
  Rationale: De-risk performance by staged ceilings.
- Ages: 3 (Era I, II, III placeholder names) for initial loop.
- Resources (4): Food, Wood, Stone, Metal.
- Multiplayer Priority: DELAYED (Single-player first) while keeping deterministic-friendly simulation boundaries to enable later lockstep.
- Map Sizes: Prototype 256×256 heightmap (playable ~128×128 area). Later: Medium 512×512, Large 768×768. We'll store terrain in a heightmap + overlay logical grid for placement & path sectors.
- Min Hardware Target: GTX 1050 / RX 560 (2017 mid-tier) @ 60 FPS on Medium settings; integrated GPU fallback 30 FPS with reduced shadows/LODs.

## High-Level Layering
```
Application (Unity Scenes, Bootstrap)
  Presentation Layer (MonoBehaviours, Rendering, FX, UI)
    - View objects (UnitView, BuildingView) reference pure simulation IDs
  Simulation Core (Pure C# Assemblies, deterministic tick)
    - Data: UnitType, BuildingType, Tech, Resource nodes
    - State: Entities (struct-like records), Spatial indices, Task queues
    - Systems: Movement, Orders, Economy, Combat, Vision, Tech, AI
  Persistence (Save/Load JSON snapshots)
  Data Pipeline (Import ScriptableObjects -> Serialized runtime Data)
```

## Determinism Strategy (For Future Lockstep)
- FixedTick at 20 Hz; interpolation on render.
- No floating non-deterministic ops in logic: use integers / fixed-point for core sim (position stored as int millimeters or scaled int2 grid + sub-tile offset).
- Randomness: Central RNG with seed per match + frame.
- Time: Derived from tick count only.

## Entity & Components (Simulation)
(We will NOT adopt full ECS initially; we use arrays-of-structs for hot data.)
- Unit: { id, typeId, factionId, hp, posX, posY, velX, velY, orderQueueRef, taskStateRef, visionRadius, formationId? }
- Building: { id, typeId, factionId, hp, posX, posY, constructionProgress }
- ResourceNode: { id, resourceTypeId, amountRemaining, posX, posY, gatherRadius }
- Projectile: { id, typeRef, posX, posY, targetEntityId, speed, remainingLife }

## Systems Tick Order
1. InputCommandSystem (queue orders into future frame if lockstep later)
2. ProductionSystem (training progress, spawn units)
3. ResearchSystem (tech timers)
4. TaskAssignmentSystem (idle workers -> resource, build tasks)
5. PathRequestSystem (batch path queries)
6. PathfindingSystem (A* on grid / region graph) -> writes paths
7. MovementSystem (integrate positions, resolve collisions/cohesion)
8. InteractionSystem (gather, build, repair)
9. CombatSystem (attack cooldowns, projectile spawn)
10. ProjectileSystem (advance & apply impact)
11. EconomySystem (deposit resources, update stockpiles)
12. VisionSystem (update visibility mask)
13. TerritorySystem (outpost influence flood fill occasionally)
14. AISystem (every N ticks; macro + micro separated)
15. EventSystem (produce UI events)

## Pathfinding Approach
- Early: Unity NavMesh baked per map + per-unit agent radius (single size) to move quickly.
- Mid: Custom grid (2D array) + hierarchical sectors (32×32 clusters) for A*; integrate flow field for large group moves.
- Local Avoidance: Steering (RVO-lite) or simple separation force in MovementSystem.

## Fog of War / Vision
- Maintain low-res (e.g., 256×256) per-faction visibility grid (byte states: 0=Unseen,1=Explored,2=Visible).
- GPU: Upload to texture each few ticks, sample in terrain + unit shader to dim/hide.

## Data Pipeline
- Authoring: JSON in /data for version control friendliness.
- Importer: Editor script reads JSON -> ScriptableObjects (cache) -> Runtime immutable Data objects.
- Hot Reload (later): Re-import JSON while game paused.

## Weather & Seasons (Differentiator)
- Global cycle (e.g., Spring/Summer/Autumn/Winter) each with modifiers (gatherRate, movementSpeed on certain terrain, vision in storms).
- Random weather events seeded (rain, fog bank) apply temporary modifiers.

## Territory & Logistics
- Each territory region map (flood fill from Outposts / Town Centers). Supply Efficiency = function(distanceToSupplyLine + enemy pressure). Efficiency scales gather rate & unit upkeep cost.

## Optimization Roadmap
Phase A (Baseline):
- Object pooling, disable per-frame MonoBehaviour logic; central manager updates.
- Combine meshes / GPU instancing for identical units (Unity Graphics.DrawMeshInstanced or Entities Graphics later).
- Culling: Camera frustum + distance LOD.

Phase B (Scale):
- Migrate Movement + Combat + Vision to Burst Jobs (DOTS) or custom jobified arrays.
- Introduce SoA memory layout for hot loops.

Phase C (Stretch):
- Deterministic rewrite using fixed-point & bit-packed components.
- Flow fields for mass path sharing.

## Save/Load Snapshot Schema (Draft)
```
{
  "version": 1,
  "tick": 12345,
  "factions": [...],
  "units": [{"id":1,"t":2,"f":0,"x":10432,"y":8841,"hp":90}],
  "buildings": [...],
  "resources": [...],
  "tech": {"researched": ["tech_wheel"], "inProgress": []},
  "rng": {"seed": 123456789, "state": 42}
}
```

## Open Questions (Tracked)
- Final unit population cap per faction for competitive mode.
- Territory algorithm granularity & memory budget.
- Whether to adopt DOTS early vs later (currently: later after baseline fun).

---
This file will evolve as systems become concrete.
