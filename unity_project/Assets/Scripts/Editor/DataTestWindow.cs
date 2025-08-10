#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FrontierAges.Sim;
using FrontierAges.Presentation;

namespace FrontierAges.EditorTools {
    public class DataTestWindow : EditorWindow {
        [MenuItem("FrontierAges/Windows/Data Test")] public static void Open() => GetWindow<DataTestWindow>("Data Test");
        private Simulator _sim;
        private SimBootstrap _bootstrap;
        private Vector2 _scroll;

        void OnGUI() {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (GUILayout.Button("Import JSON")) { DataImporter.ImportAll(); }
            if (DataRegistry.HasLoaded) {
                GUILayout.Label($"Resources Loaded: {DataRegistry.Resources.Length}");
                GUILayout.Label($"Unit Types Loaded: {DataRegistry.Units.Length}");
            } else GUILayout.Label("No data loaded.");

            EditorGUILayout.Space();
            if (Application.isPlaying) {
                if (_bootstrap == null) _bootstrap = FindObjectOfType<SimBootstrap>();
                if (_bootstrap != null) _sim = _bootstrap.GetSimulator();
                if (_sim != null) {
                    GUILayout.Label($"Sim Tick: {_sim.State.Tick} Units: {_sim.State.UnitCount}");
                    if (GUILayout.Button("Register & Spawn Imported Units")) {
                        RegisterImportedUnits();
                    }
                    if (GUILayout.Button("Save Snapshot (console log)")) {
                        var snap = SnapshotUtil.Capture(_sim.State);
                        string json = JsonUtility.ToJson(snap, true);
                        Debug.Log(json);
                    }
                    if (GUILayout.Button("Load Snapshot (from clipboard)")) {
                        try {
                            var json = EditorGUIUtility.systemCopyBuffer;
                            var snap = JsonUtility.FromJson<Snapshot>(json);
                            if (snap != null) {
                                SnapshotUtil.Apply(_sim.State, snap);
                                Debug.Log("Snapshot loaded from clipboard.");
                            }
                        } catch (System.Exception ex) { Debug.LogError(ex); }
                    }
                } else GUILayout.Label("Simulator not found (ensure scene has SimBootstrap).");
            } else {
                GUILayout.Label("Enter Play Mode to interact with simulation.");
            }
            EditorGUILayout.EndScrollView();
        }

        private void RegisterImportedUnits() {
            if (DataRegistry.Units.Length == 0 || _sim == null) return;
            foreach (var ut in DataRegistry.Units) {
                int speed = Mathf.RoundToInt(ut.moveSpeed * SimConstants.PositionScale);
                var typeId = _sim.RegisterUnitType(new UnitTypeData { MoveSpeedMilliPerSec = speed, MaxHP = ut.maxHP });
                _sim.SpawnUnit(typeId, 0, 0, 0, ut.maxHP);
            }
        }
    }
}
#endif
