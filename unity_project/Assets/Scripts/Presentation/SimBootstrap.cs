// Unity presentation bootstrap hooking into pure simulation
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class SimBootstrap : MonoBehaviour {
        public int PreSpawnCount = 10;
        public GameObject UnitPrefab; // simple capsule prefab
    public GameObject BuildingGhostPrefab; // simple cube for placement preview
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
                _queue.Enqueue(new Command { IssueTick = _sim.State.Tick, Type = CommandType.Move, EntityId = ids[i], TargetX = baseX + ox, TargetY = baseY + oy });
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
        }
#endif
    }
}
