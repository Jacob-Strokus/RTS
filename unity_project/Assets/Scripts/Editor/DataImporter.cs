#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.EditorTools {
    public static class DataImporter {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        private static string DataPath => Path.Combine(RepoRoot, "data");

        [MenuItem("FrontierAges/Data/Import JSON (Stub)")]
        public static void ImportAll() {
            if (!Directory.Exists(DataPath)) { Debug.LogWarning($"Data folder not found: {DataPath}"); return; }
            LoadResources();
            LoadAttacks();
            LoadUnits();
            LoadBuildings();
            LoadTechs();
            LoadMapConfig();
            BuildRuntimeUnitTypeData();
            ValidateCrossRefs();
            Debug.Log($"Imported Data: Resources={DataRegistry.Resources.Length} Units={DataRegistry.Units.Length} Attacks={DataRegistry.Attacks.Length} Buildings={DataRegistry.Buildings.Length}");
        }

        private static void LoadResources() {
            string file = Path.Combine(DataPath, "resources.json");
            if (!File.Exists(file)) { Debug.LogWarning("resources.json missing"); return; }
            var json = File.ReadAllText(file);
            var wrapper = JsonUtility.FromJson<ResourceDefList>(json);
            if (wrapper != null && wrapper.resources != null) DataRegistry.Resources = wrapper.resources;
        }

        private static void LoadUnits() {
            string file = Path.Combine(DataPath, "units.json");
            if (!File.Exists(file)) { Debug.LogWarning("units.json missing"); return; }
            var json = File.ReadAllText(file);
            var wrapper = JsonUtility.FromJson<UnitTypeJsonList>(json);
            if (wrapper != null && wrapper.units != null) DataRegistry.Units = wrapper.units;
        }

        private static void LoadAttacks() {
            string file = Path.Combine(DataPath, "attacks.json");
            if (!File.Exists(file)) { Debug.LogWarning("attacks.json missing"); return; }
            var json = File.ReadAllText(file);
            var wrapper = JsonUtility.FromJson<AttackJsonList>(json);
            if (wrapper != null && wrapper.attacks != null) DataRegistry.Attacks = wrapper.attacks;
        }

        private static void LoadBuildings() {
            string file = Path.Combine(DataPath, "buildings.json");
            if (!File.Exists(file)) { Debug.LogWarning("buildings.json missing"); return; }
            var json = File.ReadAllText(file);
            var wrapper = JsonUtility.FromJson<BuildingJsonList>(json);
            if (wrapper != null && wrapper.buildings != null) DataRegistry.Buildings = wrapper.buildings;
        }

        private static void LoadTechs(){
            string file = Path.Combine(DataPath, "techs.json");
            if (!File.Exists(file)) { Debug.LogWarning("techs.json missing"); return; }
            var json = File.ReadAllText(file);
            var wrapper = JsonUtility.FromJson<TechJsonList>(json);
            if (wrapper != null && wrapper.techs != null) DataRegistry.Techs = wrapper.techs;
        }

    [System.Serializable] private class MapConfig { public int width=128; public int height=128; }
    private static void LoadMapConfig(){ string file = Path.Combine(DataPath, "map.json"); if(!File.Exists(file)) return; var json = File.ReadAllText(file); var cfg = JsonUtility.FromJson<MapConfig>(json); if(cfg!=null){ var sim = Object.FindObjectOfType<FrontierAges.Presentation.SimBootstrap>()?.GetSimulator(); if(sim!=null) sim.ConfigureMapSize(cfg.width, cfg.height); Debug.Log($"Map config loaded {cfg.width}x{cfg.height}"); } }

        private static void ValidateCrossRefs() {
            // Simple O(N*M) first; optimize later with hash sets
            System.Func<string, bool> HasAttack = id => { foreach (var a in DataRegistry.Attacks) if (a.id == id) return true; return false; };
            System.Func<string, bool> HasUnit = id => { foreach (var u in DataRegistry.Units) if (u.id == id) return true; return false; };
            int warnings = 0;
            foreach (var u in DataRegistry.Units) {
                if (!string.IsNullOrEmpty(u.attackProfile) && !HasAttack(u.attackProfile)) { Debug.LogWarning($"Unit {u.id} references missing attack {u.attackProfile}"); warnings++; }
            }
            foreach (var b in DataRegistry.Buildings) {
                if (b.train == null) continue;
                foreach (var t in b.train) if (!HasUnit(t)) { Debug.LogWarning($"Building {b.id} trains missing unit {t}"); warnings++; }
            }
            if (warnings == 0) Debug.Log("Validation passed: all references resolved.");
        }

        private static void BuildRuntimeUnitTypeData(){ var sim = Object.FindObjectOfType<FrontierAges.Presentation.SimBootstrap>()?.GetSimulator(); if(sim==null){ Debug.LogWarning("Simulator not found; cannot build runtime unit types yet."); return; }
            foreach(var u in DataRegistry.Units){ FrontierAges.Sim.UnitTypeData utd = new FrontierAges.Sim.UnitTypeData(); utd.MoveSpeedMilliPerSec = (int)u.moveSpeed; utd.MaxHP = u.maxHP; utd.GatherRatePerSec = 0; utd.CarryCapacity = u.carryCapacity>0? u.carryCapacity:10; utd.Flags=0; utd.PopCost = (byte)(u.population>0? u.population:1);
                if(u.gatherRates!=null){ // mark as worker if any gather rate >0
                    float sum = u.gatherRates.wood + u.gatherRates.food + u.gatherRates.stone + u.gatherRates.metal;
                    if(sum>0.0001f){ utd.Flags |= 1; utd.GatherRatePerSec = (int)( (u.gatherRates.food+u.gatherRates.wood+u.gatherRates.stone+u.gatherRates.metal)/4f * 1000f); }
                }
                if(u.cost!=null){ utd.FoodCost=u.cost.food; utd.WoodCost=u.cost.wood; utd.StoneCost=u.cost.stone; utd.MetalCost=u.cost.metal; }
                if(u.trainTimeMs>0) utd.TrainTimeMs = u.trainTimeMs;
                // Armor
                if(u.armor!=null){ utd.ArmorMelee = u.armor.melee; utd.ArmorPierce = u.armor.pierce; utd.ArmorSiege = u.armor.siege; utd.ArmorMagic = u.armor.magic; }
                // Costs (if present) - reflect into fields via reflection dynamic JSON (not strongly typed). We'll attempt to parse via JsonUtility not supporting dictionaries; so skip advanced.
                // Attack profile with damage type multipliers
                if(!string.IsNullOrEmpty(u.attackProfile)){
                    var ap = FindAttack(u.attackProfile);
                    if(ap!=null){
                        utd.AttackRange = (int)ap.range;
                        utd.AttackDamageBase = ap.damage;
                        utd.AttackCooldownMs = ap.cooldownMs;
                        utd.AttackWindupMs = ap.windupMs;
                        utd.AttackImpactDelayMs = ap.impactDelayMs;
                        if(ap.projectile!=null){
                            utd.HasProjectile=1; utd.ProjectileSpeed = ap.projectile.speed; utd.ProjectileLifetimeMs = ap.projectile.lifetimeMs>0? ap.projectile.lifetimeMs: 5000; utd.ProjectileHoming = (byte)(ap.projectile.homing!=0?1:0);
                        }
                        if(ap.damageTypes!=null){
                            utd.DTMelee = ap.damageTypes.melee==0?1f:ap.damageTypes.melee;
                            utd.DTPierce = ap.damageTypes.pierce;
                            utd.DTSiege = ap.damageTypes.siege;
                            utd.DTMagic = ap.damageTypes.magic;
                        } else { utd.DTMelee = 1f; }
                    }
                }
                sim.RegisterUnitType(utd); }
        }
        private static AttackJson FindAttack(string id){ foreach(var a in DataRegistry.Attacks) if(a.id==id) return a; return null; }
    }
}
#endif
