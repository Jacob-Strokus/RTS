// Partial Economy (gathering, auto-assign) module for Simulator
using System.Collections.Generic;
namespace FrontierAges.Sim {
    public partial class Simulator {
        private void GatherStep() {
            int gatherRange = 1400;
            int depositRange = 1600;
            for (int i=0;i<State.UnitCount;i++) {
                ref var u = ref State.Units[i];
                if (u.CurrentOrderType!=OrderType.Gather && u.ReturningWithCargo==0) continue;
                if (u.ReturningWithCargo==1) {
                    int bestB=-1; long bestD2=long.MaxValue; int rtype = u.CarryResourceType;
                    for (int bi=0; bi<State.BuildingCount; bi++) { ref var b = ref State.Buildings[bi]; if (b.FactionId!=u.FactionId) continue; // deposit rules from data: if building declares acceptsDeposit and doesn't include our type, skip
                        bool accepts = true; if(b.TypeId>=0 && b.TypeId < FrontierAges.Sim.DataRegistry.Buildings.Length){ var bj = FrontierAges.Sim.DataRegistry.Buildings[b.TypeId]; if(bj!=null && bj.acceptsDeposit!=null && bj.acceptsDeposit.Length>0){ accepts = false; var arr=bj.acceptsDeposit; for(int ai=0; ai<arr.Length; ai++){ var s=arr[ai]; if((rtype==0 && s=="food")||(rtype==1 && s=="wood")||(rtype==2 && s=="stone")||(rtype==3 && s=="metal")){ accepts=true; break; } } } }
                        if(!accepts) continue;
                        long dx=b.X-u.X; long dy=b.Y-u.Y; long d2=dx*dx+dy*dy; if (d2<bestD2){bestD2=d2; bestB=bi;}
                    }
                    if (bestB>=0) {
                        long dx = State.Buildings[bestB].X - u.X; long dy = State.Buildings[bestB].Y - u.Y; long d2 = dx*dx+dy*dy;
                        if (d2 <= (long)depositRange*depositRange) {
                            ref var fac = ref State.Factions[u.FactionId];
                            int amount = u.CarryAmount; byte rtype = u.CarryResourceType;
                            switch(rtype){case 0: fac.Food+=amount; break; case 1: fac.Wood+=amount; break; case 2: fac.Stone+=amount; break; case 3: fac.Metal+=amount; break;}
                            if(amount>0){ _simEvents.Add(new SimEvent{ Type=SimEventType.ResourceCollected, Tick=State.Tick, A=u.Id, B=rtype, C=amount, D=u.FactionId }); }
                            u.CarryAmount=0; u.ReturningWithCargo=0;
                            int rnIdx = FindResourceNodeIndex(u.CurrentOrderEntity);
                            if (rnIdx<0 || State.ResourceNodes[rnIdx].AmountRemaining<=0) { u.CurrentOrderType=OrderType.None; u.CurrentOrderEntity=0; }
                        } else if (u.HasMoveTarget==0) {
                            QueueOrder(u.Id, new QueuedOrder{ Type=OrderType.Move, TargetX=State.Buildings[bestB].X, TargetY=State.Buildings[bestB].Y });
                        }
                    } else {
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
                int rate = 0;
                switch(node.ResourceType) {
                    case 0: rate = utd.GatherFoodPerSec>0?utd.GatherFoodPerSec:utd.GatherRatePerSec; break;
                    case 1: rate = utd.GatherWoodPerSec>0?utd.GatherWoodPerSec:utd.GatherRatePerSec; break;
                    case 2: rate = utd.GatherStonePerSec>0?utd.GatherStonePerSec:utd.GatherRatePerSec; break;
                    case 3: rate = utd.GatherMetalPerSec>0?utd.GatherMetalPerSec:utd.GatherRatePerSec; break;
                }
                if(rate<=0){ u.CurrentOrderType=OrderType.None; continue; }
                u.GatherProgressMs += SimConstants.MsPerTick * rate;
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
            for (int i=0;i<State.UnitCount;i++) {
                ref var u = ref State.Units[i];
                if (u.CurrentOrderType!=OrderType.None || u.ReturningWithCargo==1) continue;
                var utd = State.UnitTypes[u.TypeId]; if ((utd.Flags & 1)==0) continue;
                int best=-1; long bestD2=long.MaxValue;
                for (int rn=0; rn<State.ResourceNodeCount; rn++) { ref var node = ref State.ResourceNodes[rn]; if (node.AmountRemaining<=0) continue; long dx=node.X-u.X; long dy=node.Y-u.Y; long d2=dx*dx+dy*dy; if (d2<bestD2){bestD2=d2; best=rn;} }
                if (best>=0) { u.CurrentOrderType = OrderType.Gather; u.CurrentOrderEntity = State.ResourceNodes[best].Id; }
            }
        }
    }
}
