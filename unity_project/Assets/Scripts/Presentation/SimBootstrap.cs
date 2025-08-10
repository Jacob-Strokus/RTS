// Unity presentation bootstrap hooking into pure simulation
using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class SimBootstrap : MonoBehaviour {
        public int PreSpawnCount = 10;
        public GameObject UnitPrefab; // simple capsule prefab
        private Simulator _sim;
        private float _accum;
        private CommandQueue _queue;
        private System.Collections.Generic.List<int> _unitIds = new System.Collections.Generic.List<int>();
    private SelectionManager _selection;

        void Awake() {
            _queue = new CommandQueue();
            _sim = new Simulator(_queue);
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
                _sim.Tick();
            }
            // Right click: group move with simple square formation
            if (Input.GetMouseButtonDown(1)) {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 500f)) {
                    IssueGroupMove(hit.point);
                }
            }

            // B key: spawn placeholder building (for production later)
            if (Input.GetKeyDown(KeyCode.B)) {
                _sim.SpawnBuilding(0, 0, 0, 0, 0);
            }

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
    }
}
