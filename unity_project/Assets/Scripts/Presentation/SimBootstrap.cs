// Unity presentation bootstrap hooking into pure simulation
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class SimBootstrap : MonoBehaviour {
        public int PreSpawnCount = 10;
        public GameObject UnitPrefab; // simple capsule prefab
    public GameObject BuildingGhostPrefab; // simple cube for placement preview
    public GameObject BuildingViewPrefab; // visual prefab for placed building (optional)
        private Simulator _sim;
    private float _accum;
        private CommandQueue _queue;
        private System.Collections.Generic.List<int> _unitIds = new System.Collections.Generic.List<int>();
    private SelectionManager _selection;
    private GameObject _ghost;
    private bool _placingBuilding;
    private Vector3 _ghostValidColor = new Color(0,1,0,0.4f);
    private Vector3 _ghostInvalidColor = new Color(1,0,0,0.4f);
    private bool _autoAssignWorkers = true;
    private System.Diagnostics.Stopwatch _tickSw = new System.Diagnostics.Stopwatch();

        private int _placeBuildingIndex = 0; // index into DataRegistry.Buildings
        private int _lastPlacedBuildingIndexPersisted;
    private string _lastSnapshotJson; // in-memory snapshot store (prototype)
         void Awake() {
            _queue = new CommandQueue();
            _sim = new Simulator(_queue);
            _autoAssignWorkers = PlayerPrefs.GetInt("fa_autoAssign",1)==1;
            _sim.AutoAssignWorkersEnabled = _autoAssignWorkers;
            // Register provisional unit type 0 (worker placeholder)
            var typeId = _sim.RegisterUnitType(new FrontierAges.Sim.UnitTypeData {
                MoveSpeedMilliPerSec = 2500,
                MaxHP = 50,
                AttackDamage = 5,
                AttackCooldownMs = 1000,
                AttackRange = 3000,
                GatherRatePerSec = 1,
                CarryCapacity = 5,
                Flags = 1 // worker
            });
            // Spawn placeholder units in a line
            for (int i = 0; i < PreSpawnCount; i++) {
                var id = _sim.SpawnUnit(typeId:typeId, factionId:0, x:i*1500, y:0, hp:0);
                _unitIds.Add(id);
                if (UnitPrefab) {
                    var obj = Instantiate(UnitPrefab, new Vector3(i * 1.5f, 0, 0), Quaternion.identity);
                    var view = obj.GetComponent<UnitView>();
                    if (!view) view = obj.AddComponent<UnitView>();
                    view.Init(id, _sim);
                }
            }
            // Spawn a few resource nodes (wood=1)
            for (int r=0; r<4; r++) {
                _sim.SpawnResourceNode(1, r*4000 + 3000, 5000, 50); // spaced line
            }
            _placeBuildingIndex = PlayerPrefs.GetInt("fa_lastBuildingIndex", 0);
            _lastPlacedBuildingIndexPersisted = _placeBuildingIndex;
        }

    public Simulator GetSimulator() => _sim;

        void Update() {
            if (_selection == null) _selection = FindObjectOfType<SelectionManager>();
            _accum += Time.deltaTime * 1000f; // ms
            while (_accum >= SimConstants.MsPerTick) {
                _accum -= SimConstants.MsPerTick;
                _tickSw.Restart();
                _sim.Tick();
                _tickSw.Stop();
                // Store last duration (micro approx) and running avg (EWMA)
                long micros = (long)(_tickSw.Elapsed.TotalMilliseconds * 1000.0);
                _sim.State.LastTickDurationMsTimes1000 = micros;
                _sim.State.AvgTickDurationMicro = (_sim.State.AvgTickDurationMicro==0) ? micros : ( (_sim.State.AvgTickDurationMicro*9 + micros)/10 );
            }
            // Right click: group move with simple square formation
            if (Input.GetMouseButtonDown(1)) {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f)) {
                    IssueGroupMove(hit.point);
                }
            }

            // B key: toggle building placement mode
            if (Input.GetKeyDown(KeyCode.B)) {
                _placingBuilding = !_placingBuilding;
                if (_placingBuilding) {
                    if (BuildingGhostPrefab) _ghost = Instantiate(BuildingGhostPrefab);
                } else { if (_ghost) Destroy(_ghost); }
            }

            if (_placingBuilding) {
                if (Input.GetKeyDown(KeyCode.Period)) { _placeBuildingIndex = (_placeBuildingIndex + 1) % Mathf.Max(1, FrontierAges.Sim.DataRegistry.Buildings.Length); }
                if (Input.GetKeyDown(KeyCode.Comma)) { _placeBuildingIndex = (_placeBuildingIndex - 1 + Mathf.Max(1, FrontierAges.Sim.DataRegistry.Buildings.Length)) % Mathf.Max(1, FrontierAges.Sim.DataRegistry.Buildings.Length); }
                if (_placeBuildingIndex != _lastPlacedBuildingIndexPersisted) { PlayerPrefs.SetInt("fa_lastBuildingIndex", _placeBuildingIndex); PlayerPrefs.Save(); _lastPlacedBuildingIndexPersisted = _placeBuildingIndex; }
            }

            if (_placingBuilding) UpdateBuildingPlacement();

            // T key: enqueue training (unit type 0) at first building if exists
            if (Input.GetKeyDown(KeyCode.T)) {
                if (_sim.State.BuildingCount > 0) {
                    var bId = _sim.State.Buildings[0].Id;
                    _sim.EnqueueTrain(bId, 0, 5000); // 5s train time placeholder
                }
            }

            // G key: have selected workers gather nearest resource
            if (Input.GetKeyDown(KeyCode.G)) {
                if (_selection != null && _selection.Selected.Count > 0) {
                    foreach (var uid in _selection.Selected) {
                        // find nearest resource node
                        int best=-1; long bestD2=long.MaxValue;
                        int uIdx = FindUnitIndex(uid);
                        if (uIdx < 0) continue;
                        ref var u = ref _sim.State.Units[uIdx];
                        for (int rn=0; rn<_sim.State.ResourceNodeCount; rn++) {
                            ref var node = ref _sim.State.ResourceNodes[rn]; if (node.AmountRemaining<=0) continue;
                            long dx = node.X - u.X; long dy = node.Y - u.Y; long d2 = dx*dx+dy*dy; if (d2 < bestD2) { bestD2=d2; best = rn; }
                        }
                        if (best>=0) {
                            _sim.IssueGatherCommand(uid, _sim.State.ResourceNodes[best].Id);
                        }
                    }
                }
            }

            // H key: toggle auto-assign workers
            if (Input.GetKeyDown(KeyCode.H)) {
                _autoAssignWorkers = !_autoAssignWorkers;
                _sim.AutoAssignWorkersEnabled = _autoAssignWorkers;
                PlayerPrefs.SetInt("fa_autoAssign", _autoAssignWorkers?1:0); PlayerPrefs.Save();
            }

            // A key: if one unit selected and another unit under cursor, issue attack
            if (Input.GetKeyDown(KeyCode.A)) {
                TryIssueAttackFromSelection();
            }

            // F5 save snapshot, F9 load (prototype convenience)
            if (Input.GetKeyDown(KeyCode.F5)) {
                var snap = FrontierAges.Sim.SnapshotUtil.Capture(_sim); _lastSnapshotJson = JsonUtility.ToJson(snap);
                Debug.Log($"Snapshot saved (len={_lastSnapshotJson?.Length})");
            }
            if (Input.GetKeyDown(KeyCode.F9) && !string.IsNullOrEmpty(_lastSnapshotJson)) {
                var snap = JsonUtility.FromJson<FrontierAges.Sim.Snapshot>(_lastSnapshotJson); FrontierAges.Sim.SnapshotUtil.Apply(_sim, snap);
                Debug.Log("Snapshot loaded");
                // Rebuild presentation objects (simple: destroy all, respawn new views)
                RebuildViews();
            }
        }

        private void TryIssueAttackFromSelection() {
            if (_selection == null) return; if (_selection.Selected.Count == 0) return;
            // pick first selected as attacker
            int attackerId = -1; foreach (var id in _selection.Selected) { attackerId = id; break; }
            if (attackerId < 0) return;
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f)) {
                var maybeTarget = hit.collider.GetComponent<UnitView>();
                if (maybeTarget && maybeTarget.EntityId != attackerId) {
                    _sim.IssueAttackCommand(attackerId, maybeTarget.EntityId);
                }
            }
        }

        private void RebuildViews() {
            // Destroy existing spawned unit & building view objects (rudimentary: tag by components)
            foreach (var uv in FindObjectsOfType<UnitView>()) Destroy(uv.gameObject);
            foreach (var bv in FindObjectsOfType<BuildingView>()) Destroy(bv.gameObject);
            // Respawn units
            var ws = _sim.State;
            for (int i=0;i<ws.UnitCount;i++) {
                ref var u = ref ws.Units[i];
                if (UnitPrefab) {
                    var obj = Instantiate(UnitPrefab, new Vector3(u.X/(float)SimConstants.PositionScale,0,u.Y/(float)SimConstants.PositionScale), Quaternion.identity);
                    var view = obj.GetComponent<UnitView>(); if (!view) view = obj.AddComponent<UnitView>(); view.Init(u.Id,_sim);
                }
            }
            for (int b=0;b<ws.BuildingCount;b++) {
                ref var bd = ref ws.Buildings[b];
                if (BuildingViewPrefab) {
                    var obj = Instantiate(BuildingViewPrefab);
                    var bv = obj.GetComponent<BuildingView>(); if (!bv) bv = obj.AddComponent<BuildingView>();
                    int w = bd.FootprintW>0? bd.FootprintW:2; int h = bd.FootprintH>0? bd.FootprintH:2;
                    bv.Init(bd.Id, _sim, w, h);
                }
            }
            // After spawning units attach health bars
            if (WorldSpaceCanvas && HealthBarPrefab){ foreach(var uv in FindObjectsOfType<UnitView>()){ uv.AttachHealthBar(WorldSpaceCanvas, HealthBarPrefab); } }
        }

        private int FindUnitIndex(int id) {
            var ws = _sim.State; for (int i=0;i<ws.UnitCount;i++) if (ws.Units[i].Id==id) return i; return -1;
        }

        private void IssueGroupMove(Vector3 target) {
            var ids = _unitIds;
            if (_selection != null && _selection.Selected.Count > 0) {
                // Build temp list from selection
                ids = new System.Collections.Generic.List<int>(_selection.Selected);
            }
            if (ids.Count == 0) return;
            int count = ids.Count;
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int spacing = 1500; // milli-units spacing
            int baseX = (int)(target.x * SimConstants.PositionScale);
            int baseY = (int)(target.z * SimConstants.PositionScale);
            for (int i = 0; i < count; i++) {
                int row = i / cols; int col = i % cols;
                int ox = (col - cols/2) * spacing;
                int oy = (row - cols/2) * spacing;
                _sim.IssueMoveCommand(ids[i], baseX + ox, baseY + oy);
            }
        }

        private void UpdateBuildingPlacement() {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 500f)) {
                Vector3 pos = hit.point;
                // snap to whole units
                pos.x = Mathf.Round(pos.x);
                pos.z = Mathf.Round(pos.z);
                if (_ghost) _ghost.transform.position = pos;
                int worldX = (int)(pos.x * SimConstants.PositionScale);
                int worldY = (int)(pos.z * SimConstants.PositionScale);
                // TODO: look up building footprint from imported data (first building in registry if available)
                int w=2,h=2; // fallback
                if (FrontierAges.Sim.DataRegistry.Buildings.Length>0) {
                    var bjson = FrontierAges.Sim.DataRegistry.Buildings[Mathf.Clamp(_placeBuildingIndex,0,FrontierAges.Sim.DataRegistry.Buildings.Length-1)];
                    if (bjson.footprint!=null) { w=bjson.footprint.w; h=bjson.footprint.h; }
                }
                bool valid = _sim.CanPlaceBuildingRect(worldX, worldY, w, h);
                if (_ghost) {
                    foreach (var r in _ghost.GetComponentsInChildren<Renderer>()) {
                        r.material.color = valid ? new Color(0,1,0,0.4f) : new Color(1,0,0,0.4f);
                    }
                }
                if (valid && Input.GetMouseButtonDown(0)) {
                    _sim.PlaceBuildingWithFootprint(0, 0, worldX, worldY, w, h, 0);
                    _placingBuilding = false; if (_ghost) Destroy(_ghost);
                    // Instantiate view at placed location
                    if (BuildingViewPrefab) {
                        var viewGo = Instantiate(BuildingViewPrefab);
                        var bv = viewGo.GetComponent<BuildingView>(); if (!bv) bv = viewGo.AddComponent<BuildingView>();
                        // We'll need building id: last building added is at index BuildingCount-1
                        int bid = _sim.State.Buildings[_sim.State.BuildingCount-1].Id;
                        bv.Init(bid, _sim, w, h);
                    }
                }
                if (Input.GetMouseButtonDown(1)) { _placingBuilding = false; if (_ghost) Destroy(_ghost); }
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos() {
            if (_placingBuilding) {
                Gizmos.color = new Color(1,1,1,0.08f);
                int size = 64; // draw partial grid near origin for prototype
                for (int x=0; x<=size; x++) {
                    Gizmos.DrawLine(new Vector3(x,0,0), new Vector3(x,0,size));
                }
                for (int y=0; y<=size; y++) {
                    Gizmos.DrawLine(new Vector3(0,0,y), new Vector3(size,0,y));
                }
            }
            // Outline selected building footprint using same ghost color (green) if any building selected
            if (_selection == null) _selection = FindObjectOfType<SelectionManager>();
            if (_selection != null && _sim!=null) {
                foreach (var id in _selection.Selected) {
                    int bIdx=-1; for (int i=0;i<_sim.State.BuildingCount;i++) if (_sim.State.Buildings[i].Id==id) { bIdx=i; break; }
                    if (bIdx<0) continue;
                    ref var b = ref _sim.State.Buildings[bIdx];
                    int w=2,h=2; if (FrontierAges.Sim.DataRegistry.Buildings.Length>b.TypeId) { var bj=FrontierAges.Sim.DataRegistry.Buildings[b.TypeId]; if (bj.footprint!=null){ w=bj.footprint.w; h=bj.footprint.h; } }
                    float baseX = b.X/(float)SimConstants.PositionScale; float baseZ=b.Y/(float)SimConstants.PositionScale;
                    Vector3 p0=new Vector3(baseX,0.05f,baseZ); Vector3 p1=p0+new Vector3(w,0,0); Vector3 p2=p0+new Vector3(w,0,h); Vector3 p3=p0+new Vector3(0,0,h);
                    Gizmos.color = new Color(0,1,0,0.35f);
                    Gizmos.DrawLine(p0,p1); Gizmos.DrawLine(p1,p2); Gizmos.DrawLine(p2,p3); Gizmos.DrawLine(p3,p0);
                }
            }
        }
#endif
    }
}
