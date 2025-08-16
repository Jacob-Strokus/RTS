using System.IO;
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Runtime {
    // Loads JSON authoring data at runtime from StreamingAssets/data and pushes it into DataRegistry.
    // Also registers unit types into the live Simulator after the scene loads (if SimBootstrap is present).
    public static class RuntimeDataLoader {
        private class MapConfig { public int width=128; public int height=128; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void LoadDataIntoRegistry() {
            try {
                string root = Application.streamingAssetsPath;
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) { Debug.Log("[RuntimeDataLoader] StreamingAssets not found; skipping data load."); return; }
                string dataDir = Path.Combine(root, "data");
                if (!Directory.Exists(dataDir)) { Debug.Log("[RuntimeDataLoader] data folder not found in StreamingAssets; skipping."); return; }
                // Resources
                TryReadJson(Path.Combine(dataDir, "resources.json"), out ResourceDefList rlist); if (rlist?.resources != null) DataRegistry.Resources = rlist.resources;
                // Attacks
                TryReadJson(Path.Combine(dataDir, "attacks.json"), out AttackJsonList alist); if (alist?.attacks != null) DataRegistry.Attacks = alist.attacks;
                // Units
                TryReadJson(Path.Combine(dataDir, "units.json"), out UnitTypeJsonList ulist); if (ulist?.units != null) DataRegistry.Units = ulist.units;
                // Buildings
                TryReadJson(Path.Combine(dataDir, "buildings.json"), out BuildingJsonList blist); if (blist?.buildings != null) DataRegistry.Buildings = blist.buildings;
                // Techs
                TryReadJson(Path.Combine(dataDir, "techs.json"), out TechJsonList tlist); if (tlist?.techs != null) DataRegistry.Techs = tlist.techs;
                // Map config
                string mapFile = Path.Combine(dataDir, "map.json"); if (File.Exists(mapFile)) { TryReadJson(mapFile, out MapConfig cfg); if (cfg != null) _pendingMapW = cfg.width; if (cfg != null) _pendingMapH = cfg.height; _applyMapOnAwake = true; }
                Debug.Log($"[RuntimeDataLoader] Loaded data: R={DataRegistry.Resources.Length} U={DataRegistry.Units.Length} A={DataRegistry.Attacks.Length} B={DataRegistry.Buildings.Length} T={DataRegistry.Techs.Length}");
            } catch (System.Exception ex) {
                Debug.LogWarning($"[RuntimeDataLoader] Exception loading data: {ex.Message}");
            }
        }

        private static int _pendingMapW = 128, _pendingMapH = 128; private static bool _applyMapOnAwake;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void RegisterUnitTypesIntoSimulator() {
            try {
                var bootstrap = Object.FindObjectOfType<FrontierAges.Presentation.SimBootstrap>();
                if (bootstrap == null) { Debug.Log("[RuntimeDataLoader] SimBootstrap not found; skip unit type registration."); return; }
                var sim = bootstrap.GetSimulator(); if (sim == null) { Debug.Log("[RuntimeDataLoader] Simulator not ready."); return; }
                if (_applyMapOnAwake) { sim.ConfigureMapSize(_pendingMapW, _pendingMapH); }
                // Build runtime unit types from DataRegistry
                foreach (var u in DataRegistry.Units) {
                    var utd = new UnitTypeData();
                    utd.MoveSpeedMilliPerSec = (int)u.moveSpeed;
                    utd.MaxHP = u.maxHP;
                    utd.GatherRatePerSec = 0;
                    utd.CarryCapacity = u.carryCapacity>0? u.carryCapacity:10;
                    utd.Flags = 0;
                    utd.PopCost = (byte)(u.population>0? u.population:1);
                    if (u.gatherRates != null) {
                        float[] rates = { u.gatherRates.food, u.gatherRates.wood, u.gatherRates.stone, u.gatherRates.metal };
                        float sum=0f; int nonZero=0; foreach (var r in rates){ if(r>0){ sum+=r; nonZero++; } }
                        if (sum>0f) { float avg = sum / nonZero; utd.Flags |= 1; utd.GatherRatePerSec = Mathf.Max(1, Mathf.RoundToInt(avg)); }
                        utd.GatherFoodPerSec = u.gatherRates.food>0? Mathf.Max(1, Mathf.RoundToInt(u.gatherRates.food)) : 0;
                        utd.GatherWoodPerSec = u.gatherRates.wood>0? Mathf.Max(1, Mathf.RoundToInt(u.gatherRates.wood)) : 0;
                        utd.GatherStonePerSec = u.gatherRates.stone>0? Mathf.Max(1, Mathf.RoundToInt(u.gatherRates.stone)) : 0;
                        utd.GatherMetalPerSec = u.gatherRates.metal>0? Mathf.Max(1, Mathf.RoundToInt(u.gatherRates.metal)) : 0;
                    }
                    if (u.cost != null) { utd.FoodCost=u.cost.food; utd.WoodCost=u.cost.wood; utd.StoneCost=u.cost.stone; utd.MetalCost=u.cost.metal; }
                    if (u.trainTimeMs > 0) utd.TrainTimeMs = u.trainTimeMs;
                    if (u.armor != null) { utd.ArmorMelee=u.armor.melee; utd.ArmorPierce=u.armor.pierce; utd.ArmorSiege=u.armor.siege; utd.ArmorMagic=u.armor.magic; }
                    if (!string.IsNullOrEmpty(u.attackProfile)) {
                        var ap = FindAttack(u.attackProfile);
                        if (ap != null) {
                            utd.AttackRange = (int)ap.range;
                            utd.AttackDamageBase = ap.damage;
                            utd.AttackCooldownMs = ap.cooldownMs;
                            utd.AttackWindupMs = ap.windupMs;
                            utd.AttackImpactDelayMs = ap.impactDelayMs;
                            if (ap.projectile != null) { utd.HasProjectile=1; utd.ProjectileSpeed = ap.projectile.speed; utd.ProjectileLifetimeMs = ap.projectile.lifetimeMs>0? ap.projectile.lifetimeMs:5000; utd.ProjectileHoming = (byte)(ap.projectile.homing!=0?1:0); }
                            if (ap.damageTypes != null) { utd.DTMelee = ap.damageTypes.melee==0?1f:ap.damageTypes.melee; utd.DTPierce=ap.damageTypes.pierce; utd.DTSiege=ap.damageTypes.siege; utd.DTMagic=ap.damageTypes.magic; } else { utd.DTMelee=1f; }
                        }
                    }
                    sim.RegisterUnitType(utd);
                }
                Debug.Log($"[RuntimeDataLoader] Registered {DataRegistry.Units.Length} unit types into Simulator");
            } catch (System.Exception ex) {
                Debug.LogWarning($"[RuntimeDataLoader] Exception registering unit types: {ex.Message}");
            }
        }

        private static AttackJson FindAttack(string id){ foreach(var a in DataRegistry.Attacks) if(a.id==id) return a; return null; }

        private static bool TryReadJson<T>(string file, out T obj) {
            obj = default; if (!File.Exists(file)) return false; try { string json = File.ReadAllText(file); obj = JsonUtility.FromJson<T>(json); return obj != null; } catch { return false; }
        }
    }
}
