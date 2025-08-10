using System.Collections.Generic;
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class SelectionManager : MonoBehaviour {
        public LayerMask GroundMask;
        public RectTransform SelectionBoxUi; // optional future UI overlay
    public Color UnitSelectionColor = Color.cyan;
    public Color BuildingSelectionColor = Color.yellow;
    public Color BuildingFootprintOutline = new Color(0f,1f,0f,0.35f);
        public KeyCode AdditiveModifier = KeyCode.LeftShift;
        private Simulator _sim;
        private HashSet<int> _selected = new HashSet<int>();
        private Vector2 _dragStart;
        private bool _dragging;
        private Camera _cam;
        public bool ShowPaths;

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
            if (Input.GetKeyDown(KeyCode.P)) ShowPaths = !ShowPaths;
        }

        private void TryClickSelect(Vector2 screenPos, bool clear) {
            if (clear) _selected.Clear();
            Ray ray = _cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out var hit, 500f)) {
                var view = hit.collider.GetComponentInParent<UnitView>();
                if (view) { _selected.Add(view.EntityId); return; }
                var buildView = hit.collider.GetComponentInParent<BuildingView>();
                if (buildView) { _selected.Add(buildView.EntityId); return; }
                // fallback: nothing
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
            // Buildings: screen-space test of all 4 corners of footprint AABB
            for (int b=0; b<ws.BuildingCount; b++) {
                ref var bd = ref ws.Buildings[b];
                // Determine footprint size from DataRegistry (fallback 2x2)
                int w=2,h=2; if (DataRegistry.Buildings.Length>bd.TypeId) { var bj = DataRegistry.Buildings[bd.TypeId]; if (bj.footprint!=null){ w=bj.footprint.w; h=bj.footprint.h; } }
                // Building origin stored at top-left tile origin (assumed). We'll center selection like BuildingView.
                float baseX = bd.X/(float)SimConstants.PositionScale; float baseZ = bd.Y/(float)SimConstants.PositionScale;
                Vector3[] corners = new[]{ new Vector3(baseX,0,baseZ), new Vector3(baseX+w,0,baseZ), new Vector3(baseX,0,baseZ+h), new Vector3(baseX+w,0,baseZ+h)};
                int inside=0; foreach (var c in corners){ var sp=_cam.WorldToScreenPoint(c); if (r.Contains(sp,true)) inside++; }
                if (inside>0) _selected.Add(bd.Id);
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
                    int w=2,h=2; if (DataRegistry.Buildings.Length>b.TypeId) { var bj=DataRegistry.Buildings[b.TypeId]; if (bj.footprint!=null){ w=bj.footprint.w; h=bj.footprint.h; } }
                    // Draw footprint outline slightly raised
                    float baseX = b.X/(float)SimConstants.PositionScale; float baseZ = b.Y/(float)SimConstants.PositionScale;
                    Vector3 p0 = new Vector3(baseX,0.05f,baseZ);
                    Vector3 p1 = p0 + new Vector3(w,0,0);
                    Vector3 p2 = p0 + new Vector3(w,0,h);
                    Vector3 p3 = p0 + new Vector3(0,0,h);
                    Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2); Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);
                    Gizmos.color = new Color(BuildingFootprintOutline.r, BuildingFootprintOutline.g, BuildingFootprintOutline.b, BuildingFootprintOutline.a);
                    // Optional: semi-transparent cube
                    Gizmos.DrawWireCube(p0 + new Vector3(w*0.5f,0,h*0.5f), new Vector3(w,0.02f,h));
                }
            }
            if (ShowPaths && _sim != null) {
                var buffer = new List<(int x,int y)>();
                foreach (var id in _selected) {
                    int idx = FindUnitIndex(id, _sim.State); if (idx<0) continue; if (_sim.TryGetPath(id, buffer)) {
                        Vector3 prev = new Vector3(_sim.State.Units[idx].X/(float)SimConstants.PositionScale,0.05f,_sim.State.Units[idx].Y/(float)SimConstants.PositionScale);
                        Gizmos.color = Color.green;
                        foreach (var wp in buffer) {
                            Vector3 next = new Vector3(wp.x,0.05f,wp.y); Gizmos.DrawLine(prev,next); prev = next; }
                    }
                }
            }
        }
#endif
        private int FindUnitIndex(int id, WorldState ws) { for (int i=0;i<ws.UnitCount;i++) if (ws.Units[i].Id==id) return i; return -1; }
    }
}
