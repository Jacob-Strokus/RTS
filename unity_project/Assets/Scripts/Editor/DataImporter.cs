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
    }
}
#endif
