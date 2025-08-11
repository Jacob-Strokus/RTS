// Deterministic Behavior Tree core
using System.Collections.Generic;
namespace FrontierAges.Sim.AI {
    public enum BTStatus { Running, Success, Failure }
    public abstract class BTNode { public abstract BTStatus Tick(AIContext ctx); }
    public class AIContext { public Simulator Sim; public int FactionId; public DeterministicRng Rng; public AIContext(Simulator sim,int faction,uint seed){ Sim=sim; FactionId=faction; Rng=new DeterministicRng(seed); } }
    public abstract class Composite : BTNode { protected List<BTNode> children = new List<BTNode>(); public Composite Add(BTNode c){ children.Add(c); return this; } }
    public class Sequence : Composite { private int _index; public override BTStatus Tick(AIContext ctx){ for(;;){ if(_index>=children.Count){ _index=0; return BTStatus.Success; } var st=children[_index].Tick(ctx); if(st==BTStatus.Success){ _index++; continue; } if(st==BTStatus.Failure){ _index=0; return BTStatus.Failure; } return BTStatus.Running; } } }
    public class Selector : Composite { private int _index; public override BTStatus Tick(AIContext ctx){ for(;;){ if(_index>=children.Count){ _index=0; return BTStatus.Failure; } var st=children[_index].Tick(ctx); if(st==BTStatus.Success){ _index=0; return BTStatus.Success; } if(st==BTStatus.Failure){ _index++; continue; } return BTStatus.Running; } } }
    public abstract class Decorator : BTNode { protected BTNode child; public Decorator(BTNode c){ child=c; } }
    public class Repeat : Decorator { public Repeat(BTNode c):base(c){} public override BTStatus Tick(AIContext ctx){ child.Tick(ctx); return BTStatus.Running; } }
    public class Cooldown : Decorator { private int _coolTicks; private int _remain; public Cooldown(BTNode c,int ticks):base(c){ _coolTicks=ticks; } public override BTStatus Tick(AIContext ctx){ if(_remain>0){ _remain--; return BTStatus.Running; } var st=child.Tick(ctx); if(st!=BTStatus.Running) _remain=_coolTicks; return st; } }
    // Leaf nodes
    public class FindEnemyAndAttack : BTNode { private int _range; public FindEnemyAndAttack(int range){ _range=range; } public override BTStatus Tick(AIContext ctx){ int best=-1; long bestD=long.MaxValue; var st=ctx.Sim.State; long r2=(long)_range*_range; for(int i=0;i<st.UnitCount;i++){ ref var u = ref st.Units[i]; if(u.FactionId==ctx.FactionId||u.HP<=0) continue; // enemy
                // pick first soldier we own to issue attack
                // find one friendly soldier
                int soldierIdx=-1; for(int j=0;j<st.UnitCount;j++){ ref var su = ref st.Units[j]; if(su.FactionId==ctx.FactionId && su.AttackTargetId==0){ soldierIdx=j; break; } }
                if(soldierIdx<0) return BTStatus.Failure; ref var sref = ref st.Units[soldierIdx]; long dx=u.X-sref.X; long dy=u.Y-sref.Y; long d2=dx*dx+dy*dy; if(d2<bestD && d2<=r2){ bestD=d2; best=i; }
                if(best>=0){ ctx.Sim.IssueAttackCommand(st.Units[soldierIdx].Id, st.Units[best].Id); return BTStatus.Success; }
            } return BTStatus.Failure; } }
    public class AssignIdleWorkerToResource : BTNode { public override BTStatus Tick(AIContext ctx){ var st=ctx.Sim.State; for(int i=0;i<st.UnitCount;i++){ ref var u = ref st.Units[i]; if(u.FactionId!=ctx.FactionId) continue; var utd=st.UnitTypes[u.TypeId]; if((utd.Flags & 1)==0) continue; if(u.CurrentOrderType==OrderType.None){ // find resource
                    int rn=-1; long bestD=long.MaxValue; for(int r=0;r<st.ResourceNodeCount;r++){ ref var node = ref st.ResourceNodes[r]; if(node.AmountRemaining<=0) continue; long dx=node.X-u.X; long dy=node.Y-u.Y; long d2=dx*dx+dy*dy; if(d2<bestD){bestD=d2; rn=r;} }
                    if(rn>=0){ ctx.Sim.IssueGatherCommand(u.Id, st.ResourceNodes[rn].Id); return BTStatus.Success; }
                } }
            return BTStatus.Failure; } }
    public class RootAI { public BTNode Tree; public AIContext Ctx; public RootAI(BTNode tree, AIContext ctx){ Tree=tree; Ctx=ctx; } public void Tick(){ Tree.Tick(Ctx); } }
}
