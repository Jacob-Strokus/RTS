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
    }

    public struct UnitTypeData {
        public int MoveSpeedMilliPerSec; // e.g., 2500 = 2.5 units/sec
        public int MaxHP;
        public int AttackRange; // milli-units
        public int AttackDamage;
        public int AttackCooldownMs;
    public int GatherRatePerSec; // integer units per second (scaled ms)
    public int CarryCapacity; // max carried units
    public byte Flags; // bit0 = worker
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
    public byte[,] Visibility; // fog-of-war tiles (1 visible, 0 unseen) prototype
    // Profiling accumulators (not deterministic-critical)
    public long LastTickDurationMsTimes1000; // micro-ish (approx) using System.Diagnostics stopwatch outside core scope
    public long AvgTickDurationMicro;
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
    }

    public struct Faction {
        public int Food, Wood, Stone, Metal;
    }

    public struct ResourceNode {
        public int Id;
        public short ResourceType; // 0=Food 1=Wood etc.
        public int X; public int Y; // milli-units
        public int AmountRemaining; // simple counter
    }

    public enum OrderType : byte { None=0, Move=1, Attack=2, Gather=3 }

    public struct QueuedOrder {
        public OrderType Type;
        public int TargetEntityId; // unit/building/resource depending
        public int TargetX; public int TargetY; // for move fallback
    }

    public enum CommandType : byte { None = 0, Move = 1, Attack = 2, Gather = 3 }

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

    public class Simulator {
        public WorldState State { get; private set; } = new WorldState();
        private readonly ICommandQueue _cmdQueue;
    private DeterministicRng _rng = new DeterministicRng(0xC0FFEEu);
    // Exposed as internal so SnapshotUtil & debug systems can read (prototype scope)
    internal readonly Dictionary<int,List<QueuedOrder>> _orderQueues = new Dictionary<int,List<QueuedOrder>>();
    internal readonly Dictionary<int,List<(int x,int y)>> _paths = new Dictionary<int,List<(int x,int y)>>();
    private readonly Dictionary<int,int> _unitIndex = new Dictionary<int,int>(); // id -> index
        private bool[,] _grid; // occupancy grid
        private const int GridSize = 128;
        private const int TileSize = SimConstants.PositionScale; // 1 world unit tiles

    public Simulator(ICommandQueue queue) { _cmdQueue = queue; _grid = new bool[GridSize,GridSize]; State.Visibility = new byte[GridSize,GridSize]; }

        public bool AutoAssignWorkersEnabled = true;
        public void Tick() {
            State.Tick++;
            ProcessCommandsWrapper();
            ProcessUnitOrderQueues();
            MovementStep();
            UpdateVision(); // fog-of-war skeleton
            ProductionStep();
            CombatStep();
            GatherStep();
            if (AutoAssignWorkersEnabled) AutoAssignIdleWorkers();
            // Future: research, pathfinding, combat, economy, vision
        }

    private void ProcessCommands() {
            while (_cmdQueue.TryDequeue(State.Tick, out var cmd)) {
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
                            QueueOrder(au.Id, new QueuedOrder { Type = OrderType.Attack, TargetEntityId = cmd.TargetX }); // TargetX repurposed to carry entity id here
                        }
                        break;
                    case CommandType.Gather:
                        var gIdx = FindUnitIndex(cmd.EntityId);
                        if (gIdx >= 0) {
                            ref var gu = ref State.Units[gIdx];
                            QueueOrder(gu.Id, new QueuedOrder { Type = OrderType.Gather, TargetEntityId = cmd.TargetX }); // TargetX carries resource node id
                        }
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
                }
            }
        }

        private void MovementStep() {
            // Naive linear movement toward target using per-type speed
            int delta = SimConstants.MsPerTick; // ms per tick
            for (int i = 0; i < State.UnitCount; i++) {
                ref var u = ref State.Units[i];
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
                int dx = u.TargetX - u.X;
                int dy = u.TargetY - u.Y;
                long dist2 = (long)dx * dx + (long)dy * dy;
                if (dist2 == 0) { u.HasMoveTarget = 0; continue; }
                // If within moveDist, snap
                long dist = IntegerSqrt(dist2);
                if (dist <= moveDist) {
                    u.X = u.TargetX;
                    u.Y = u.TargetY;
                    u.HasMoveTarget = 0;
                } else {
                    // move proportionally
                    int mvx = (int)(dx * moveDist / dist);
                    int mvy = (int)(dy * moveDist / dist);
                    u.X += mvx;
                    u.Y += mvy;
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
            State.Units[State.UnitCount] = new Unit { Id = id, TypeId = typeId, FactionId = factionId, X = x, Y = y, HP = hp, CurrentOrderType = OrderType.None };
            _unitIndex[id] = State.UnitCount;
            State.UnitCount++;
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

        public void EnqueueTrain(int buildingId, short unitType, int trainTimeMs) {
            int idx = FindBuildingIndex(buildingId);
            if (idx < 0) return;
            ref var b = ref State.Buildings[idx];
            if (b.HasActiveQueue == 1) return; // single slot for now
            b.QueueUnitType = unitType;
            b.QueueRemainingMs = trainTimeMs;
            b.HasActiveQueue = 1;
            b.QueueTotalMs = trainTimeMs;
        }

    private void ProductionStep() {
            for (int i = 0; i < State.BuildingCount; i++) {
                ref var b = ref State.Buildings[i];
                if (b.HasActiveQueue == 0) continue;
                b.QueueRemainingMs -= SimConstants.MsPerTick;
                if (b.QueueRemainingMs <= 0) {
                    int spawnX = b.X + _rng.Range(-2000, 2000);
                    int spawnY = b.Y + _rng.Range(-2000, 2000);
                    SpawnUnit((short)b.QueueUnitType, b.FactionId, spawnX, spawnY, 0);
                    b.HasActiveQueue = 0;
                    b.QueueUnitType = -1;
            b.QueueTotalMs = 0;
                }
            }
        }

        private int FindBuildingIndex(int id) {
            for (int i = 0; i < State.BuildingCount; i++) if (State.Buildings[i].Id == id) return i; return -1;
        }

        private void QueueOrder(int unitId, QueuedOrder order) {
            if (!_orderQueues.TryGetValue(unitId, out var list)) {
                list = new List<QueuedOrder>(4);
                _orderQueues[unitId] = list;
            }
            list.Add(order);
        }

        private void CombatStep() {
            for (int i = 0; i < State.UnitCount; i++) {
                ref var u = ref State.Units[i];
                if (State.UnitTypes[u.TypeId].AttackDamage <= 0) continue;
                if (u.AttackCooldownMs > 0) { u.AttackCooldownMs -= SimConstants.MsPerTick; continue; }
                int range = State.UnitTypes[u.TypeId].AttackRange;
                int targetIdx = -1;
                if (u.AttackTargetId != 0) {
                    int idx = FindUnitIndex(u.AttackTargetId);
                    if (idx >= 0) {
                        ref var tu = ref State.Units[idx];
                        if (tu.FactionId != u.FactionId) {
                            long dx = tu.X - u.X; long dy = tu.Y - u.Y; long d2 = dx*dx + dy*dy;
                            if (d2 <= (long)range * range) targetIdx = idx; else {
                                // not in range; ensure we are moving toward it
                                if (u.HasMoveTarget == 0) { QueueOrder(u.Id, new QueuedOrder { Type = OrderType.Move, TargetX = tu.X, TargetY = tu.Y }); }
                            }
                        }
                    } else {
                        u.AttackTargetId = 0; // cleared
                    }
                }
                if (targetIdx < 0) {
                    // fallback auto acquire
                    long bestDist2 = long.MaxValue;
                    for (int j = 0; j < State.UnitCount; j++) {
                        if (i == j) continue; ref var t = ref State.Units[j]; if (t.FactionId == u.FactionId) continue;
                        long dx = t.X - u.X; long dy = t.Y - u.Y; long d2 = dx * dx + dy * dy; if (d2 <= (long)range * range && d2 < bestDist2) { bestDist2 = d2; targetIdx = j; }
                    }
                }
                if (targetIdx >= 0) {
                    ref var target = ref State.Units[targetIdx];
                    target.HP -= State.UnitTypes[u.TypeId].AttackDamage;
                    if (target.HP <= 0) {
                        RemoveUnitByIndex(targetIdx);
                    }
                    u.AttackCooldownMs = State.UnitTypes[u.TypeId].AttackCooldownMs;
                }
            }
        }

        private void GatherStep() {
            int gatherRange = 1400;
            int depositRange = 1600;
            for (int i=0;i<State.UnitCount;i++) {
                ref var u = ref State.Units[i];
                if (u.CurrentOrderType!=OrderType.Gather && u.ReturningWithCargo==0) continue;
                // Returning to deposit?
                if (u.ReturningWithCargo==1) {
                    int bestB=-1; long bestD2=long.MaxValue;
                    for (int bi=0; bi<State.BuildingCount; bi++) { ref var b = ref State.Buildings[bi]; if (b.FactionId!=u.FactionId) continue; long dx=b.X-u.X; long dy=b.Y-u.Y; long d2=dx*dx+dy*dy; if (d2<bestD2){bestD2=d2; bestB=bi;} }
                    if (bestB>=0) {
                        long dx = State.Buildings[bestB].X - u.X; long dy = State.Buildings[bestB].Y - u.Y; long d2 = dx*dx+dy*dy;
                        if (d2 <= (long)depositRange*depositRange) {
                            ref var fac = ref State.Factions[u.FactionId];
                            switch(u.CarryResourceType){case 0: fac.Food+=u.CarryAmount; break; case 1: fac.Wood+=u.CarryAmount; break; case 2: fac.Stone+=u.CarryAmount; break; case 3: fac.Metal+=u.CarryAmount; break;}
                            u.CarryAmount=0; u.ReturningWithCargo=0;
                            int rnIdx = FindResourceNodeIndex(u.CurrentOrderEntity);
                            if (rnIdx<0 || State.ResourceNodes[rnIdx].AmountRemaining<=0) { u.CurrentOrderType=OrderType.None; u.CurrentOrderEntity=0; }
                        } else if (u.HasMoveTarget==0) {
                            QueueOrder(u.Id, new QueuedOrder{ Type=OrderType.Move, TargetX=State.Buildings[bestB].X, TargetY=State.Buildings[bestB].Y });
                        }
                    } else { // no deposit
                        u.ReturningWithCargo=0; u.CarryAmount=0; u.CurrentOrderType=OrderType.None; u.CurrentOrderEntity=0;
                    }
                    continue;
                }
                int nodeId = u.CurrentOrderEntity; if (nodeId==0) continue;
                int nIdx = FindResourceNodeIndex(nodeId); if (nIdx<0){ u.CurrentOrderType=OrderType.None; u.CurrentOrderEntity=0; continue; }
                ref var node = ref State.ResourceNodes[nIdx];
                if (node.AmountRemaining<=0) { u.CurrentOrderType=OrderType.None; u.CurrentOrderEntity=0; continue; }
                long ndx = node.X-u.X; long ndy = node.Y-u.Y; long nd2 = ndx*ndx+ndy*ndy;
                if (nd2 > (long)gatherRange*gatherRange) {
                    if (u.HasMoveTarget==0) QueueOrder(u.Id, new QueuedOrder{ Type=OrderType.Move, TargetX=node.X, TargetY=node.Y });
                    continue;
                }
                var utd = State.UnitTypes[u.TypeId]; if ((utd.Flags & 1)==0) { u.CurrentOrderType=OrderType.None; continue; }
                // Accumulate progress
                u.GatherProgressMs += SimConstants.MsPerTick * utd.GatherRatePerSec; // treat as gatherRatePerSec resources per second
                while (u.GatherProgressMs >= 1000) {
                    if (node.AmountRemaining<=0) break;
                    u.GatherProgressMs -= 1000;
                    node.AmountRemaining--; u.CarryResourceType=(byte)node.ResourceType; u.CarryAmount++;
                    if (u.CarryAmount >= utd.CarryCapacity) { u.ReturningWithCargo=1; u.HasMoveTarget=0; break; }
                }
                if (node.AmountRemaining<=0 && u.CarryAmount>0) { u.ReturningWithCargo=1; u.HasMoveTarget=0; }
            }
        }

        private void AutoAssignIdleWorkers() {
            // Simple heuristic: if worker has no order and not carrying, assign nearest resource node with remaining >0
            for (int i=0;i<State.UnitCount;i++) {
                ref var u = ref State.Units[i];
                if (u.CurrentOrderType!=OrderType.None || u.ReturningWithCargo==1) continue;
                var utd = State.UnitTypes[u.TypeId]; if ((utd.Flags & 1)==0) continue; // not a worker
                // Find closest node
                int best=-1; long bestD2=long.MaxValue;
                for (int rn=0; rn<State.ResourceNodeCount; rn++) {
                    ref var node = ref State.ResourceNodes[rn]; if (node.AmountRemaining<=0) continue; long dx=node.X-u.X; long dy=node.Y-u.Y; long d2=dx*dx+dy*dy; if (d2<bestD2){bestD2=d2; best=rn;} }
                if (best>=0) {
                    u.CurrentOrderType = OrderType.Gather; u.CurrentOrderEntity = State.ResourceNodes[best].Id;
                }
            }
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
            return id;
        }

        private int FindResourceNodeIndex(int id) { for (int i=0;i<State.ResourceNodeCount;i++) if (State.ResourceNodes[i].Id==id) return i; return -1; }

        private void RemoveUnitByIndex(int idx) {
            int id = State.Units[idx].Id;
            _paths.Remove(id);
            _orderQueues.Remove(id);
            _unitIndex.Remove(id);
            State.UnitCount--; if (idx != State.UnitCount) { State.Units[idx] = State.Units[State.UnitCount]; _unitIndex[State.Units[idx].Id] = idx; }
        }

        // Public helper APIs for gameplay layer
        public void IssueAttackCommand(int unitId, int targetUnitId) { _cmdQueue.Enqueue(new Command { IssueTick = State.Tick, Type = CommandType.Attack, EntityId = unitId, TargetX = targetUnitId }); }
        public void IssueGatherCommand(int unitId, int resourceNodeId) { _cmdQueue.Enqueue(new Command { IssueTick = State.Tick, Type = CommandType.Gather, EntityId = unitId, TargetX = resourceNodeId }); }
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
            for (int i=0;i<State.UnitCount;i++) { ref var u = ref State.Units[i]; int ux = u.X/TileSize; int uy = u.Y/TileSize; for (int dx=-radiusTiles; dx<=radiusTiles; dx++) for (int dy=-radiusTiles; dy<=radiusTiles; dy++) { int gx=ux+dx; int gy=uy+dy; if (gx<0||gy<0||gx>=_mapWidth||gy>=_mapHeight) continue; if (dx*dx+dy*dy <= radiusTiles*radiusTiles) vis[gx,gy]=1; } }
            // Track dirties comparing with previous
            _visionDirty.Clear();
            for (int x=0;x<_mapWidth;x++) for (int y=0;y<_mapHeight;y++) { if (vis[x,y] != _prevVisibility[x,y]) { _visionDirty.Add((x,y)); _prevVisibility[x,y] = vis[x,y]; } }
        }
        public IReadOnlyList<(int x,int y)> GetVisionDirty() => _visionDirty;

        // Replay skeleton
    private List<Command> _recorded = new List<Command>(4096);
    private bool _recording;
    private bool _playback;
    private int _playbackIndex;
    private int _playbackStartTick;
    private int _recordStartTick;
    private Snapshot _baselineSnapshot; // stored at StartRecording for scrub resets
    public void StartRecording() { _recorded.Clear(); _recordStartTick = State.Tick; _baselineSnapshot = SnapshotUtil.Capture(State); _recording = true; _playback = false; }
    public List<Command> StopRecording() { _recording = false; return new List<Command>(_recorded); }
    public void StartPlayback(List<Command> cmds) { _recorded = cmds ?? new List<Command>(); _playback = true; _recording = false; _playbackIndex = 0; _playbackStartTick = State.Tick; }
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
        }

        // Public gameplay APIs that record
        public void IssueMoveCommand(int unitId, int targetX, int targetY) {
            var cmd = new Command { IssueTick = State.Tick, Type = CommandType.Move, EntityId = unitId, TargetX = targetX, TargetY = targetY };
            RecordCommand(cmd); _cmdQueue.Enqueue(cmd);
        }
        public void IssueAttackCommand(int unitId, int targetUnitId) { var cmd = new Command { IssueTick = State.Tick, Type = CommandType.Attack, EntityId = unitId, TargetX = targetUnitId }; RecordCommand(cmd); _cmdQueue.Enqueue(cmd); }
        public void IssueGatherCommand(int unitId, int resourceNodeId) { var cmd = new Command { IssueTick = State.Tick, Type = CommandType.Gather, EntityId = unitId, TargetX = resourceNodeId }; RecordCommand(cmd); _cmdQueue.Enqueue(cmd); }
        // Replace Tick to call wrapper (already edited earlier but ensure wrapper used)
    }

    // Simple deterministic RNG (xorshift32)
    public struct DeterministicRng {
        private uint _state;
        public DeterministicRng(uint seed) { _state = seed == 0 ? 1u : seed; }
        public uint NextU32() { uint x = _state; x ^= x << 13; x ^= x >> 17; x ^= x << 5; _state = x; return x; }
        public int Range(int minInclusive, int maxExclusive) { return (int)(NextU32() % (uint)(maxExclusive - minInclusive)) + minInclusive; }
        public float NextFloat01() { return (NextU32() & 0xFFFFFF) / (float)0x1000000; }
    }

    // Snapshot DTOs for save/load (simplified JSON-friendly)
    [System.Serializable] public class Snapshot {
        public int tick;
        public UnitSnap[] units;
        public BuildingSnap[] buildings;
        public FactionSnap[] factions;
        public ResourceNodeSnap[] resourceNodes;
    }
    [System.Serializable] public class UnitSnap { public int id; public short type; public short faction; public int x; public int y; public int hp; public OrderType currentOrder; public int currentOrderEntity; public int[] orderTypes; public int[] orderEnts; public int[] orderXs; public int[] orderYs; public int[] pathX; public int[] pathY; }
    [System.Serializable] public class BuildingSnap { public int id; public short type; public short faction; public int x; public int y; public int hp; public int queueUnitType; public int queueRemaining; public int queueTotal; public byte hasQueue; public short fw; public short fh; }
    [System.Serializable] public class FactionSnap { public int food; public int wood; public int stone; public int metal; }
    [System.Serializable] public class ResourceNodeSnap { public int id; public short r; public int x; public int y; public int amt; }

    public static class SnapshotUtil {
        // Legacy capture without orders/paths (kept for compatibility)
        public static Snapshot Capture(WorldState ws) => CaptureInternal(ws, null, null);
        public static Snapshot Capture(Simulator sim) => CaptureInternal(sim.State, sim._orderQueues, sim._paths);

        private static Snapshot CaptureInternal(WorldState ws, Dictionary<int,List<QueuedOrder>> orderQueues, Dictionary<int,List<(int x,int y)>> paths) {
            var snap = new Snapshot {
                tick = ws.Tick,
                units = new UnitSnap[ws.UnitCount],
                buildings = new BuildingSnap[ws.BuildingCount],
                factions = new FactionSnap[ws.Factions.Length],
                resourceNodes = new ResourceNodeSnap[ws.ResourceNodeCount]
            };
            for (int i = 0; i < ws.UnitCount; i++) {
                ref var u = ref ws.Units[i];
                var us = new UnitSnap { id = u.Id, type = u.TypeId, faction = u.FactionId, x = u.X, y = u.Y, hp = u.HP, currentOrder = u.CurrentOrderType, currentOrderEntity = u.CurrentOrderEntity };
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
                snap.buildings[i] = new BuildingSnap { id = b.Id, type = b.TypeId, faction = b.FactionId, x = b.X, y = b.Y, hp = b.HP, queueUnitType = b.QueueUnitType, queueRemaining = b.QueueRemainingMs, queueTotal = b.QueueTotalMs, hasQueue = b.HasActiveQueue, fw = b.FootprintW, fh = b.FootprintH };
            }
            for (int f=0; f<ws.Factions.Length; f++) { ref var fac = ref ws.Factions[f]; snap.factions[f] = new FactionSnap { food=fac.Food, wood=fac.Wood, stone=fac.Stone, metal=fac.Metal }; }
            for (int r=0; r<ws.ResourceNodeCount; r++) { ref var rn = ref ws.ResourceNodes[r]; snap.resourceNodes[r] = new ResourceNodeSnap { id = rn.Id, r = rn.ResourceType, x = rn.X, y = rn.Y, amt = rn.AmountRemaining }; }
            return snap;
        }

        // Legacy apply (without orders). Prefer Apply(sim,...)
        public static void Apply(WorldState ws, Snapshot snap) => ApplyInternal(ws, null, null, snap);
        public static void Apply(Simulator sim, Snapshot snap) => ApplyInternal(sim.State, sim._orderQueues, sim._paths, snap);

        private static void ApplyInternal(WorldState ws, Dictionary<int,List<QueuedOrder>> orderQueues, Dictionary<int,List<(int x,int y)>> paths, Snapshot snap) {
            ws.Tick = snap.tick;
            ws.UnitCount = 0; ws.BuildingCount = 0; ws.ResourceNodeCount = 0;
            // Clear existing queues/paths
            orderQueues?.Clear(); paths?.Clear();
            if (snap.units != null) {
                if (ws.Units.Length < snap.units.Length) ws.Units = new Unit[snap.units.Length];
                foreach (var us in snap.units) {
                    ws.Units[ws.UnitCount++] = new Unit { Id = us.id, TypeId = us.type, FactionId = us.faction, X = us.x, Y = us.y, HP = us.hp, CurrentOrderType = us.currentOrder, CurrentOrderEntity = us.currentOrderEntity };
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
                }
            }
            if (snap.buildings != null) {
                if (ws.Buildings.Length < snap.buildings.Length) ws.Buildings = new Building[snap.buildings.Length];
                foreach (var bs in snap.buildings) {
                    ws.Buildings[ws.BuildingCount++] = new Building { Id = bs.id, TypeId = bs.type, FactionId = bs.faction, X = bs.x, Y = bs.y, HP = bs.hp, QueueUnitType = (short)bs.queueUnitType, QueueRemainingMs = bs.queueRemaining, HasActiveQueue = bs.hasQueue, QueueTotalMs = bs.queueTotal, FootprintW = bs.fw, FootprintH = bs.fh };
                }
            }
            if (snap.factions != null) {
                for (int f=0; f<ws.Factions.Length && f<snap.factions.Length; f++) { var fs = snap.factions[f]; ws.Factions[f].Food=fs.food; ws.Factions[f].Wood=fs.wood; ws.Factions[f].Stone=fs.stone; ws.Factions[f].Metal=fs.metal; }
            }
            if (snap.resourceNodes != null) {
                if (ws.ResourceNodes.Length < snap.resourceNodes.Length) ws.ResourceNodes = new ResourceNode[snap.resourceNodes.Length];
                foreach (var rn in snap.resourceNodes) { ws.ResourceNodes[ws.ResourceNodeCount++] = new ResourceNode { Id=rn.id, ResourceType=rn.r, X=rn.x, Y=rn.y, AmountRemaining=rn.amt }; }
            }
        }
    }
}
