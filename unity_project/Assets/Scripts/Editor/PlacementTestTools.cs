#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FrontierAges.Sim;
using FrontierAges.Presentation;

namespace FrontierAges.EditorTools {
    public static class PlacementTestTools {
        private static Simulator Sim => Object.FindObjectOfType<SimBootstrap>()?.GetSimulator();

        [MenuItem("FrontierAges/Debug/Spawn Test Resource Node")] public static void SpawnNode() {
            if (Sim == null) { Debug.LogWarning("Simulator not running"); return; }
            int x = Random.Range(0, 40) * SimConstants.PositionScale;
            int y = Random.Range(0, 40) * SimConstants.PositionScale;
            Sim.SpawnResourceNode(1, x, y, 60);
            Debug.Log($"Spawned resource node at {x},{y}");
        }

        [MenuItem("FrontierAges/Debug/Clear All Resource Nodes")] public static void ClearNodes() {
            if (Sim == null) return;
            Sim.State.ResourceNodeCount = 0; // quick clear for prototyping
            Debug.Log("Cleared resource nodes");
        }
    }
}
#endif
