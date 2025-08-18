// Unity presentation bootstrap hooking into pure simulation
using UnityEngine;
using FrontierAges.Sim;
using System.IO;
using System.Linq;
using System.IO.Compression;

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
    private Color _ghostValidColor = new Color(0,1,0,0.4f);
    private Color _ghostInvalidColor = new Color(1,0,0,0.4f);
    public Material FogMaterial; // assigned in inspector (optional)
    private FogOverlay _fog;
    private bool _autoAssignWorkers = true;
    private System.Diagnostics.Stopwatch _tickSw = new System.Diagnostics.Stopwatch();
    private readonly System.Collections.Generic.Dictionary<int, GameObject> _projectileViews = new System.Collections.Generic.Dictionary<int, GameObject>();
    public GameObject ProjectilePrefab; // simple sphere/capsule
    [Header("World-space UI")]
    public Canvas WorldSpaceCanvas;
    public GameObject HealthBarPrefab;

        private int _placeBuildingIndex = 0; // index into DataRegistry.Buildings
        private int _lastPlacedBuildingIndexPersisted;
    private string _lastSnapshotJson; // in-memory snapshot store (prototype)
         void Awake() {
            _queue = new CommandQueue();
            _sim = new Simulator(_queue);
            _autoAssignWorkers = PlayerPrefs.GetInt("fa_autoAssign",1)==1;
            _sim.AutoAssignWorkersEnabled = _autoAssignWorkers;
            // Ensure minimal default prefabs exist so bootstrap is playable without setup
            if (UnitPrefab == null) UnitPrefab = CreateDefaultUnitPrefab();
            if (BuildingViewPrefab == null) BuildingViewPrefab = CreateDefaultBuildingViewPrefab();
            if (BuildingGhostPrefab == null) BuildingGhostPrefab = CreateDefaultGhostPrefab();

            // Register provisional unit type 0 (worker placeholder)
            var typeId = _sim.RegisterUnitType(new FrontierAges.Sim.UnitTypeData {
                MoveSpeedMilliPerSec = 2500,
                MaxHP = 50,
                AttackDamageBase = 5,
                AttackCooldownMs = 1000,
                AttackRange = 3000,
                GatherRatePerSec = 1,
                CarryCapacity = 5,
                Flags = 1, // worker
                PopCost = 1
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
            // Grant starting resources
            _sim.State.Factions[0].Food = 300; _sim.State.Factions[0].Wood = 500; _sim.State.Factions[0].Stone = 300; _sim.State.Factions[0].Metal = 200; _sim.State.Factions[0].PopCap = 10; // baseline from starting town center assumption
            _placeBuildingIndex = PlayerPrefs.GetInt("fa_lastBuildingIndex", 0);
            _lastPlacedBuildingIndexPersisted = _placeBuildingIndex;

            // Fog overlay instantiation
            if (FogMaterial){ var fogGo = new GameObject("FogOverlay"); fogGo.transform.position = Vector3.zero; _fog = fogGo.AddComponent<FogOverlay>(); var mf = fogGo.AddComponent<MeshFilter>(); fogGo.AddComponent<MeshRenderer>().sharedMaterial = FogMaterial; _fog.Init(_sim,128,128); }

            // Runtime fallbacks: ensure SelectionManager and Help overlay exist for minimal UX
            if (FindObjectOfType<SelectionManager>() == null) { var selGo = new GameObject("SelectionManager"); selGo.AddComponent<SelectionManager>(); }
            if (FindObjectOfType<MinimalHelpOverlay>() == null) { var helpGo = new GameObject("HelpOverlay"); helpGo.AddComponent<MinimalHelpOverlay>(); }
        }

        private GameObject CreateDefaultUnitPrefab(){
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Unit_DefaultPrefab";
            var rend = go.GetComponent<Renderer>();
            if (rend) {
                try { var mat = rend.material; if (mat != null) mat.color = new Color(0.65f,0.85f,1f,1f); } catch {}
            }
            var col = go.GetComponent<Collider>(); if (col) col.isTrigger = false;
            go.AddComponent<UnitView>();
            return go;
        }

        private GameObject CreateDefaultBuildingViewPrefab(){
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Building_DefaultPrefab";
            var rend = go.GetComponent<Renderer>();
            if (rend) {
                try { var mat = rend.material; if (mat != null) mat.color = new Color(0.8f,0.75f,0.6f,1f); } catch {}
            }
            var existingCol = go.GetComponent<Collider>(); if (existingCol && !(existingCol is BoxCollider)) { Destroy(existingCol); }
            if (!go.TryGetComponent<BoxCollider>(out var _)) go.AddComponent<BoxCollider>();
            go.AddComponent<BuildingView>();
            return go;
        }

        private GameObject CreateDefaultGhostPrefab(){
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Building_GhostPrefab";
            // Make it semi-transparent green; remove collider to avoid blocking raycasts
            var coll = go.GetComponent<Collider>(); if (coll) Destroy(coll);
            var rend = go.GetComponent<Renderer>();
            if (rend) {
                try {
                    var mat = rend.material; // instance a per-renderer material
                    if (mat != null) {
                        mat.color = new Color(0f,1f,0f,0.35f);
                        // Try to enable transparency if supported by the current shader (best-effort, no hard dependency)
                        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f); // URP Lit transparent
                        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f); // Built-in Standard transparent
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.renderQueue = 3000;
                    }
                } catch {}
            }
            return go;
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
                SyncProjectiles();
                // Drain damage events and spawn floating text
                if(FloatingTextManager){ _damageBuffer ??= new System.Collections.Generic.List<FrontierAges.Sim.DamageEvent>(128); if(_sim.DrainDamageEvents(_damageBuffer)>0){ foreach(var de in _damageBuffer){ FloatingTextManager.Spawn(de); } } }
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

            // Shift+T enqueue multiple (stress multi-queue)
            if (Input.GetKeyDown(KeyCode.Y)) {
                if (_sim.State.BuildingCount > 0) { var bId=_sim.State.Buildings[0].Id; for(int i=0;i<3;i++) _sim.EnqueueTrain(bId,0,5000); }
            }

            // Set rally point at mouse with R (when holding LeftShift)
            if (Input.GetKeyDown(KeyCode.P)) {
                if (_sim.State.BuildingCount>0) {
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if(Physics.Raycast(ray,out var hit,500f)){
                        int wx = (int)(hit.point.x * SimConstants.PositionScale);
                        int wy = (int)(hit.point.z * SimConstants.PositionScale);
                        _sim.SetRallyPoint(_sim.State.Buildings[0].Id, wx, wy);
                        Debug.Log($"Rally set {wx},{wy}");
                    }
                }
            }
            if (Input.GetKeyDown(KeyCode.O)) { if(_sim.State.BuildingCount>0){ _sim.ClearRallyPoint(_sim.State.Buildings[0].Id); Debug.Log("Rally cleared"); } }

            // Cancel active production slot (C), cancel tail last (V)
            if (Input.GetKeyDown(KeyCode.C)) { if(_sim.State.BuildingCount>0) _sim.CancelProduction(_sim.State.Buildings[0].Id,0); }
            if (Input.GetKeyDown(KeyCode.V)) { if(_sim.State.BuildingCount>0) { // cancel last tail index
                    // naive: attempt high index values until fail
                    for(int qi=5; qi>=1; qi--){ if(_sim.CancelProduction(_sim.State.Buildings[0].Id, qi)) break; }
                } }

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

            // Shift + A : attack-move to cursor for selection
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.A)) {
                IssueAttackMoveFromSelection();
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

            if (ReplayScrubSlider && _cachedReplay!=null && _cachedReplay.Count>0) {
                if (Input.GetMouseButtonUp(0) && Time.time - _scrubLastChangeTime > ScrubDebounceSeconds) ApplyPendingScrub();
            }

            // R key: start first available tech (index 0) if not researched; fallback no-op
            if (Input.GetKeyDown(KeyCode.R)) {
                _sim.StartResearch(0, 0);
            }
        }

    // Floating combat text support
    public FloatingCombatTextManager FloatingTextManager;
    private System.Collections.Generic.List<FrontierAges.Sim.DamageEvent> _damageBuffer;

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

        private void IssueAttackMoveFromSelection(){ if(_selection==null || _selection.Selected.Count==0) return; var ray=Camera.main.ScreenPointToRay(Input.mousePosition); if(Physics.Raycast(ray,out var hit,500f)){ int tx = (int)(hit.point.x * SimConstants.PositionScale); int ty = (int)(hit.point.z * SimConstants.PositionScale); foreach(var id in _selection.Selected){ _queue.Enqueue( new FrontierAges.Sim.Command{ IssueTick=_sim.State.Tick, Type=FrontierAges.Sim.CommandType.AttackMove, EntityId=id, TargetX=tx, TargetY=ty}); } } }

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
            // Clear projectile views (will respawn as they exist next tick)
            foreach(var kv in _projectileViews) if(kv.Value) Destroy(kv.Value); _projectileViews.Clear();
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
                    // Use economy aware construction start
                    int newId = _sim.TryStartConstruction(0, worldX, worldY, _placeBuildingIndex);
                    if (newId!=-1) {
                    _placingBuilding = false; if (_ghost) Destroy(_ghost);
                    // Instantiate view at placed location
                    if (BuildingViewPrefab) {
                        var viewGo = Instantiate(BuildingViewPrefab);
                        var bv = viewGo.GetComponent<BuildingView>(); if (!bv) bv = viewGo.AddComponent<BuildingView>();
                            bv.Init(newId, _sim, w, h);
                        }
                    } else {
                        Debug.Log("Cannot afford or invalid placement");
                    }
                }
                if (Input.GetMouseButtonDown(1)) { _placingBuilding = false; if (_ghost) Destroy(_ghost); }
            }
        }

        private void SyncProjectiles(){ var ws=_sim.State; // remove stale
            _projectileViews.Keys.Where(id=> !HasProjectile(id, ws)).ToList().ForEach(id=> { if(_projectileViews[id]) Destroy(_projectileViews[id]); _projectileViews.Remove(id); });
            for(int i=0;i<ws.ProjectileCount;i++){ ref var p = ref ws.Projectiles[i]; if(!_projectileViews.ContainsKey(p.Id)){ if(ProjectilePrefab){ var go=Instantiate(ProjectilePrefab); go.name=$"Projectile_{p.Id}"; var pv=go.GetComponent<ProjectileView>(); if(!pv) pv=go.AddComponent<ProjectileView>(); pv.Init(p.Id, p.TargetUnitId); _projectileViews[p.Id]=go; go.transform.position = new Vector3(p.X/(float)SimConstants.PositionScale, 0.2f, p.Y/(float)SimConstants.PositionScale); } } else { var go=_projectileViews[p.Id]; if(go) go.transform.position = new Vector3(p.X/(float)SimConstants.PositionScale, 0.2f, p.Y/(float)SimConstants.PositionScale); } }
        }
        private bool HasProjectile(int id, WorldState ws){ for(int i=0;i<ws.ProjectileCount;i++) if(ws.Projectiles[i].Id==id) return true; return false; }

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

        public UnityEngine.UI.Slider ReplayScrubSlider; // assign in canvas
        private bool _scrubInProgress;
    private System.Collections.Generic.List<FrontierAges.Sim.Command> _cachedReplay;
        public RectTransform SnapshotListContainer; public GameObject SnapshotListItemPrefab; // UI list
        private Snapshot _recordBaseline; // local cached baseline for scrub
        private float _scrubLastChangeTime;
        public float ScrubDebounceSeconds = 0.15f;
        public UnityEngine.UI.Text SnapshotMetaText;
        public void OnReplayScrubChanged(float v){ _scrubLastChangeTime = Time.time; }
    private void ApplyPendingScrub(){ if(!ReplayScrubSlider) return; float v = ReplayScrubSlider.value; int targetTick = Mathf.RoundToInt(v * (_cachedReplay[_cachedReplay.Count-1].IssueTick)); JumpToReplayTick(targetTick); }
        public void RefreshSnapshotList(){ if(!Directory.Exists(SnapshotDirectory) || SnapshotListContainer==null || SnapshotListItemPrefab==null) return; foreach(Transform c in SnapshotListContainer) Destroy(c.gameObject); var files = Directory.GetFiles(SnapshotDirectory, "snap_*.json.gz").Concat(Directory.GetFiles(SnapshotDirectory, "snap_*.json")).OrderByDescending(f=>f).Take(50); foreach(var f in files){ Snapshot snap=null; try { snap = LoadSnapshotFromFile(f); } catch{} if(snap==null) continue; var go = Instantiate(SnapshotListItemPrefab, SnapshotListContainer); var btn = go.GetComponent<UnityEngine.UI.Button>(); var txt = go.GetComponentInChildren<UnityEngine.UI.Text>(); if(txt) txt.text = $"{Path.GetFileNameWithoutExtension(f)} U:{snap.unitCount} B:{snap.buildingCount} v{snap.version}"; if(btn) btn.onClick.AddListener(()=> { SnapshotUtil.Apply(_sim,snap); RebuildViews(); if(SnapshotMetaText) SnapshotMetaText.text=$"Tick {snap.tick} Units {snap.unitCount} Buildings {snap.buildingCount} v{snap.version}"; }); // add delete if child button named Delete exists
                var del = go.transform.Find("Delete"); if(del){ var db = del.GetComponent<UnityEngine.UI.Button>(); if(db){ string pathCopy=f; db.onClick.AddListener(()=> { try{ File.Delete(pathCopy);} catch{} RefreshSnapshotList(); }); } }
            } }
    private Snapshot LoadSnapshotFromFile(string path){ if(path.EndsWith(".gz")){ using(var fs=File.OpenRead(path)) using(var gz=new GZipStream(fs,CompressionMode.Decompress)) using(var ms=new MemoryStream()){ gz.CopyTo(ms); var json=System.Text.Encoding.UTF8.GetString(ms.ToArray()); return JsonUtility.FromJson<Snapshot>(json);} } else { var json=File.ReadAllText(path); return JsonUtility.FromJson<Snapshot>(json);} }
    private void SaveSnapshotToDisk(Snapshot snap){ if(!Directory.Exists(SnapshotDirectory)) Directory.CreateDirectory(SnapshotDirectory); var json=JsonUtility.ToJson(snap); var bytes=System.Text.Encoding.UTF8.GetBytes(json); var file=$"snap_{System.DateTime.UtcNow:yyyyMMdd_HHmmss}_{snap.unitCount}u_{snap.buildingCount}b.json.gz"; var path=Path.Combine(SnapshotDirectory,file); using(var fs=File.Create(path)) using(var gz=new GZipStream(fs,System.IO.Compression.CompressionLevel.Optimal)){ gz.Write(bytes,0,bytes.Length);} }
        public string SnapshotDirectory => Path.Combine(Application.persistentDataPath, "Snapshots");
    public void UiSaveSnapshot() { var snap = FrontierAges.Sim.SnapshotUtil.Capture(_sim); _lastSnapshotJson = JsonUtility.ToJson(snap); }
    public void UiLoadSnapshot() { if(string.IsNullOrEmpty(_lastSnapshotJson)) return; var snap = JsonUtility.FromJson<FrontierAges.Sim.Snapshot>(_lastSnapshotJson); FrontierAges.Sim.SnapshotUtil.Apply(_sim, snap); RebuildViews(); }
    public void UiSaveSnapshotToDisk(){ var snap=SnapshotUtil.Capture(_sim); SaveSnapshotToDisk(snap); RefreshSnapshotList(); }
    public void UiLoadLatestSnapshotFromDisk(){ if(!Directory.Exists(SnapshotDirectory)) return; var latest = Directory.GetFiles(SnapshotDirectory, "snap_*.json.gz").Concat(Directory.GetFiles(SnapshotDirectory, "snap_*.json")).OrderByDescending(f=>f).FirstOrDefault(); if(latest==null) return; var snap=LoadSnapshotFromFile(latest); SnapshotUtil.Apply(_sim,snap); RebuildViews(); }
    public void UiStartRecording(){ _sim.StartRecording(); _recordBaseline = SnapshotUtil.Capture(_sim.State); Debug.Log("Replay recording started"); }
    public void UiStopRecording(){ _cachedReplay = _sim.StopRecording(); ReplayScrubSlider?.SetValueWithoutNotify(0f); }
    public void UiPlayRecording(){ if(_cachedReplay==null||_cachedReplay.Count==0){ Debug.Log("No recording"); return; } _sim.StartPlayback(_cachedReplay); }
        private void JumpToReplayTick(int relativeTick){ if(_cachedReplay==null||_recordBaseline==null) return; // Use new fast forward API
            // _cachedReplay already stores Simulator.Command entries
            _sim.FastForwardFromBaseline(_recordBaseline, _cachedReplay, relativeTick);
            RebuildViews(); }
    }
}
