using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    // Simple presenter for a placed building with collider-based selection
    [RequireComponent(typeof(BoxCollider))]
    public class BuildingView : MonoBehaviour {
        public int EntityId; // simulation building id
        public int FootprintW = 1;
        public int FootprintH = 1;
        private Simulator _sim;
        private int _lastTick = -1;

        public void Init(int id, Simulator sim, int w, int h) {
            EntityId = id; _sim = sim; FootprintW = w; FootprintH = h;
            var col = GetComponent<BoxCollider>();
            col.size = new Vector3(FootprintW, 1f, FootprintH);
            col.center = Vector3.up * 0.5f; // raise slightly
        }

        void Update() {
            if (_sim == null) return;
            var ws = _sim.State; if (ws.Tick == _lastTick) return; _lastTick = ws.Tick;
            int idx = FindBuildingIndex(EntityId, ws); if (idx < 0) { Destroy(gameObject); return; }
            ref var b = ref ws.Buildings[idx];
            // Position center based on top-left origin in simulation
            float baseX = b.X / (float)SimConstants.PositionScale;
            float baseZ = b.Y / (float)SimConstants.PositionScale;
            transform.position = new Vector3(baseX + FootprintW * 0.5f - 0.5f, 0, baseZ + FootprintH * 0.5f - 0.5f);
        }

        private int FindBuildingIndex(int id, WorldState ws) { for (int i = 0; i < ws.BuildingCount; i++) if (ws.Buildings[i].Id == id) return i; return -1; }
    }
}
