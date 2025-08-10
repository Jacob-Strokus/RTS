using System.Collections.Generic;
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class SelectionManager : MonoBehaviour {
        public LayerMask GroundMask;
        public RectTransform SelectionBoxUi; // optional future UI overlay
    public Color UnitSelectionColor = Color.cyan;
    public Color BuildingSelectionColor = Color.yellow;
        public KeyCode AdditiveModifier = KeyCode.LeftShift;
        private Simulator _sim;
        private HashSet<int> _selected = new HashSet<int>();
        private Vector2 _dragStart;
        private bool _dragging;
        private Camera _cam;

        void Start() { _sim = FindObjectOfType<SimBootstrap>()?.GetComponent<SimBootstrap>()?.GetSimulator(); _cam = Camera.main; }

        void Update() {
            if (_cam == null || _sim == null) return;
            if (Input.GetMouseButtonDown(0)) { _dragStart = Input.mousePosition; _dragging = true; }
            if (Input.GetMouseButtonUp(0)) {
                if (_dragging) {
                    var dragEnd = (Vector2)Input.mousePosition;
                    if ((dragEnd - _dragStart).magnitude < 4f) {
                        // Click select
                        TryClickSelect(dragEnd, !Input.GetKey(AdditiveModifier));
                    } else {
                        // Box select
                        PerformBoxSelect(_dragStart, dragEnd, !Input.GetKey(AdditiveModifier));
                    }
                }
                _dragging = false;
            }
            if (Input.GetKeyDown(KeyCode.Escape)) _selected.Clear();
        }

        private void TryClickSelect(Vector2 screenPos, bool clear) {
            if (clear) _selected.Clear();
            Ray ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 500f)) {
                var view = hit.collider.GetComponentInParent<UnitView>();
                if (view) _selected.Add(view.EntityId);
                else {
                    // building selection attempt: simple proximity pick (later: BuildingView prefab)
                    var ws = _sim.State;
                    Vector3 hp = hit.point;
                    float best=1.5f; int bestId=-1;
                    for (int i=0;i<ws.BuildingCount;i++) { ref var b = ref ws.Buildings[i];
                        float dx = (b.X/(float)SimConstants.PositionScale)-hp.x; float dz=(b.Y/(float)SimConstants.PositionScale)-hp.z; float d=Mathf.Sqrt(dx*dx+dz*dz);
                        if (d<best) { best=d; bestId=b.Id; }
                    }
                    if (bestId!=-1) _selected.Add(bestId);
                }
            }
        }

        private void PerformBoxSelect(Vector2 start, Vector2 end, bool clear) {
            if (clear) _selected.Clear();
            Rect r = Rect.MinMaxRect(Mathf.Min(start.x,end.x), Mathf.Min(start.y,end.y), Mathf.Max(start.x,end.x), Mathf.Max(start.y,end.y));
            var ws = _sim.State;
            for (int i = 0; i < ws.UnitCount; i++) {
                ref var u = ref ws.Units[i];
                Vector3 world = new Vector3(u.X / (float)SimConstants.PositionScale, 0, u.Y / (float)SimConstants.PositionScale);
                Vector3 sp = _cam.WorldToScreenPoint(world);
                if (r.Contains(sp, true)) _selected.Add(u.Id);
            }
        }

        public IReadOnlyCollection<int> Selected => _selected;

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (_sim == null) return;
            foreach (var id in _selected) {
                int uIdx = FindUnitIndex(id, _sim.State);
                if (uIdx >=0) {
                    Gizmos.color = UnitSelectionColor;
                    ref var u = ref _sim.State.Units[uIdx];
                    Gizmos.DrawWireCube(new Vector3(u.X/(float)SimConstants.PositionScale,0,u.Y/(float)SimConstants.PositionScale)+Vector3.up*0.1f, new Vector3(0.8f,0.1f,0.8f));
                    continue;
                }
                // building?
                int bIdx = -1; for (int i=0;i<_sim.State.BuildingCount;i++) if (_sim.State.Buildings[i].Id==id) { bIdx=i; break; }
                if (bIdx>=0) {
                    Gizmos.color = BuildingSelectionColor;
                    ref var b = ref _sim.State.Buildings[bIdx];
                    Gizmos.DrawWireCube(new Vector3(b.X/(float)SimConstants.PositionScale,0,b.Y/(float)SimConstants.PositionScale)+Vector3.up*0.15f, new Vector3(1.2f,0.1f,1.2f));
                }
            }
        }
#endif
        private int FindUnitIndex(int id, WorldState ws) { for (int i=0;i<ws.UnitCount;i++) if (ws.Units[i].Id==id) return i; return -1; }
    }
}
