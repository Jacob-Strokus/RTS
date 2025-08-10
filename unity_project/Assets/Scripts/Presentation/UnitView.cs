using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class UnitView : MonoBehaviour {
        public int EntityId; // assigned at spawn
        private Simulator _sim;
        private int _lastKnownTick;

        public void Init(int entityId, Simulator sim) {
            EntityId = entityId; _sim = sim; _lastKnownTick = -1;
        }

        void Update() {
            if (_sim == null) return;
            var state = _sim.State;
            if (state.Tick == _lastKnownTick) return;
            _lastKnownTick = state.Tick;
            int idx = FindUnitIndex(EntityId, state);
            if (idx < 0) return; // entity gone
            ref var u = ref state.Units[idx];
            transform.position = new Vector3(u.X / (float)SimConstants.PositionScale, 0, u.Y / (float)SimConstants.PositionScale);
        }

        private int FindUnitIndex(int id, WorldState ws) {
            for (int i = 0; i < ws.UnitCount; i++) if (ws.Units[i].Id == id) return i; return -1;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
#endif
    }
}
