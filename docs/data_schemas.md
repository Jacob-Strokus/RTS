# Data Schemas (Draft)

We keep authoring in JSON for VCS readability & potential mod support. At build/import time we transform into immutable runtime data objects.

## Conventions
- id: lowercase snake case unique string key.
- Costs absent ⇒ 0.
- Time fields milliseconds unless specified.
- Distances in fixed-point milli-units (1.0 world unit = 1000 milli units) for determinism.

## Resource Types
```
resources.json
{
  "resources": [
    { "id": "food",  "displayName": "Food",  "description": "Sustains population" },
    { "id": "wood",  "displayName": "Wood",  "description": "Basic construction material" },
    { "id": "stone", "displayName": "Stone", "description": "Fortifications and heavy buildings" },
    { "id": "metal", "displayName": "Metal", "description": "Advanced arms and tech" }
  ]
}
```

## Unit Types
```
units.json
{
  "units": [
    {
      "id": "worker",
      "displayName": "Worker",
      "age": 1,
      "category": "civilian",
      "maxHP": 50,
      "moveSpeed": 2500,              // 2.5 world units / sec
      "acceleration": 8000,
      "sight": 18000,                 // 18 world units
      "gatherRates": { "wood": 0.9, "food": 0.9, "stone": 0.6, "metal": 0.5 },
      "carryCapacity": 10,
      "buildOptions": ["town_center", "house", "barracks"],
      "attackProfile": null,
      "cost": { "food": 50 },
      "trainTimeMs": 15000,
      "population": 1
    },
    {
      "id": "spearman",
      "displayName": "Spearman",
      "age": 1,
      "category": "military_infantry",
      "maxHP": 90,
      "moveSpeed": 2600,
      "acceleration": 9000,
      "sight": 20000,
      "attackProfile": "melee_spear_basic",
      "armor": { "melee": 1, "pierce": 0 },
      "cost": { "food": 60, "wood": 20 },
      "trainTimeMs": 18000,
      "population": 1
    }
  ]
}
```

## Attack Profiles
```
attacks.json
{
  "attacks": [
    {
      "id": "melee_spear_basic",
      "range": 1500,
      "cooldownMs": 1800,
      "damage": 12,
      "damageTypes": { "melee": 1.0 },
      "projectile": null,
      "windupMs": 400,
      "impactDelayMs": 0
    }
  ]
}
```

## Building Types
```
buildings.json
{
  "buildings": [
    {
      "id": "town_center",
      "displayName": "Town Center",
      "age": 1,
      "maxHP": 2400,
      "footprint": { "w": 5, "h": 5 },  // grid tiles
      "lineOfSight": 30000,
      "providesPopulation": 10,
      "train": ["worker"],
      "buildTimeMs": 60000,
      "cost": { "wood": 200, "stone": 150 },
      "influenceRadius": 40000
    },
    {
      "id": "barracks",
      "displayName": "Barracks",
      "age": 1,
      "maxHP": 1400,
      "footprint": { "w": 4, "h": 4 },
      "lineOfSight": 22000,
      "train": ["spearman"],
      "buildTimeMs": 45000,
      "cost": { "wood": 175, "stone": 50 }
    }
  ]
}
```

## Tech Definitions
```
techs.json
{
  "techs": [
    {
      "id": "age_up_2",
      "displayName": "Advance to Era II",
      "prereq": [],
      "age": 1,
      "researchTimeMs": 90000,
      "cost": { "food": 400, "wood": 200, "metal": 100 },
      "effects": [ { "type": "unlockAge", "targetAge": 2 } ]
    }
  ]
}
```

## Save Game Delta Extensions (future)
- We may adopt binary chunked format for large army states (varint + delta compression) while keeping JSON export for debugging.

## Validation Rules (Importer)
- Verify referenced IDs exist (attackProfile, buildOptions, train lists).
- Enforce monotonic age progression (unit.age <= building.age producing it + 1).
- Cost values >= 0; trainTimeMs > 0.

## Open Schema Issues
- Need armor/damage type matrix externalization.
- Weather effect stacking representation.
