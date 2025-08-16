// Core deterministic simulation loop skeleton
// NOTE: This is engine-agnostic logic; keep UnityEngine references out.
using System.Collections.Generic;
namespace FrontierAges.Sim {
    public static class SimConstants {
        public const int TickRate = 20; // ticks per second
        public const int MsPerTick = 1000 / TickRate; // 50 ms
        public const int PositionScale = 1000; // fixed-point scale (1 world unit = 1000)
    }

    public struct Unit {
        public int Id;
        public short TypeId;
        public short FactionId;
        public int X; // milli-units
        public int Y; // milli-units
        public int HP;
        public short OrderIndex; // future use for order queue index
        public int TargetX; // movement target (milli-units)
        public int TargetY;
        public byte HasMoveTarget; // 0/1
        public int AttackCooldownMs;
        public int CurrentOrderEntity; // e.g. target unit/resource id
        public OrderType CurrentOrderType; // active order being executed
        public int AttackTargetId; // preferred attack target (explicit command)
    public int GatherProgressMs; // accumulation toward one resource unit
    public int CarryAmount; // current carried resource
    public byte CarryResourceType; // which resource is carried
    public byte ReturningWithCargo; // heading to deposit
    public int AttackWindupRemainingMs; // >0 during windup
    }

    public struct UnitTypeData {
        public int MoveSpeedMilliPerSec; // e.g., 2500 = 2.5 units/sec
        public int MaxHP;
        // Attack runtime resolved from attack profile
        public int AttackRange; // milli-units (from profile)
        public int AttackDamageBase;
        public int AttackCooldownMs;
        public int AttackWindupMs;
        public int AttackImpactDelayMs; // if >0 and no projectile, delay damage application
        public byte HasProjectile; // 1 if projectile profile present
        public int ProjectileSpeed;
        public int ProjectileLifetimeMs;
        public byte ProjectileHoming; // 1 = homing
        // Armor values per damage type
        public int ArmorMelee;
        public int ArmorPierce;
        public int ArmorSiege;
        public int ArmorMagic;
    // Damage type multipliers (offense) resolved from profile
    public float DTMelee;
    public float DTPierce;
    public float DTSiege;
    public float DTMagic;
    public int GatherRatePerSec; // integer units per second (scaled ms)
    // Split per-resource gather rates (if zero, fallback to average GatherRatePerSec)
    public int GatherFoodPerSec;
    public int GatherWoodPerSec;
    public int GatherStonePerSec;
    public int GatherMetalPerSec;
    public int CarryCapacity; // max carried units
    public byte Flags; // bit0 = worker
    public byte PopCost; // population cost (default 1)
    // Production economy (placeholder; later derive from JSON)
    public int FoodCost; public int WoodCost; public int StoneCost; public int MetalCost; public int TrainTimeMs;
    }

    public class WorldState {
        public int Tick;
        public Unit[] Units = new Unit[1024];
        public int UnitCount;
        public UnitTypeData[] UnitTypes = new UnitTypeData[64];
        public int UnitTypeCount;
        public Building[] Buildings = new Building[256];
        public int BuildingCount;
        public Faction[] Factions = new Faction[8];
        public ResourceNode[] ResourceNodes = new ResourceNode[256];
        public int ResourceNodeCount;
    public byte[,] Visibility; // fog-of-war tiles (1 visible, 0 unseen) prototype (local player)
    public byte[,] Explored; // enhanced fog: tiles that were ever seen (1 explored)
    public int LocalFactionId; // which faction's vision is represented in Visibility/Explored
    public Projectile[] Projectiles = new Projectile[512];
    public int ProjectileCount;
    // Profiling accumulators (not deterministic-critical)
    public long LastTickDurationMsTimes1000; // micro-ish (approx) using System.Diagnostics stopwatch outside core scope
    public long AvgTickDurationMicro;

    // Tech / research state (up to 2 concurrent researches; bitmask flags)
    public const int MaxConcurrentResearch = 2;
    public int[] FactionTechFlags = new int[8];
    public short[,] FactionResearchTechId = new short[8,MaxConcurrentResearch]; // -1 when slot free
    public int[,] FactionResearchRemainingMs = new int[8,MaxConcurrentResearch];
    public int[,] FactionResearchTotalMs = new int[8,MaxConcurrentResearch];
    public int[] FactionAges = new int[8]; // highest unlocked age (starts at 1)
    }

    public struct Building {
        public int Id;
        public short TypeId;
        public short FactionId;
        public int X;
        public int Y;
        public int HP;
        public short QueueUnitType; // -1 none
        public int QueueRemainingMs; // time remaining
        public byte HasActiveQueue; // 0/1
        public int QueueTotalMs; // original total for progress UI
    public short FootprintW;
    public short FootprintH;
    public byte IsUnderConstruction; // 1 if not finished
    public int BuildTotalMs; // planned total build time
    public int BuildRemainingMs; // remaining until completion
    public int RallyX; public int RallyY; public byte HasRally;
    }

    public struct Faction {
        public int Food, Wood, Stone, Metal;
        public int Pop; // current used population
        public int PopCap; // max population
    }

    public struct ResourceNode {
        public int Id;
        public short ResourceType; // 0=Food 1=Wood etc.
        public int X; public int Y; // milli-units
        public int AmountRemaining; // simple counter
    }

    public enum OrderType : byte { None=0, Move=1, Attack=2, Gather=3, AttackMove=4 }

    public struct QueuedOrder {
        public OrderType Type;
        public int TargetEntityId; // unit/building/resource depending
        public int TargetX; public int TargetY; // for move fallback
    }

    public enum CommandType : byte { None = 0, Move = 1, Attack = 2, Gather = 3, AttackMove = 4 }

    public enum DamageType : byte { Melee=0, Pierce=1, Siege=2, Magic=3 }

    // Damage event emitted for presentation each time damage is applied (after armor)
    public struct DamageEvent {
        public int Tick; public int AttackerUnitId; public int TargetUnitId; public int Damage; public DamageType DType; public int TargetX; public int TargetY; public byte WasKill;
    }

    public struct AttackProfile { public int Range; public int CooldownMs; public int Damage; public int WindupMs; public int ImpactDelayMs; public byte HasProjectile; public int ProjSpeed; public int ProjLifetimeMs; public byte ProjHoming; public float DT_Melee; public float DT_Pierce; public float DT_Siege; public float DT_Magic; }

    public struct Projectile { public int Id; public int X; public int Y; public int TargetUnitId; public short AttackSourceTypeId; public int FactionId; public int Speed; public int Damage; public DamageType DType; public int LifetimeMs; public int AttackerUnitId; }

    public struct Command {
        public int IssueTick;
        public CommandType Type;
        public int EntityId;
        public int TargetX;
        public int TargetY;
    }

    public interface ICommandQueue {
        void Enqueue(Command cmd);
        bool TryDequeue(int currentTick, out Command cmd);
    }

    // Simple ring buffer command queue (not thread-safe; prototype only)
    public class CommandQueue : ICommandQueue {
        private Command[] _buffer = new Command[4096];
        private int _head, _tail;
        public void Enqueue(Command cmd) { _buffer[_tail++ & 4095] = cmd; }
        public bool TryDequeue(int currentTick, out Command cmd) {
            if (_head == _tail) { cmd = default; return false; }
            cmd = _buffer[_head++ & 4095];
            return cmd.IssueTick <= currentTick; // command ready
        }
    }

    public partial class Simulator {
        public WorldState State { get; private set; } = new WorldState();
        private readonly ICommandQueue _cmdQueue;
    private DeterministicRng _rng = new DeterministicRng(0xC0FFEEu);
    // Alias for research slots (declared in WorldState) for use across partials
    private const int MaxConcurrentResearch = WorldState.MaxConcurrentResearch;
    // Exposed as internal so SnapshotUtil & debug systems can read (prototype scope)
    internal readonly Dictionary<int,List<QueuedOrder>> _orderQueues = new Dictionary<int,List<QueuedOrder>>();
    internal readonly Dictionary<int,List<(int x,int y)>> _paths = new Dictionary<int,List<(int x,int y)>>();
    // Spawn tick tracking for replay validation
    internal readonly Dictionary<int,int> _spawnTick = new Dictionary<int,int>();
    private readonly Dictionary<int,int> _unitIndex = new Dictionary<int,int>(); // id -> index
        private bool[,] _grid; // occupancy grid
        private const int GridSize = 128;
        private const int TileSize = SimConstants.PositionScale; // 1 world unit tiles

        // Replay timeline indexing ---------------------------------------------------------
        public class ReplayTimeline
        {
            public struct TickSummary
            {
                public int Tick;
                public int FirstDamageIndex; // index into DamageEvents list
                public int DamageCount;
                public int FirstSimEventIndex; // index into SimEvents list
                public int SimEventCount;
                public int Hash; // truncated state hash (lower 32 bits)
            }

            public readonly List<DamageEvent> DamageEvents = new List<DamageEvent>(4096);
            public readonly List<SimEvent> SimEvents = new List<SimEvent>(4096);
            public readonly List<TickSummary> Ticks = new List<TickSummary>(4096);
            public readonly List<Snapshot> Snapshots = new List<Snapshot>(256);

            public int SnapshotInterval = 50; // configurable
            public int LastRecordedTick = -1;

            public void Begin(int startingTick)
            {
                DamageEvents.Clear();
                SimEvents.Clear();
                Ticks.Clear();
                Snapshots.Clear();
                LastRecordedTick = startingTick - 1;
            }
        }

        private ReplayTimeline _replay;
    public ReplayTimeline Replay => _replay;
    public void LoadReplay(ReplayTimeline timeline){ _replay = timeline; }
        public void EnableReplayRecording(int snapshotInterval=50)
        {
            if(_replay==null) _replay = new ReplayTimeline();
            _replay.SnapshotInterval = snapshotInterval;
            _replay.Begin(State.Tick);
            _replay.Snapshots.Add(SnapshotUtil.Capture(this)); // baseline snapshot
        }
        public void DisableReplayRecording(){ _replay=null; }
        private void RecordReplayFrame()
        {
            if(_replay==null) return;
            var damageSrc = PeekDamageEvents();
            var simSrc = PeekSimEvents();
            var summary = new ReplayTimeline.TickSummary{
                Tick=State.Tick,
                FirstDamageIndex=_replay.DamageEvents.Count,
                DamageCount=damageSrc.Count,
                FirstSimEventIndex=_replay.SimEvents.Count,
                SimEventCount=simSrc.Count,
                Hash=(int)(LastTickHash & 0xFFFFFFFF)
            };
            if(damageSrc.Count>0) _replay.DamageEvents.AddRange(damageSrc);
            if(simSrc.Count>0) _replay.SimEvents.AddRange(simSrc);
            _replay.Ticks.Add(summary);
            _replay.LastRecordedTick = State.Tick;
            if((State.Tick % _replay.SnapshotInterval)==0)
                _replay.Snapshots.Add(SnapshotUtil.Capture(this));
            // Clear events so they are not double-recorded. Presentation layer should drain earlier if needed.
            _damageEvents.Clear();
            _simEvents.Clear();
        }
        public bool TryLoadReplayTick(int targetTick)
        {
            if(_replay==null) return false;
            if(targetTick<0 || targetTick>_replay.LastRecordedTick) return false;
            if(targetTick==State.Tick) return true;
            // find snapshot <= targetTick
            Snapshot best = null;
            for(int i=_replay.Snapshots.Count-1;i>=0;i--){ var s=_replay.Snapshots[i]; if(s.tick<=targetTick){ best=s; break; } }
            if(best==null) return false;
            SnapshotUtil.Apply(this, best);
            // disable recording while fast-forwarding
            var savedReplay = _replay; _replay=null;
            while(State.Tick<targetTick) Tick();
            _replay=savedReplay; return State.Tick==targetTick;
        }

    // ---- Rollback buffer (circular) ----
    private readonly System.Collections.Generic.LinkedList<Snapshot> _rollback = new System.Collections.Generic.LinkedList<Snapshot>();
    private int _rollbackInterval = 20; // ticks
    private int _rollbackCapacity = 16; // number of snapshots to keep
    public void ConfigureRollback(int interval, int capacity){ _rollbackInterval = interval>0?interval:20; _rollbackCapacity = capacity>1?capacity:16; }
    private void RecordRollbackSnapshotIfDue(){ if(_rollbackInterval<=0) return; if(State.Tick % _rollbackInterval != 0) return; var snap = SnapshotUtil.Capture(this); _rollback.AddLast(snap); while(_rollback.Count > _rollbackCapacity) _rollback.RemoveFirst(); }
    public bool TryRollbackToTick(int targetTick){ if(_rollback.Count==0) return false; Snapshot best=null; foreach(var s in _rollback){ if(s.tick<=targetTick) best=s; else break; } if(best==null) best=_rollback.First.Value; SnapshotUtil.Apply(this, best); int goal = targetTick; var saved=_replay; _replay=null; while(State.Tick<goal) Tick(); _replay=saved; return State.Tick==targetTick; }

    // Damage events buffer (cleared when drained by presentation layer)
    private readonly List<DamageEvent> _damageEvents = new List<DamageEvent>(256);
    public int DrainDamageEvents(List<DamageEvent> buffer){ if(buffer==null) return 0; buffer.Clear(); if(_damageEvents.Count==0) return 0; buffer.AddRange(_damageEvents); _damageEvents.Clear(); return buffer.Count; }
    public System.Collections.Generic.IReadOnlyList<DamageEvent> PeekDamageEvents()=>_damageEvents;
    public struct SimEvent { public SimEventType Type; public int Tick; public int A; public int B; public int C; public int D; }
    public enum SimEventType : byte { UnitSpawned=1, UnitDied=2, ResourceCollected=3, ResearchComplete=4, FactionDefeated=5, Victory=6, BuildingDestroyed=7 }
    private readonly List<SimEvent> _simEvents = new List<SimEvent>(256);
    public int DrainSimEvents(List<SimEvent> buffer){ if(buffer==null) return 0; buffer.Clear(); if(_simEvents.Count==0) return 0; buffer.AddRange(_simEvents); _simEvents.Clear(); return buffer.Count; }
    public System.Collections.Generic.IReadOnlyList<SimEvent> PeekSimEvents()=>_simEvents;

    public Simulator(ICommandQueue queue) { _cmdQueue = queue; _grid = new bool[GridSize,GridSize]; State.Visibility = new byte[GridSize,GridSize]; State.Explored = new byte[GridSize,GridSize]; for(int f=0; f<8; f++){ for(int s=0;s<MaxConcurrentResearch;s++){ State.FactionResearchTechId[f,s] = -1; } State.FactionAges[f]=1; } State.LocalFactionId = 0; }
    // Spatial / steering helpers
    private SpatialGrid _spatial = new SpatialGrid(GridSize*SimConstants.PositionScale, GridSize*SimConstants.PositionScale, SimConstants.PositionScale*2);
    private FlowField _flowField = new FlowField();
    public void ActivateFlowFieldTo(int worldX, int worldY){ int tx=worldX/TileSize; int ty=worldY/TileSize; _flowField.Activate(tx,ty,_mapWidth,_mapHeight); }
    public void DeactivateFlowField(){ _flowField.Active=false; }

        public bool AutoAssignWorkersEnabled = true;
    // ---- Deterministic Tick Hash ----
    private const int HashRingSize = 512;
    private ulong[] _hashRing = new ulong[HashRingSize];
    private int[] _hashTickRing = new int[HashRingSize];
    private int _hashRingIndex;
    public ulong LastTickHash { get; private set; }

    // ---- Replay Batches (compressed view) ----
    public struct ReplayBatch { public int Tick; public int StartIndex; public int Count; }
    private System.Collections.Generic.List<ReplayBatch> _replayBatches = new System.Collections.Generic.List<ReplayBatch>(256);
    private bool _replayBatchesDirty;
    private void MarkReplayDirty(){ _replayBatchesDirty = true; }
    private void RebuildReplayBatchesIfNeeded(){ if(!_replayBatchesDirty) return; _replayBatches.Clear(); if(_recorded.Count==0){ _replayBatchesDirty=false; return;} int start=0; int cur=_recorded[0].IssueTick; for(int i=1;i<_recorded.Count;i++){ int t=_recorded[i].IssueTick; if(t!=cur){ _replayBatches.Add( new ReplayBatch{ Tick=cur, StartIndex=start, Count=i-start}); start=i; cur=t; } } _replayBatches.Add(new ReplayBatch{ Tick=cur, StartIndex=start, Count=_recorded.Count-start}); _replayBatchesDirty=false; }
    public System.Collections.Generic.IReadOnlyList<ReplayBatch> GetReplayBatches(){ RebuildReplayBatchesIfNeeded(); return _replayBatches; }
        public void Tick() {
            State.Tick++;
            // Rebuild spatial grid each tick (prototype; can be optimized to dirty-region updates)
            _spatial.Rebuild(State);
            // If a flow field target is active, ensure it's up to date
            if(_flowField.Active && (_flowField.VersionComputedTick != State.Tick || _flowField.NeedsRebuild)) {
                _flowField.Build(State, _grid, _mapWidth, _mapHeight);
            }
            ProcessCommandsWrapper();
            ProcessUnitOrderQueues();
            MovementStep();
            UpdateVision(); // fog-of-war skeleton
            ProductionStep();
            ResearchStep();
            CombatStep();
            GatherStep();
            if (AutoAssignWorkersEnabled) AutoAssignIdleWorkers();
            // Future: research, pathfinding, combat, economy, vision
            ComputeAndStoreTickHash();
            RecordReplayFrame();
            RecordRollbackSnapshotIfDue();
        }

        private static ulong Mix64(ulong z) {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
        private static void HashAdd(ref ulong h, int v) { unchecked { h ^= Mix64((ulong)(uint)v + 0x9E3779B97F4A7C15UL); h = (h << 13) | (h >> 51); } }
        private void ComputeAndStoreTickHash(){
            ulong h = 0xCAFEBABEDEADBEEFUL;
            HashAdd(ref h, State.Tick);
            HashAdd(ref h, State.UnitCount);
            for(int i=0;i<State.UnitCount;i++){ ref var u = ref State.Units[i]; HashAdd(ref h,u.Id); HashAdd(ref h,u.TypeId); HashAdd(ref h,u.FactionId); HashAdd(ref h,u.X); HashAdd(ref h,u.Y); HashAdd(ref h,u.HP); HashAdd(ref h,u.TargetX); HashAdd(ref h,u.TargetY); HashAdd(ref h,u.HasMoveTarget); HashAdd(ref h,(int)u.CurrentOrderType); HashAdd(ref h,u.CurrentOrderEntity); HashAdd(ref h,u.AttackTargetId); HashAdd(ref h,u.AttackWindupRemainingMs); HashAdd(ref h,u.CarryAmount); HashAdd(ref h,u.CarryResourceType); HashAdd(ref h,u.ReturningWithCargo); }
            HashAdd(ref h, State.ProjectileCount);
            for(int i=0;i<State.ProjectileCount;i++){ ref var p = ref State.Projectiles[i]; HashAdd(ref h,p.Id); HashAdd(ref h,p.X); HashAdd(ref h,p.Y); HashAdd(ref h,p.TargetUnitId); HashAdd(ref h,p.AttackSourceTypeId); HashAdd(ref h,p.FactionId); HashAdd(ref h,p.Speed); HashAdd(ref h,p.Damage); HashAdd(ref h,(int)p.DType); HashAdd(ref h,p.LifetimeMs); HashAdd(ref h,p.AttackerUnitId); }
            HashAdd(ref h, State.BuildingCount);
            for(int i=0;i<State.BuildingCount;i++){ ref var b = ref State.Buildings[i]; HashAdd(ref h,b.Id); HashAdd(ref h,b.TypeId); HashAdd(ref h,b.FactionId); HashAdd(ref h,b.X); HashAdd(ref h,b.Y); HashAdd(ref h,b.HP); HashAdd(ref h,b.QueueUnitType); HashAdd(ref h,b.QueueRemainingMs); HashAdd(ref h,b.HasActiveQueue); HashAdd(ref h,b.QueueTotalMs); HashAdd(ref h,b.FootprintW); HashAdd(ref h,b.FootprintH); }
            HashAdd(ref h, State.ResourceNodeCount);
            for(int i=0;i<State.ResourceNodeCount;i++){ ref var rn = ref State.ResourceNodes[i]; HashAdd(ref h,rn.Id); HashAdd(ref h,rn.ResourceType); HashAdd(ref h,rn.X); HashAdd(ref h,rn.Y); HashAdd(ref h,rn.AmountRemaining); }
            for(int f=0; f<State.Factions.Length; f++){ ref var fac = ref State.Factions[f]; HashAdd(ref h,fac.Food); HashAdd(ref h,fac.Wood); HashAdd(ref h,fac.Stone); HashAdd(ref h,fac.Metal); HashAdd(ref h,fac.Pop); HashAdd(ref h,fac.PopCap); HashAdd(ref h, State.FactionTechFlags[f]); HashAdd(ref h, State.FactionAges[f]); for(int s=0;s<MaxConcurrentResearch;s++){ HashAdd(ref h, State.FactionResearchTechId[f,s]); HashAdd(ref h, State.FactionResearchRemainingMs[f,s]); } }
            HashAdd(ref h, (int)_rng.GetState());
            LastTickHash = h;
            _hashRing[_hashRingIndex] = h; _hashTickRing[_hashRingIndex] = State.Tick; _hashRingIndex = (_hashRingIndex + 1) & (HashRingSize-1);
        }
        public int GetRecentHashes(System.Collections.Generic.List<(int tick, ulong hash)> buffer, int max=128){ if(buffer==null) return 0; buffer.Clear(); int available=0; for(int i=0;i<HashRingSize;i++){ if(_hashTickRing[i]!=0) available++; } int take = System.Math.Min(available,max); for(int i=0;i<take;i++){ int idx = (_hashRingIndex - 1 - i) & (HashRingSize-1); buffer.Add((_hashTickRing[idx], _hashRing[idx])); } return buffer.Count; }

    private void ProcessCommands() {
            while (_cmdQueue.TryDequeue(State.Tick, out var cmd)) {
                // Basic validation: entity must have spawned already
                if (!_spawnTick.TryGetValue(cmd.EntityId, out var spawnTick) || spawnTick > State.Tick) continue;
                switch (cmd.Type) {
                    case CommandType.Move:
                        var idx = FindUnitIndex(cmd.EntityId);
                        if (idx >= 0) {
                            ref var u = ref State.Units[idx];
                            u.TargetX = cmd.TargetX;
                            u.TargetY = cmd.TargetY;
                            u.HasMoveTarget = 1;
                QueueOrder(u.Id, new QueuedOrder { Type = OrderType.Move, TargetX = cmd.TargetX, TargetY = cmd.TargetY });
                ComputePathForUnit(u.Id, cmd.TargetX, cmd.TargetY);
                        }
                        break;
                    case CommandType.Attack:
                        var aIdx = FindUnitIndex(cmd.EntityId);
                        if (aIdx >= 0) {
                            ref var au = ref State.Units[aIdx];
                            if (!_spawnTick.ContainsKey(cmd.TargetX) || _spawnTick[cmd.TargetX] > State.Tick) break; // target not yet spawned
                            QueueOrder(au.Id, new QueuedOrder { Type = OrderType.Attack, TargetEntityId = cmd.TargetX }); // TargetX repurposed to carry entity id here
                        }
                        break;
                    case CommandType.Gather:
                        var gIdx = FindUnitIndex(cmd.EntityId);
                        if (gIdx >= 0) {
                            ref var gu = ref State.Units[gIdx];
                            if (!_spawnTick.ContainsKey(cmd.TargetX) || _spawnTick[cmd.TargetX] > State.Tick) break; // resource node not yet spawned
                            QueueOrder(gu.Id, new QueuedOrder { Type = OrderType.Gather, TargetEntityId = cmd.TargetX }); // TargetX carries resource node id
                        }
                        break;
                    case CommandType.AttackMove:
                        var amIdx = FindUnitIndex(cmd.EntityId);
                        if (amIdx>=0){ ref var amu = ref State.Units[amIdx]; QueueOrder(amu.Id, new QueuedOrder{ Type=OrderType.AttackMove, TargetX=cmd.TargetX, TargetY=cmd.TargetY }); }
                        break;
                }
            }
        }

    private void ProcessUnitOrderQueues() {
            // Activate next order if idle
            foreach (var kv in _orderQueues) {
                int unitId = kv.Key; var list = kv.Value; if (list.Count == 0) continue;
                int idx = FindUnitIndex(unitId); if (idx < 0) continue;
                ref var u = ref State.Units[idx];
                if (u.HasMoveTarget == 1 || u.CurrentOrderType != OrderType.None) continue; // busy
                var order = list[0];
                switch (order.Type) {
                    case OrderType.Move:
                        u.TargetX = order.TargetX; u.TargetY = order.TargetY; u.HasMoveTarget = 1; ComputePathForUnit(u.Id, order.TargetX, order.TargetY); list.RemoveAt(0); break;
                    case OrderType.Attack:
                        // Move toward target if not in range; set AttackTargetId for preference
                        u.AttackTargetId = order.TargetEntityId; list.RemoveAt(0); u.CurrentOrderType = OrderType.Attack; u.CurrentOrderEntity = order.TargetEntityId; break;
                    case OrderType.Gather:
                        u.CurrentOrderType = OrderType.Gather; u.CurrentOrderEntity = order.TargetEntityId; list.RemoveAt(0); break;
                    case OrderType.AttackMove:
                        u.CurrentOrderType = OrderType.AttackMove; u.CurrentOrderEntity = 0; u.TargetX=order.TargetX; u.TargetY=order.TargetY; u.HasMoveTarget=1; ComputePathForUnit(u.Id, order.TargetX, order.TargetY); list.RemoveAt(0); break;
                }
            }
        }

    private void MovementStep() {
            // Naive linear movement toward target using per-type speed
            int delta = SimConstants.MsPerTick; // ms per tick
            for (int i = 0; i < State.UnitCount; i++) {
                ref var u = ref State.Units[i];
                // Chase logic for explicit Attack orders: move toward target (unit or building) until in range
                if (u.CurrentOrderType == OrderType.Attack) {
                    int tx=0, ty=0; bool haveTarget=false;
                    if (u.AttackTargetId != 0) {
                        int tu = FindUnitIndex(u.AttackTargetId);
                        if (tu >= 0) { tx = State.Units[tu].X; ty = State.Units[tu].Y; haveTarget = true; }
                        else {
                            int tb = FindBuildingIndex(u.AttackTargetId);
                            if (tb >= 0) { tx = State.Buildings[tb].X; ty = State.Buildings[tb].Y; haveTarget = true; }
                            else { u.AttackTargetId = 0; u.CurrentOrderType = OrderType.None; }
                        }
                    }
                    if (haveTarget) {
                        int range = State.UnitTypes[u.TypeId].AttackRange;
                        long dx = tx - u.X; long dy = ty - u.Y; long d2 = dx*dx + dy*dy; long r2 = (long)range * range;
                        if (d2 > r2) { u.TargetX = tx; u.TargetY = ty; u.HasMoveTarget = 1; ComputePathForUnit(u.Id, tx, ty); }
                        else { u.HasMoveTarget = 0; }
                    }
                }
                if (u.HasMoveTarget == 0) continue;
                // If path exists follow waypoint
                if (_paths.TryGetValue(u.Id, out var path) && path.Count > 0) {
                    // Waypoint world coords
                    var wp = path[0];
                    int wx = wp.x * TileSize + TileSize/2;
                    int wy = wp.y * TileSize + TileSize/2;
                    u.TargetX = wx; u.TargetY = wy;
                    if (System.Math.Abs(u.X - wx) < 200 && System.Math.Abs(u.Y - wy) < 200) {
                        path.RemoveAt(0);
                        if (path.Count == 0) _paths.Remove(u.Id);
                    }
                }
                int speedPerSec = State.UnitTypes[u.TypeId].MoveSpeedMilliPerSec;
                int moveDist = (speedPerSec * delta) / 1000;
                int tdx = u.TargetX - u.X;
                int tdy = u.TargetY - u.Y;
                long dist2 = (long)tdx * tdx + (long)tdy * tdy;
                if (dist2 == 0) { u.HasMoveTarget = 0; continue; }
                long dist = IntegerSqrt(dist2);
                if (dist <= moveDist) { u.X = u.TargetX; u.Y = u.TargetY; u.HasMoveTarget = 0; }
                else {
                    int mvx = (int)(tdx * moveDist / dist);
                    int mvy = (int)(tdy * moveDist / dist);
                    // Flow-field steering if applicable
                    if(_flowField.Active){
                        int tx = u.X/TileSize; int ty = u.Y/TileSize;
                        if(_flowField.TryGetDir(tx, ty, out var fdx, out var fdy)){
                            // Blend 75% path intent, 25% flow vector
                            mvx = (mvx*3 + fdx*moveDist)/4; mvy = (mvy*3 + fdy*moveDist)/4;
                        }
                    }
                    // Separation using spatial grid neighborhood instead of full scan
                    const int sepRadius = 800; const int sepRadius2 = sepRadius*sepRadius;
                    int ax=0, ay=0;
                    foreach(var otherIdx in _spatial.QueryNeighbors(u.X, u.Y, sepRadius)){
                        if(otherIdx==i) continue; ref var o = ref State.Units[otherIdx];
                        int rx = u.X - o.X; int ry = u.Y - o.Y; int r2 = rx*rx + ry*ry; if(r2==0 || r2>sepRadius2) continue; int r = IntegerSqrt(r2); if(r==0) continue; int force = (sepRadius - r); ax += rx * force / r; ay += ry * force / r; }
                    if(ax!=0||ay!=0){ ax/=400; ay/=400; mvx+=ax; mvy+=ay; }
                    u.X += mvx; u.Y += mvy;
                }
            }
        }

        // Integer sqrt (Newton) for movement distance.
        private static int IntegerSqrt(long x) {
            if (x <= 0) return 0;
            long r = x;
            long prev;
            do { prev = r; r = (r + x / r) >> 1; } while (r < prev);
            return (int)r;
        }

        private int FindUnitIndex(int id) {
            for (int i = 0; i < State.UnitCount; i++) if (State.Units[i].Id == id) return i; return -1;
        }

        public int SpawnUnit(short typeId, short factionId, int x, int y, int hp) {
            int id = (State.Tick << 12) | State.UnitCount; // simplistic id generator
            if (State.UnitCount >= State.Units.Length) {
                // naive expand
                var newArr = new Unit[State.Units.Length * 2];
                System.Array.Copy(State.Units, newArr, State.Units.Length);
                State.Units = newArr;
            }
            if (hp <= 0) hp = State.UnitTypes[typeId].MaxHP;
            // Population gating (assume caller verified; enforce anyway)
            ref var fac = ref State.Factions[factionId];
            byte popCost = State.UnitTypes[typeId].PopCost == 0 ? (byte)1 : State.UnitTypes[typeId].PopCost;
            if (fac.Pop + popCost > fac.PopCap && fac.PopCap>0) {
                return -1; // cannot spawn
            }
            State.Units[State.UnitCount] = new Unit { Id = id, TypeId = typeId, FactionId = factionId, X = x, Y = y, HP = hp, CurrentOrderType = OrderType.None };
            _unitIndex[id] = State.UnitCount;
            _spawnTick[id] = State.Tick;
            State.UnitCount++;
            if (fac.PopCap>0) fac.Pop += popCost; // don't track pop if cap is zero (no housing yet)
            _simEvents.Add(new SimEvent{ Type=SimEventType.UnitSpawned, Tick=State.Tick, A=id, B=typeId, C=factionId });
            return id;
        }

        public short RegisterUnitType(UnitTypeData data) {
            short id = (short)State.UnitTypeCount;
            if (id >= State.UnitTypes.Length) {
                var arr = new UnitTypeData[State.UnitTypes.Length * 2];
                System.Array.Copy(State.UnitTypes, arr, State.UnitTypes.Length);
                State.UnitTypes = arr;
            }
            State.UnitTypes[id] = data;
            State.UnitTypeCount++;
            return id;
        }

        public int SpawnBuilding(short typeId, short factionId, int x, int y, int hp) {
            int id = (State.Tick << 20) | State.BuildingCount;
            if (State.BuildingCount >= State.Buildings.Length) {
                var arr = new Building[State.Buildings.Length * 2];
                System.Array.Copy(State.Buildings, arr, State.Buildings.Length);
                State.Buildings = arr;
            }
            if (hp <= 0) hp = 1000; // placeholder
            State.Buildings[State.BuildingCount++] = new Building { Id = id, TypeId = typeId, FactionId = factionId, X = x, Y = y, HP = hp, QueueUnitType = -1, QueueRemainingMs = 0, HasActiveQueue = 0, QueueTotalMs = 0 };
            // Mark occupancy (simple 1 tile) on grid
            int gx = x / TileSize; int gy = y / TileSize;
            if (gx>=0 && gx<GridSize && gy>=0 && gy<GridSize) _grid[gx,gy] = true;
            _spawnTick[id] = State.Tick;
            _flowField.NeedsRebuild = _flowField.NeedsRebuild || _flowField.Active;
            return id;
        }

        // Start construction: building enters list with partial HP and construction timers
        public int StartConstruction(short buildingTypeId, short factionId, int originX, int originY, int wTiles, int hTiles, int buildTimeMs, int maxHP) {
            if (!CanPlaceBuildingRect(originX, originY, wTiles, hTiles)) return -1;
            int id = (State.Tick << 20) | State.BuildingCount;
            if (State.BuildingCount >= State.Buildings.Length) {
                var arr = new Building[State.Buildings.Length * 2];
                System.Array.Copy(State.Buildings, arr, State.Buildings.Length);
                State.Buildings = arr;
            }
            // Reserve grid immediately (simple)
            int gx0 = originX/TileSize; int gy0=originY/TileSize; for(int dx=0;dx<wTiles;dx++) for(int dy=0;dy<hTiles;dy++){ int gx=gx0+dx; int gy=gy0+dy; if(gx>=0&&gy>=0&&gx<GridSize&&gy<GridSize) _grid[gx,gy]=true; }
            int initialHP = maxHP/10; if(initialHP<=0) initialHP=1;
            State.Buildings[State.BuildingCount++] = new Building { Id=id, TypeId=buildingTypeId, FactionId=factionId, X=originX, Y=originY, HP=initialHP, QueueUnitType=-1, QueueRemainingMs=0, HasActiveQueue=0, QueueTotalMs=0, FootprintW=(short)wTiles, FootprintH=(short)hTiles, IsUnderConstruction=1, BuildTotalMs=buildTimeMs, BuildRemainingMs=buildTimeMs };
            _spawnTick[id] = State.Tick;
            _flowField.NeedsRebuild = _flowField.NeedsRebuild || _flowField.Active;
            return id;
        }

        public bool CanPlaceBuildingRect(int originX, int originY, int wTiles, int hTiles) {
            int gx0 = originX / TileSize; int gy0 = originY / TileSize;
            for (int dx=0; dx<wTiles; dx++) for (int dy=0; dy<hTiles; dy++) {
                int gx = gx0+dx; int gy = gy0+dy; if (gx<0||gy<0||gx>=GridSize||gy>=GridSize) return false; if (_grid[gx,gy]) return false; }
            return true;
        }
        public int PlaceBuildingWithFootprint(short typeId, short factionId, int originX, int originY, int wTiles, int hTiles, int hp) {
            if (!CanPlaceBuildingRect(originX, originY, wTiles, hTiles)) return -1;
            int id = SpawnBuilding(typeId, factionId, originX, originY, hp);
            int gx0 = originX/TileSize; int gy0=originY/TileSize; for(int dx=0;dx<wTiles;dx++) for(int dy=0;dy<hTiles;dy++){ int gx=gx0+dx; int gy=gy0+dy; if(gx>=0&&gy>=0&&gx<GridSize&&gy<GridSize) _grid[gx,gy]=true; }
            // store footprint
            int bIdx = FindBuildingIndex(id); if (bIdx>=0) { State.Buildings[bIdx].FootprintW = (short)wTiles; State.Buildings[bIdx].FootprintH = (short)hTiles; }
            return id;
        }

        // New economy-aware placement: returns building id or -1 if insufficient resources / invalid
        public int TryStartConstruction(int factionId, int originX, int originY, int buildingTypeIndex){
            if(buildingTypeIndex<0 || buildingTypeIndex >= DataRegistry.Buildings.Length) return -1;
            var bjson = DataRegistry.Buildings[buildingTypeIndex];
            int w = bjson.footprint!=null? bjson.footprint.w : 2;
            int h = bjson.footprint!=null? bjson.footprint.h : 2;
            if(!CanPlaceBuildingRect(originX, originY, w, h)) return -1;
            // Costs
            int needFood = bjson.cost!=null? bjson.cost.food:0;
            int needWood = bjson.cost!=null? bjson.cost.wood:0;
            int needStone = bjson.cost!=null? bjson.cost.stone:0;
            int needMetal = bjson.cost!=null? bjson.cost.metal:0;
            ref var fac = ref State.Factions[factionId];
            if(fac.Food < needFood || fac.Wood < needWood || fac.Stone < needStone || fac.Metal < needMetal) return -1;
            // Deduct
            fac.Food -= needFood; fac.Wood -= needWood; fac.Stone -= needStone; fac.Metal -= needMetal;
            int buildTime = bjson.buildTimeMs>0? bjson.buildTimeMs : 10000;
            int id = StartConstruction((short)buildingTypeIndex, (short)factionId, originX, originY, w, h, buildTime, bjson.maxHP>0? bjson.maxHP:1000);
            return id;
        }

        // --- Production Multi-Queue ---
        private const int MaxQueuePerBuilding = 5; // active + 4 pending
        private readonly System.Collections.Generic.Dictionary<int,System.Collections.Generic.List<short>> _prodTail = new System.Collections.Generic.Dictionary<int,System.Collections.Generic.List<short>>();
        private bool CanAffordUnit(int factionId, short unitType){ if(unitType<0||unitType>=State.UnitTypeCount) return false; ref var fac = ref State.Factions[factionId]; var utd = State.UnitTypes[unitType]; return fac.Food>=utd.FoodCost && fac.Wood>=utd.WoodCost && fac.Stone>=utd.StoneCost && fac.Metal>=utd.MetalCost; }
        private void DeductUnitCost(int factionId, short unitType){ if(unitType<0||unitType>=State.UnitTypeCount) return; ref var fac = ref State.Factions[factionId]; var utd = State.UnitTypes[unitType]; fac.Food-=utd.FoodCost; fac.Wood-=utd.WoodCost; fac.Stone-=utd.StoneCost; fac.Metal-=utd.MetalCost; }
        private void RefundUnitCost(int factionId, short unitType){ if(unitType<0||unitType>=State.UnitTypeCount) return; ref var fac = ref State.Factions[factionId]; var utd = State.UnitTypes[unitType]; fac.Food+=utd.FoodCost; fac.Wood+=utd.WoodCost; fac.Stone+=utd.StoneCost; fac.Metal+=utd.MetalCost; }
        public bool CancelProduction(int buildingId, int queueIndex){ int idx=FindBuildingIndex(buildingId); if(idx<0) return false; ref var b = ref State.Buildings[idx]; if(queueIndex==0){ if(b.HasActiveQueue==0) return false; RefundUnitCost(b.FactionId,b.QueueUnitType); b.HasActiveQueue=0; b.QueueUnitType=-1; b.QueueRemainingMs=0; b.QueueTotalMs=0; PromoteNextQueued(ref b); State.Buildings[idx]=b; return true; } if(!_prodTail.TryGetValue(b.Id,out var list)) return false; int tailIdx = queueIndex-1; if(tailIdx<0||tailIdx>=list.Count) return false; var ut = list[tailIdx]; RefundUnitCost(b.FactionId, ut); list.RemoveAt(tailIdx); return true; }
        public bool SetRallyPoint(int buildingId, int wx, int wy){ int idx=FindBuildingIndex(buildingId); if(idx<0) return false; ref var b = ref State.Buildings[idx]; b.RallyX=wx; b.RallyY=wy; b.HasRally=1; State.Buildings[idx]=b; return true; }
        public bool ClearRallyPoint(int buildingId){ int idx=FindBuildingIndex(buildingId); if(idx<0) return false; ref var b = ref State.Buildings[idx]; b.HasRally=0; State.Buildings[idx]=b; return true; }
        public void EnqueueTrain(int buildingId, short unitType, int trainTimeMs) {
            int idx = FindBuildingIndex(buildingId); if (idx < 0) return; ref var b = ref State.Buildings[idx]; if (b.IsUnderConstruction==1) return; if(unitType<0||unitType>=State.UnitTypeCount) return; byte popCost = State.UnitTypes[unitType].PopCost==0?(byte)1:State.UnitTypes[unitType].PopCost; ref var fac = ref State.Factions[b.FactionId]; if(fac.PopCap>0 && fac.Pop + popCost > fac.PopCap) return; if(!CanAffordUnit(b.FactionId, unitType)) return; int existing = (b.HasActiveQueue==1?1:0); if(_prodTail.TryGetValue(b.Id,out var list)) existing += list.Count; if(existing>=MaxQueuePerBuilding) return; DeductUnitCost(b.FactionId, unitType); if(b.HasActiveQueue==0){ b.QueueUnitType=unitType; int t = trainTimeMs>0?trainTimeMs: (State.UnitTypes[unitType].TrainTimeMs>0? State.UnitTypes[unitType].TrainTimeMs:5000); b.QueueRemainingMs=t; b.QueueTotalMs=t; b.HasActiveQueue=1; State.Buildings[idx]=b; } else { if(list==null){ list=new System.Collections.Generic.List<short>(); _prodTail[b.Id]=list; } list.Add(unitType); }
        }
        private void PromoteNextQueued(ref Building b){ if(_prodTail.TryGetValue(b.Id,out var list) && list.Count>0){ var ut = list[0]; list.RemoveAt(0); b.QueueUnitType=ut; var utd = State.UnitTypes[ut]; int t = utd.TrainTimeMs>0? utd.TrainTimeMs:5000; b.QueueRemainingMs=t; b.QueueTotalMs=t; b.HasActiveQueue=1; } }
        private void ProductionStep() {
            for (int i=0;i<State.BuildingCount;i++) { ref var b = ref State.Buildings[i]; if (b.IsUnderConstruction==1) { b.BuildRemainingMs -= SimConstants.MsPerTick; int maxHP=1000; int providesPop=0; if(b.TypeId>=0 && b.TypeId < DataRegistry.Buildings.Length){ var bj=DataRegistry.Buildings[b.TypeId]; if(bj!=null){ if(bj.maxHP>0) maxHP=bj.maxHP; providesPop=bj.providesPopulation; } } int builtMs=b.BuildTotalMs - b.BuildRemainingMs; if(builtMs<0) builtMs=0; if(b.BuildTotalMs<1) b.BuildTotalMs=1; b.HP = (int)((long)maxHP * builtMs / b.BuildTotalMs); if(b.HP<1) b.HP=1; if(b.BuildRemainingMs<=0){ b.IsUnderConstruction=0; b.BuildRemainingMs=0; b.HP=maxHP; if(providesPop>0){ ref var f2 = ref State.Factions[b.FactionId]; f2.PopCap += providesPop; } } State.Buildings[i]=b; continue; }
                if(b.HasActiveQueue==0){ PromoteNextQueued(ref b); State.Buildings[i]=b; if(b.HasActiveQueue==0) continue; }
                b.QueueRemainingMs -= SimConstants.MsPerTick; if(b.QueueRemainingMs<=0){ if(b.QueueUnitType>=0 && b.QueueUnitType<State.UnitTypeCount){ byte popCost = State.UnitTypes[b.QueueUnitType].PopCost==0?(byte)1:State.UnitTypes[b.QueueUnitType].PopCost; ref var fac3 = ref State.Factions[b.FactionId]; if(fac3.PopCap==0 || fac3.Pop + popCost <= fac3.PopCap){ int spawnX = b.X + _rng.Range(-2000,2000); int spawnY = b.Y + _rng.Range(-2000,2000); SpawnUnit((short)b.QueueUnitType, b.FactionId, spawnX, spawnY, 0); if(b.HasRally==1){ IssueMoveImmediateToNewest(spawnX, spawnY, b.RallyX, b.RallyY); } } }
                    b.HasActiveQueue=0; b.QueueUnitType=-1; b.QueueRemainingMs=0; b.QueueTotalMs=0; PromoteNextQueued(ref b); State.Buildings[i]=b; }
            }
        }
        private void IssueMoveImmediateToNewest(int nearX, int nearY, int targetX, int targetY){ // naive: find unit closest to spawn coords without a move target yet
            int best=-1; long bestD=long.MaxValue; for(int i=0;i<State.UnitCount;i++){ ref var u = ref State.Units[i]; if(u.HasMoveTarget==1) continue; long dx=u.X-nearX; long dy=u.Y-nearY; long d=dx*dx+dy*dy; if(d<bestD){ bestD=d; best=i; } }
            if(best>=0){ ref var u = ref State.Units[best]; u.TargetX=targetX; u.TargetY=targetY; u.HasMoveTarget=1; ComputePathForUnit(u.Id, targetX, targetY); }
        }

        private int FindBuildingIndex(int id) {
            for (int i = 0; i < State.BuildingCount; i++) if (State.Buildings[i].Id == id) return i; return -1;
        }

        private void RemoveBuildingByIndex(int idx){
            // Cleanup grid occupancy by footprint if available
            ref var b = ref State.Buildings[idx];
            int w = b.FootprintW>0? b.FootprintW:1; int h=b.FootprintH>0? b.FootprintH:1;
            int gx0=b.X/TileSize; int gy0=b.Y/TileSize;
            for(int dx=0;dx<w;dx++) for(int dy=0;dy<h;dy++){
                int gx=gx0+dx, gy=gy0+dy; if(gx>=0&&gy>=0&&gx<GridSize&&gy<GridSize) _grid[gx,gy]=false;
            }
            int id=b.Id; short typeId=b.TypeId; short faction=b.FactionId;
            // Adjust pop cap if this building provided it
            if(typeId>=0 && typeId < DataRegistry.Buildings.Length){ var bj = DataRegistry.Buildings[typeId]; if(bj!=null && bj.providesPopulation>0){ ref var f = ref State.Factions[faction]; f.PopCap -= bj.providesPopulation; if(f.PopCap<0) f.PopCap=0; } }
            // Compact array
            State.BuildingCount--; if(idx!=State.BuildingCount){ State.Buildings[idx]=State.Buildings[State.BuildingCount]; }
            _simEvents.Add(new SimEvent{ Type=SimEventType.BuildingDestroyed, Tick=State.Tick, A=id, B=typeId, C=faction });
            // Rebuild flow field if needed
            _flowField.NeedsRebuild = _flowField.NeedsRebuild || _flowField.Active;
            // Evaluate win/loss after building destruction
            CheckWinLoss();
        }

        public void DestroyBuilding(int buildingId){ int idx=FindBuildingIndex(buildingId); if(idx>=0) RemoveBuildingByIndex(idx); }

        private void QueueOrder(int unitId, QueuedOrder order) {
            if (!_orderQueues.TryGetValue(unitId, out var list)) {
                list = new List<QueuedOrder>(4);
                _orderQueues[unitId] = list;
            }
            list.Add(order);
        }


    private void ComputePathForUnit(int unitId, int targetX, int targetY) {
            int sx, sy, tx, ty; GetUnitTile(unitId, out sx, out sy); tx = targetX/TileSize; ty = targetY/TileSize;
            var path = AStar(sx, sy, tx, ty);
            if (path != null) _paths[unitId] = path;
        }

        private void GetUnitTile(int unitId, out int x, out int y) {
            int idx = FindUnitIndex(unitId); if (idx<0) { x=y=0; return; }
            ref var u = ref State.Units[idx]; x = u.X/TileSize; y = u.Y/TileSize;
        }

        // Reusable A* allocations
        private Dictionary<int,Node> _aNodes = new Dictionary<int,Node>(256);
        private PriorityQueue<Node> _aOpen = new PriorityQueue<Node>();
        private List<(int x,int y)> AStar(int sx,int sy,int tx,int ty) {
            if (sx==tx && sy==ty) return new List<(int,int)>();
            _aNodes.Clear(); _aOpen.Clear();
            Node start = new Node{X=sx,Y=sy,G=0,H=Heuristic(sx,sy,tx,ty)}; _aNodes[Key(sx,sy)] = start; _aOpen.Push(start);
            int[] dirs = { -1,0, 1,0, 0,-1, 0,1, -1,-1, -1,1, 1,-1, 1,1};
            while(_aOpen.Count>0) {
                var cur = _aOpen.Pop();
                if (cur.X==tx && cur.Y==ty) return Reconstruct(cur);
                for (int di=0; di<8; di++) {
                    int nx = cur.X + dirs[di*2]; int ny = cur.Y + dirs[di*2+1];
                    if (nx<0||ny<0||nx>=GridSize||ny>=GridSize) continue;
                    if (_grid[nx,ny]) continue;
                    int g = cur.G + ((di>=4)?14:10);
                    int k = Key(nx,ny);
                    if (!_aNodes.TryGetValue(k, out var nn) || g < nn.G) {
                        nn = new Node{X=nx,Y=ny,G=g,H=Heuristic(nx,ny,tx,ty),Parent=cur};
                        _aNodes[k]=nn; _aOpen.Push(nn);
                    }
                }
            }
            return new List<(int,int)>();
        }

        private int Heuristic(int x,int y,int tx,int ty){int dx=System.Math.Abs(x-tx);int dy=System.Math.Abs(y-ty);return 10*(dx+dy)-6*System.Math.Min(dx,dy);} // diag heuristic
        private int Key(int x,int y)=> (x<<16)^y;
        private List<(int x,int y)> Reconstruct(Node end){var list=new List<(int,int)>();var c=end;while(c.Parent!=null){list.Insert(0,(c.X,c.Y));c=c.Parent;}return list;}

        private class Node : IHeapItem<Node> {
            public int X,Y,G,H; public Node Parent; public int F=>G+H; public int HeapIndex{get;set;} public int CompareTo(Node other)=> other.F.CompareTo(F);
        }
        private class PriorityQueue<T> where T: IHeapItem<T> { List<T> items=new List<T>(); public int Count=>items.Count; public void Push(T item){item.HeapIndex=items.Count;items.Add(item);SortUp(item);} public T Pop(){var first=items[0]; int lastIndex=items.Count-1; items[0]=items[lastIndex]; items[0].HeapIndex=0; items.RemoveAt(lastIndex); if(items.Count>0) SortDown(items[0]); return first;} void SortUp(T item){while(true){int parent=(item.HeapIndex-1)/2; if(parent<0) break; if(items[parent].CompareTo(item)>0){Swap(item,items[parent]);} else break;} } void SortDown(T item){ while(true){int left=item.HeapIndex*2+1; int right=left+1; int swap=-1; if(left<items.Count){swap=left; if(right<items.Count && items[left].CompareTo(items[right])<0) swap=right; if(items[swap].CompareTo(item)>0){Swap(item,items[swap]);} else return;} else return;} } void Swap(T a,T b){int ai=a.HeapIndex; int bi=b.HeapIndex; items[ai]=b; items[bi]=a; a.HeapIndex=bi; b.HeapIndex=ai;} public void Clear(){items.Clear();} }
        private interface IHeapItem<T>:System.IComparable<T>{int HeapIndex {get;set;}}

        // Resource Nodes
        public int SpawnResourceNode(short resourceType, int x, int y, int amount) {
            int id = (State.Tick << 16) | State.ResourceNodeCount;
            if (State.ResourceNodeCount >= State.ResourceNodes.Length) {
                var arr = new ResourceNode[State.ResourceNodes.Length * 2];
                System.Array.Copy(State.ResourceNodes, arr, State.ResourceNodes.Length);
                State.ResourceNodes = arr;
            }
            State.ResourceNodes[State.ResourceNodeCount++] = new ResourceNode { Id = id, ResourceType = resourceType, X = x, Y = y, AmountRemaining = amount };
            _spawnTick[id] = State.Tick;
            return id;
        }

        private int FindResourceNodeIndex(int id) { for (int i=0;i<State.ResourceNodeCount;i++) if (State.ResourceNodes[i].Id==id) return i; return -1; }

        private void RemoveUnitByIndex(int idx) {
            int id = State.Units[idx].Id;
            short factionId = State.Units[idx].FactionId;
            short typeId = State.Units[idx].TypeId;
            _paths.Remove(id);
            _orderQueues.Remove(id);
            _unitIndex.Remove(id);
            _spawnTick.Remove(id);
            // Decrement population
            ref var fac = ref State.Factions[factionId];
            byte popCost = State.UnitTypes[State.Units[idx].TypeId].PopCost==0?(byte)1:State.UnitTypes[State.Units[idx].TypeId].PopCost;
            if (fac.Pop >= popCost) fac.Pop -= popCost;
            State.UnitCount--; if (idx != State.UnitCount) { State.Units[idx] = State.Units[State.UnitCount]; _unitIndex[State.Units[idx].Id] = idx; }
            _simEvents.Add(new SimEvent{ Type=SimEventType.UnitDied, Tick=State.Tick, A=id, B=typeId, C=factionId });
            // Evaluate win/loss after a death (buildings removal should also trigger this when implemented)
            CheckWinLoss();
        }

    // Public helper APIs for gameplay layer
    // Note: Recording-aware versions are defined later; keep a single definition to avoid duplicate members.
    public bool TryGetPath(int unitId, List<(int x,int y)> buffer) { if (_paths.TryGetValue(unitId, out var p)) { buffer.Clear(); buffer.AddRange(p); return true; } return false; }

        // Fog-of-war skeleton: mark tiles around each faction 0 unit as visible (simple diamond radius)
        private byte[,] _prevVisibility = new byte[GridSize,GridSize];
        private List<(int x,int y)> _visionDirty = new List<(int x,int y)>(512);
        private int _mapWidth = GridSize; private int _mapHeight = GridSize; // configurable later
        public void ConfigureMapSize(int w, int h) { _mapWidth = System.Math.Clamp(w,1,GridSize); _mapHeight = System.Math.Clamp(h,1,GridSize); }
        private void UpdateVision() {
            var vis = State.Visibility; if (vis == null) return;
            // Reset only active map region
            for (int x=0;x<_mapWidth;x++) for (int y=0;y<_mapHeight;y++) vis[x,y]=0;
            int radiusTiles = 6; // placeholder vision radius
            for (int i=0;i<State.UnitCount;i++) { ref var u = ref State.Units[i]; if(u.FactionId!=State.LocalFactionId) continue; int ux = u.X/TileSize; int uy = u.Y/TileSize; for (int dx=-radiusTiles; dx<=radiusTiles; dx++) for (int dy=-radiusTiles; dy<=radiusTiles; dy++) { int gx=ux+dx; int gy=uy+dy; if (gx<0||gy<0||gx>=_mapWidth||gy>=_mapHeight) continue; if (dx*dx+dy*dy <= radiusTiles*radiusTiles) vis[gx,gy]=1; } }
            // Track dirties comparing with previous
            _visionDirty.Clear();
            for (int x=0;x<_mapWidth;x++) for (int y=0;y<_mapHeight;y++) { if (vis[x,y]==1) State.Explored[x,y]=1; byte prev = _prevVisibility[x,y]; if (vis[x,y] != prev) { _visionDirty.Add((x,y)); _prevVisibility[x,y] = vis[x,y]; } else if (State.Explored[x,y]==1 && prev==0) { // remained unseen but explored newly? not possible
                }
            }
        }
        public IReadOnlyList<(int x,int y)> GetVisionDirty() => _visionDirty;
        public bool IsWorldPosVisible(int worldX, int worldY){ int gx=worldX/TileSize; int gy=worldY/TileSize; if(gx<0||gy<0||gx>=_mapWidth||gy>=_mapHeight) return false; return State.Visibility[gx,gy]==1; }
        public void SetLocalVisionFaction(int factionId){ State.LocalFactionId = System.Math.Clamp(factionId,0,State.Factions.Length-1); }

    // --- Research / Tech System (multi-slot) ---
    public bool IsTechResearched(int factionId, short techIndex){ return (State.FactionTechFlags[factionId] & (1<<techIndex))!=0; }
    private int FindTechIndex(string id){ if(string.IsNullOrEmpty(id)) return -1; for(short i=0;i<DataRegistry.Techs.Length;i++) if(DataRegistry.Techs[i].id==id) return i; return -1; }
    private bool HasPrereqs(int factionId, short techIndex){ if(techIndex<0||techIndex>=DataRegistry.Techs.Length) return false; var t=DataRegistry.Techs[techIndex]; if(t.prereq!=null){ foreach(var rid in t.prereq){ short pi=(short)FindTechIndex(rid); if(pi<0 || !IsTechResearched(factionId,pi)) return false; } } return true; }
    private static bool CanAfford(ref Faction f, CostJson c){ if(c==null) return true; return f.Food>=c.food && f.Wood>=c.wood && f.Stone>=c.stone && f.Metal>=c.metal; }
    private static void DeductCost(ref Faction f, CostJson c){ if(c==null) return; f.Food-=c.food; f.Wood-=c.wood; f.Stone-=c.stone; f.Metal-=c.metal; }
    public bool StartResearch(short techIndex, int factionId){ if(factionId<0||factionId>=State.Factions.Length) return false; if(techIndex<0||techIndex>=DataRegistry.Techs.Length) return false; if(IsTechResearched(factionId, techIndex)) return false; var tech = DataRegistry.Techs[techIndex]; if(State.FactionAges[factionId] < tech.age) return false; if(!HasPrereqs(factionId, techIndex)) return false; ref var fac = ref State.Factions[factionId]; if(!CanAfford(ref fac, tech.cost)) return false; DeductCost(ref fac, tech.cost); for(int s=0;s<MaxConcurrentResearch;s++){ if(State.FactionResearchTechId[factionId,s]==-1){ State.FactionResearchTechId[factionId,s]=techIndex; State.FactionResearchRemainingMs[factionId,s]=tech.researchTimeMs; State.FactionResearchTotalMs[factionId,s]=tech.researchTimeMs; return true; } } return false; }

        // Fast forward utility using replay batches (for editor scrub)
        public void FastForwardFromBaseline(Snapshot baseline, System.Collections.Generic.List<Command> recorded, int targetRelativeTick){ if(baseline==null||recorded==null){ return; } SnapshotUtil.Apply(State, baseline); // Reset world
            // Reset internal aux state
            _orderQueues.Clear(); _paths.Clear(); _spawnTick.Clear(); _unitIndex.Clear(); // baseline apply repopulates spawn ticks via Apply? (captures spawn) else we repopulate when spawning new units
            _recorded = recorded; _playback=false; _recording=false; _replayBatchesDirty=true; RebuildReplayBatchesIfNeeded(); int currentRelative=0; var batches = GetReplayBatches(); // Iterate batches
            for(int bi=0; bi<batches.Count; bi++){ var b = batches[bi]; if(b.Tick>targetRelativeTick) break; // advance time to batch tick
                while(currentRelative < b.Tick && currentRelative < targetRelativeTick){ TickNoReplay(); currentRelative++; }
                // execute commands in batch
                for(int ci=0; ci<b.Count; ci++){ var c = _recorded[b.StartIndex+ci]; ExecuteCommandImmediate(c); }
            }
            while(currentRelative < targetRelativeTick){ TickNoReplay(); currentRelative++; }
        }
        private void TickNoReplay(){ State.Tick++; ProcessCommands(); ProcessUnitOrderQueues(); MovementStep(); UpdateVision(); ProductionStep(); ResearchStep(); CombatStep(); GatherStep(); if (AutoAssignWorkersEnabled) AutoAssignIdleWorkers(); ComputeAndStoreTickHash(); }
        private void ExecuteCommandImmediate(Command cmd){ // simplified mirror of ProcessCommands logic
            if(!_spawnTick.TryGetValue(cmd.EntityId, out var spawnTick) || spawnTick>State.Tick) return;
            switch(cmd.Type){
                case CommandType.Move: {
                    var idx = FindUnitIndex(cmd.EntityId);
                    if(idx>=0){
                        ref var u = ref State.Units[idx];
                        u.TargetX=cmd.TargetX; u.TargetY=cmd.TargetY; u.HasMoveTarget=1;
                        QueueOrder(u.Id,new QueuedOrder{ Type=OrderType.Move, TargetX=cmd.TargetX, TargetY=cmd.TargetY});
                        ComputePathForUnit(u.Id, cmd.TargetX, cmd.TargetY);
                    }
                    break;
                }
                case CommandType.Attack: {
                    var idx = FindUnitIndex(cmd.EntityId);
                    if(idx>=0){
                        ref var u = ref State.Units[idx];
                        if(_spawnTick.ContainsKey(cmd.TargetX) && _spawnTick[cmd.TargetX]<=State.Tick){
                            QueueOrder(u.Id, new QueuedOrder{ Type=OrderType.Attack, TargetEntityId=cmd.TargetX });
                        }
                    }
                    break;
                }
                case CommandType.Gather: {
                    var idx = FindUnitIndex(cmd.EntityId);
                    if(idx>=0){
                        ref var u = ref State.Units[idx];
                        if(_spawnTick.ContainsKey(cmd.TargetX) && _spawnTick[cmd.TargetX]<=State.Tick){
                            QueueOrder(u.Id, new QueuedOrder{ Type=OrderType.Gather, TargetEntityId=cmd.TargetX });
                        }
                    }
                    break;
                }
            }
        }

        // Replay skeleton
    private List<Command> _recorded = new List<Command>(4096);
    private bool _recording;
    private bool _playback;
    private int _playbackIndex;
    private int _playbackStartTick;
    private int _recordStartTick;
    private Snapshot _baselineSnapshot; // stored at StartRecording for scrub resets (immutable reference)
    public void StartRecording() { _recorded.Clear(); _recordStartTick = State.Tick; _baselineSnapshot = SnapshotUtil.Capture(State); _recording = true; _playback = false; MarkReplayDirty(); }
    public List<Command> StopRecording() { _recording = false; return new List<Command>(_recorded); }
    public void StartPlayback(List<Command> cmds) { _recorded = cmds ?? new List<Command>(); _playback = true; _recording = false; _playbackIndex = 0; _playbackStartTick = State.Tick; MarkReplayDirty(); }
    public bool IsRecording => _recording; public bool IsPlayback => _playback;
    public IReadOnlyList<Command> GetRecordedCommands() => _recorded;

        // Inject playback commands each tick before processing live ones
    private void InjectPlaybackCommands() {
            if (!_playback) return; while (_playbackIndex < _recorded.Count) { var c = _recorded[_playbackIndex]; int relative = c.IssueTick; // use original tick delta
                if (State.Tick - _playbackStartTick >= relative) { _cmdQueue.Enqueue(new Command { IssueTick = State.Tick, Type = c.Type, EntityId = c.EntityId, TargetX = c.TargetX, TargetY = c.TargetY }); _playbackIndex++; }
                else break; }
            if (_playbackIndex >= _recorded.Count) _playback = false; }

    // Modify ProcessCommands entry point to log & playback injection
        private void ProcessCommandsWrapper() { InjectPlaybackCommands(); ProcessCommands(); }

    private void RecordCommand(Command c) {
            if (!_recording) return;
            // store relative tick offset in IssueTick for playback
            c.IssueTick = State.Tick - _recordStartTick;
            _recorded.Add(c);
            MarkReplayDirty();
        }

        // Public gameplay APIs that record
        public void IssueMoveCommand(int unitId, int targetX, int targetY) {
            var cmd = new Command { IssueTick = State.Tick, Type = CommandType.Move, EntityId = unitId, TargetX = targetX, TargetY = targetY };
            RecordCommand(cmd); _cmdQueue.Enqueue(cmd);
        }
        public void IssueAttackCommand(int unitId, int targetUnitId) { var cmd = new Command { IssueTick = State.Tick, Type = CommandType.Attack, EntityId = unitId, TargetX = targetUnitId }; RecordCommand(cmd); _cmdQueue.Enqueue(cmd); }
    public void IssueGatherCommand(int unitId, int resourceNodeId) { var cmd = new Command { IssueTick = State.Tick, Type = CommandType.Gather, EntityId = unitId, TargetX = resourceNodeId }; RecordCommand(cmd); _cmdQueue.Enqueue(cmd); }
    // Schedule future-tick command (used by lockstep). Not recorded (replay handles deterministic distribution).
    public void ScheduleCommand(CommandType type, int entityId, int targetX, int targetY, int executeTick){ _cmdQueue.Enqueue(new Command{ IssueTick=executeTick, Type=type, EntityId=entityId, TargetX=targetX, TargetY=targetY }); }
    // Tick already uses ProcessCommandsWrapper inside Tick method
    }

    // Win/Loss evaluation helper (placed outside Simulator methods above to avoid patch overlap)
    // Note: This method belongs to Simulator class region; placed here to keep compilation consistent.
    public partial class Simulator {
        private void CheckWinLoss(){
            bool[] alive = new bool[State.Factions.Length];
            for(int i=0;i<State.UnitCount;i++) alive[State.Units[i].FactionId] = true;
            for(int i=0;i<State.BuildingCount;i++) alive[State.Buildings[i].FactionId] = true;
            int survivors = 0; int last = -1;
            for(int f=0; f<State.Factions.Length; f++){
                if(alive[f]){ survivors++; last=f; }
                else {
                    if(State.FactionTechFlags[f] != -1){ _simEvents.Add(new SimEvent{ Type=SimEventType.FactionDefeated, Tick=State.Tick, A=f }); State.FactionTechFlags[f] = -1; }
                }
            }
            if(survivors==1 && last>=0){ _simEvents.Add(new SimEvent{ Type=SimEventType.Victory, Tick=State.Tick, A=last }); }
        }
    }

    // Simple deterministic RNG (xorshift32)
    public struct DeterministicRng {
        private uint _state;
        public DeterministicRng(uint seed) { _state = seed == 0 ? 1u : seed; }
        public uint NextU32() { uint x = _state; x ^= x << 13; x ^= x >> 17; x ^= x << 5; _state = x; return x; }
        public int Range(int minInclusive, int maxExclusive) { return (int)(NextU32() % (uint)(maxExclusive - minInclusive)) + minInclusive; }
        public float NextFloat01() { return (NextU32() & 0xFFFFFF) / (float)0x1000000; }
    public uint GetState() => _state;
    }

    // Snapshot DTOs for save/load (simplified JSON-friendly)
    public static class SnapshotVersions { public const string Current = "7"; }
    [System.Serializable] public class Snapshot {
        public int tick;
        public UnitSnap[] units;
        public BuildingSnap[] buildings;
        public FactionSnap[] factions;
        public ResourceNodeSnap[] resourceNodes;
        // Metadata
        public string version; // semantic minor only for now
        public long savedUnix;
        public int unitCount;
        public int buildingCount;
    public int[] factionPop; public int[] factionPopCap;
    public int[] factionTechFlags; public short[] activeResearchTech; public int[] activeResearchRemaining; public byte[,] explored; // simple serialization (Unity JSON won't handle multidim; placeholder, may need flatten later)
    public ProjectileSnap[] projectiles;
    }
    [System.Serializable] public class UnitSnap { public int id; public short type; public short faction; public int x; public int y; public int hp; public OrderType currentOrder; public int currentOrderEntity; public int spawnTick; public int attackCooldown; public int windup; public int[] orderTypes; public int[] orderEnts; public int[] orderXs; public int[] orderYs; public int[] pathX; public int[] pathY; }
    [System.Serializable] public class BuildingSnap { public int id; public short type; public short faction; public int x; public int y; public int hp; public int queueUnitType; public int queueRemaining; public int queueTotal; public byte hasQueue; public short fw; public short fh; public int spawnTick; }
    [System.Serializable] public class FactionSnap { public int food; public int wood; public int stone; public int metal; }
    [System.Serializable] public class ResourceNodeSnap { public int id; public short r; public int x; public int y; public int amt; public int spawnTick; }
    [System.Serializable] public class ProjectileSnap { public int id; public int x; public int y; public int target; public short sourceType; public int faction; public int speed; public int damage; public DamageType dtype; public int lifetime; public int attacker; }

    public static class SnapshotUtil {
        // Capture APIs
        public static Snapshot Capture(WorldState ws) => CaptureInternal(ws, null, null, null);
        public static Snapshot Capture(Simulator sim) => CaptureInternal(sim.State, sim._orderQueues, sim._paths, sim._spawnTick);

        private static Snapshot CaptureInternal(WorldState ws, Dictionary<int,List<QueuedOrder>> orderQueues, Dictionary<int,List<(int x,int y)>> paths, Dictionary<int,int> spawnTicks) {
            var snap = new Snapshot {
                tick = ws.Tick,
                units = new UnitSnap[ws.UnitCount],
                buildings = new BuildingSnap[ws.BuildingCount],
                factions = new FactionSnap[ws.Factions.Length],
                resourceNodes = new ResourceNodeSnap[ws.ResourceNodeCount],
                projectiles = new ProjectileSnap[ws.ProjectileCount],
                version = SnapshotVersions.Current,
                savedUnix = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                unitCount = ws.UnitCount,
                buildingCount = ws.BuildingCount,
                factionPop = new int[ws.Factions.Length], factionPopCap = new int[ws.Factions.Length], factionTechFlags = new int[ws.Factions.Length], activeResearchTech = new short[ws.Factions.Length], activeResearchRemaining = new int[ws.Factions.Length]
            };
            for (int i = 0; i < ws.UnitCount; i++) {
                ref var u = ref ws.Units[i];
                int st=0; if (spawnTicks!=null) spawnTicks.TryGetValue(u.Id, out st);
                var us = new UnitSnap { id = u.Id, type = u.TypeId, faction = u.FactionId, x = u.X, y = u.Y, hp = u.HP, currentOrder = u.CurrentOrderType, currentOrderEntity = u.CurrentOrderEntity, spawnTick = st, attackCooldown = u.AttackCooldownMs, windup = u.AttackWindupRemainingMs };
                if (orderQueues != null && orderQueues.TryGetValue(u.Id, out var q) && q.Count > 0) {
                    int max = q.Count > 16 ? 16 : q.Count; // cap
                    us.orderTypes = new int[max]; us.orderEnts = new int[max]; us.orderXs = new int[max]; us.orderYs = new int[max];
                    for (int oi=0; oi<max; oi++) {
                        var qo = q[oi]; us.orderTypes[oi] = (int)qo.Type; us.orderEnts[oi] = qo.TargetEntityId; us.orderXs[oi] = qo.TargetX; us.orderYs[oi] = qo.TargetY;
                    }
                }
                if (paths != null && paths.TryGetValue(u.Id, out var path) && path.Count > 0) {
                    int maxp = path.Count > 64 ? 64 : path.Count; // cap
                    us.pathX = new int[maxp]; us.pathY = new int[maxp];
                    for (int pi=0; pi<maxp; pi++) { var wp = path[pi]; us.pathX[pi] = wp.x; us.pathY[pi] = wp.y; }
                }
                snap.units[i] = us;
            }
            for (int i = 0; i < ws.BuildingCount; i++) {
                ref var b = ref ws.Buildings[i];
                int st=0; if (spawnTicks!=null) spawnTicks.TryGetValue(b.Id, out st);
                snap.buildings[i] = new BuildingSnap { id = b.Id, type = b.TypeId, faction = b.FactionId, x = b.X, y = b.Y, hp = b.HP, queueUnitType = b.QueueUnitType, queueRemaining = b.QueueRemainingMs, queueTotal = b.QueueTotalMs, hasQueue = b.HasActiveQueue, fw = b.FootprintW, fh = b.FootprintH, spawnTick = st };
            }
        for (int f=0; f<ws.Factions.Length; f++) { ref var fac = ref ws.Factions[f]; snap.factions[f] = new FactionSnap { food=fac.Food, wood=fac.Wood, stone=fac.Stone, metal=fac.Metal }; if (snap.factionPop!=null){ snap.factionPop[f]=fac.Pop; snap.factionPopCap[f]=fac.PopCap; } if (snap.factionTechFlags!=null){ snap.factionTechFlags[f]=ws.FactionTechFlags[f]; // snapshot only first research slot for compatibility
            snap.activeResearchTech[f]=ws.FactionResearchTechId[f,0];
            snap.activeResearchRemaining[f]=ws.FactionResearchRemainingMs[f,0]; } }
            for (int r=0; r<ws.ResourceNodeCount; r++) { ref var rn = ref ws.ResourceNodes[r]; int st=0; if (spawnTicks!=null) spawnTicks.TryGetValue(rn.Id, out st); snap.resourceNodes[r] = new ResourceNodeSnap { id = rn.Id, r = rn.ResourceType, x = rn.X, y = rn.Y, amt = rn.AmountRemaining, spawnTick = st }; }
            for (int p=0; p<ws.ProjectileCount; p++){ ref var pr = ref ws.Projectiles[p]; snap.projectiles[p] = new ProjectileSnap{ id=pr.Id, x=pr.X, y=pr.Y, target=pr.TargetUnitId, sourceType=pr.AttackSourceTypeId, faction=pr.FactionId, speed=pr.Speed, damage=pr.Damage, dtype=pr.DType, lifetime=pr.LifetimeMs, attacker=pr.AttackerUnitId}; }
            return snap;
        }

        // Apply APIs
        public static void Apply(WorldState ws, Snapshot snap) => ApplyInternal(ws, null, null, null, snap);
        public static void Apply(Simulator sim, Snapshot snap) => ApplyInternal(sim.State, sim._orderQueues, sim._paths, sim._spawnTick, snap);

        private static void ApplyInternal(WorldState ws, Dictionary<int,List<QueuedOrder>> orderQueues, Dictionary<int,List<(int x,int y)>> paths, Dictionary<int,int> spawnTicks, Snapshot snap) {
            ws.Tick = snap.tick;
            ws.UnitCount = 0; ws.BuildingCount = 0; ws.ResourceNodeCount = 0;
            // Clear existing queues/paths/spawn ticks
            orderQueues?.Clear(); paths?.Clear(); spawnTicks?.Clear();
            if (string.IsNullOrEmpty(snap.version)) snap.version = "1"; // legacy default
            if (snap.version != SnapshotVersions.Current) SnapshotMigrator.Migrate(snap);
            if (snap.units != null) {
                if (ws.Units.Length < snap.units.Length) ws.Units = new Unit[snap.units.Length];
                foreach (var us in snap.units) {
                    ws.Units[ws.UnitCount++] = new Unit { Id = us.id, TypeId = us.type, FactionId = us.faction, X = us.x, Y = us.y, HP = us.hp, CurrentOrderType = us.currentOrder, CurrentOrderEntity = us.currentOrderEntity, AttackCooldownMs = us.attackCooldown, AttackWindupRemainingMs = us.windup };
                    if (orderQueues != null && us.orderTypes != null) {
                        var list = new List<QueuedOrder>(us.orderTypes.Length);
                        for (int i=0;i<us.orderTypes.Length;i++) list.Add(new QueuedOrder { Type = (OrderType)us.orderTypes[i], TargetEntityId = us.orderEnts?[i] ?? 0, TargetX = us.orderXs?[i] ?? 0, TargetY = us.orderYs?[i] ?? 0 });
                        orderQueues[us.id] = list;
                    }
                    if (paths != null && us.pathX != null) {
                        var plist = new List<(int x,int y)>(us.pathX.Length);
                        for (int i=0;i<us.pathX.Length;i++) plist.Add((us.pathX[i], us.pathY[i]));
                        paths[us.id] = plist;
                    }
                    if (spawnTicks != null) spawnTicks[us.id] = us.spawnTick;
                }
            }
            if (snap.factions != null) {
                for (int f=0; f<ws.Factions.Length && f<snap.factions.Length; f++) {
                    var fs = snap.factions[f];
                    ws.Factions[f].Food=fs.food; ws.Factions[f].Wood=fs.wood; ws.Factions[f].Stone=fs.stone; ws.Factions[f].Metal=fs.metal;
                    if (snap.factionPop!=null && f<snap.factionPop.Length) ws.Factions[f].Pop = snap.factionPop[f];
                    if (snap.factionPopCap!=null && f<snap.factionPopCap.Length) ws.Factions[f].PopCap = snap.factionPopCap[f];
                }
            }
            if (snap.factionTechFlags!=null) {
                for(int f=0; f<ws.Factions.Length && f<snap.factionTechFlags.Length; f++) {
                    ws.FactionTechFlags[f]=snap.factionTechFlags[f];
                    // restore only first research slot (others remain default/unchanged)
                    if (snap.activeResearchTech!=null && f<snap.activeResearchTech.Length) ws.FactionResearchTechId[f,0]=snap.activeResearchTech[f];
                    if (snap.activeResearchRemaining!=null && f<snap.activeResearchRemaining.Length) ws.FactionResearchRemainingMs[f,0]=snap.activeResearchRemaining[f];
                }
            }
            if (snap.buildings != null) {
                if (ws.Buildings.Length < snap.buildings.Length) ws.Buildings = new Building[snap.buildings.Length];
                foreach (var bs in snap.buildings) {
                    ws.Buildings[ws.BuildingCount++] = new Building { Id = bs.id, TypeId = bs.type, FactionId = bs.faction, X = bs.x, Y = bs.y, HP = bs.hp, QueueUnitType = (short)bs.queueUnitType, QueueRemainingMs = bs.queueRemaining, HasActiveQueue = bs.hasQueue, QueueTotalMs = bs.queueTotal, FootprintW = bs.fw, FootprintH = bs.fh };
                    if (spawnTicks != null) spawnTicks[bs.id] = bs.spawnTick;
                }
            }
            if (snap.resourceNodes != null) {
                if (ws.ResourceNodes.Length < snap.resourceNodes.Length) ws.ResourceNodes = new ResourceNode[snap.resourceNodes.Length];
                foreach (var rn in snap.resourceNodes) {
                    ws.ResourceNodes[ws.ResourceNodeCount++] = new ResourceNode { Id=rn.id, ResourceType=rn.r, X=rn.x, Y=rn.y, AmountRemaining=rn.amt };
                    if (spawnTicks!=null) spawnTicks[rn.id] = rn.spawnTick;
                }
            }
            if (snap.projectiles != null){ if(ws.Projectiles.Length < snap.projectiles.Length) ws.Projectiles = new Projectile[snap.projectiles.Length]; ws.ProjectileCount=0; foreach(var pr in snap.projectiles){ ws.Projectiles[ws.ProjectileCount++] = new Projectile{ Id=pr.id, X=pr.x, Y=pr.y, TargetUnitId=pr.target, AttackSourceTypeId=pr.sourceType, FactionId=pr.faction, Speed=pr.speed, Damage=pr.damage, DType=pr.dtype, LifetimeMs=pr.lifetime, AttackerUnitId=pr.attacker}; } }
        }
    }
    // Migration stub (v1 -> v3 adds spawnTick, then pop/tech arrays)
    public static class SnapshotMigrator {
        public static void Migrate(Snapshot snap) {
            if (snap.version == SnapshotVersions.Current) return;
            // Chain migrations to latest
            if (snap.version == "1") { snap.version = "2"; }
            if (snap.version == "2") { // introduce pop/tech arrays (v3)
                snap.factionPop = new int[8]; snap.factionPopCap = new int[8]; snap.factionTechFlags = new int[8]; snap.activeResearchTech = new short[8]; snap.activeResearchRemaining = new int[8]; snap.version = "3"; }
            if (snap.version == "3" || snap.version=="4" || snap.version=="5") { // versions prior to projectile serialization
                if (snap.projectiles==null) snap.projectiles = new ProjectileSnap[0]; snap.version = "6"; }
            if (snap.version == "6") { // add attacker field
                if (snap.projectiles!=null){ for(int i=0;i<snap.projectiles.Length;i++){ if(snap.projectiles[i]!=null) { /* attacker default 0 */ } } }
                snap.version = SnapshotVersions.Current;
            }
            if (snap.version != SnapshotVersions.Current) snap.version = SnapshotVersions.Current;
        }
    }

    // ---- Spatial Grid (for neighbor queries) ----
    internal class SpatialGrid {
        private List<int>[,] _cells;
        private int _cellSize;
        private int _width; private int _height;
        public SpatialGrid(int mapW, int mapH, int cellSize){ _cellSize=cellSize; _width=mapW; _height=mapH; int cw = (mapW+cellSize-1)/cellSize; int ch=(mapH+cellSize-1)/cellSize; _cells = new List<int>[cw,ch]; for(int x=0;x<cw;x++) for(int y=0;y<ch;y++) _cells[x,y]=new List<int>(32); }
        public void ResizeIfNeeded(int mapW, int mapH){ if(mapW==_width && mapH==_height) return; _width=mapW; _height=mapH; int cw = (mapW+_cellSize-1)/_cellSize; int ch=(mapH+_cellSize-1)/_cellSize; _cells = new List<int>[cw,ch]; for(int x=0;x<cw;x++) for(int y=0;y<ch;y++) _cells[x,y]=new List<int>(32); }
        public void Rebuild(WorldState state){ foreach(var list in _cells) list.Clear(); for(int i=0;i<state.UnitCount;i++){ ref var u = ref state.Units[i]; int cx=u.X/ _cellSize; int cy=u.Y/ _cellSize; if(cx>=0 && cy>=0 && cx<_cells.GetLength(0) && cy<_cells.GetLength(1)) _cells[cx,cy].Add(i); } }
        public IEnumerable<int> QueryNeighbors(int x, int y, int radius){ int cx = x/_cellSize; int cy=y/_cellSize; int rCells = radius/_cellSize + 1; for(int dx=-rCells; dx<=rCells; dx++) for(int dy=-rCells; dy<=rCells; dy++){ int nx=cx+dx; int ny=cy+dy; if(nx<0||ny<0||nx>=_cells.GetLength(0)||ny>=_cells.GetLength(1)) continue; var cell=_cells[nx,ny]; for(int i=0;i<cell.Count;i++) yield return cell[i]; } }
    }

    // ---- Flow Field (single-target) ----
    internal class FlowField {
        public bool Active; public int TargetX; public int TargetY; public bool NeedsRebuild; public int VersionComputedTick;
        private short[,] _vx; private short[,] _vy; private int _w; private int _h; private const short Scale=1000;
        public void Activate(int tx, int ty, int mapW, int mapH){ TargetX=tx; TargetY=ty; Active=true; NeedsRebuild=true; Ensure(mapW,mapH); }
        private void Ensure(int w, int h){ if(_vx!=null && _w==w && _h==h) return; _w=w; _h=h; _vx=new short[w,h]; _vy=new short[w,h]; }
        public void Build(WorldState state, bool[,] grid, int mapW, int mapH){ if(!Active) return; Ensure(mapW,mapH); for(int x=0;x<mapW;x++) for(int y=0;y<mapH;y++){ _vx[x,y]=0; _vy[x,y]=0; }
            // BFS from target tile
            var q = new System.Collections.Generic.Queue<(int x,int y)>();
            var dist = new int[mapW,mapH]; for(int x=0;x<mapW;x++) for(int y=0;y<mapH;y++) dist[x,y]=-1;
            if(TargetX<0||TargetY<0||TargetX>=mapW||TargetY>=mapH){ Active=false; return; }
            dist[TargetX,TargetY]=0; q.Enqueue((TargetX,TargetY)); int[] dirs={1,0,-1,0,0,1,0,-1};
            while(q.Count>0){ var c=q.Dequeue(); int cd=dist[c.x,c.y]; for(int di=0; di<4; di++){ int nx=c.x+dirs[di*2]; int ny=c.y+dirs[di*2+1]; if(nx<0||ny<0||nx>=mapW||ny>=mapH) continue; if(grid[nx,ny]) continue; if(dist[nx,ny]!=-1) continue; dist[nx,ny]=cd+1; q.Enqueue((nx,ny)); } }
            // For each traversable cell choose neighbor with lowest dist as direction
            for(int x=0;x<mapW;x++) for(int y=0;y<mapH;y++){ int d=dist[x,y]; if(d<=0) continue; int best=d; int bx=0,by=0; for(int di=0; di<4; di++){ int nx=x+dirs[di*2]; int ny=y+dirs[di*2+1]; if(nx<0||ny<0||nx>=mapW||ny>=mapH) continue; int nd=dist[nx,ny]; if(nd>=0 && nd<best){ best=nd; bx=dirs[di*2]; by=dirs[di*2+1]; } }
                _vx[x,y]=(short)(bx*Scale); _vy[x,y]=(short)(by*Scale); }
            NeedsRebuild=false; VersionComputedTick=state.Tick;
        }
        public bool TryGetDir(int tileX,int tileY, out int dx, out int dy){ if(!_In(tileX,tileY)){ dx=dy=0; return false; } dx=_vx[tileX,tileY]; dy=_vy[tileX,tileY]; return dx!=0||dy!=0; }
        private bool _In(int x,int y)=> _vx!=null && x>=0&&y>=0&&x<_w&&y<_h;
    }
}
