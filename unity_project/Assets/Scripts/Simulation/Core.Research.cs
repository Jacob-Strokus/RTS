// Partial Research module for Simulator
namespace FrontierAges.Sim {
    public partial class Simulator {
        private void ResearchStep(){
            for(int f=0; f<8; f++){
                for(int s=0;s<MaxConcurrentResearch;s++){
                    short tid = State.FactionResearchTechId[f,s]; if(tid<0) continue;
                    State.FactionResearchRemainingMs[f,s]-=SimConstants.MsPerTick;
                    if(State.FactionResearchRemainingMs[f,s]<=0){
                        State.FactionTechFlags[f] |= (1<<tid);
                        ApplyTechEffects(f, tid);
                        _simEvents.Add(new SimEvent{ Type=SimEventType.ResearchComplete, Tick=State.Tick, A=f, B=tid });
                        State.FactionResearchTechId[f,s]=-1; State.FactionResearchRemainingMs[f,s]=0; State.FactionResearchTotalMs[f,s]=0;
                    }
                }
            }
        }
        private void ApplyTechEffects(int factionId, short techIndex){ if(techIndex<0||techIndex>=DataRegistry.Techs.Length) return; var t = DataRegistry.Techs[techIndex]; if(t.effects==null) return; foreach(var ef in t.effects){ if(ef==null) continue; switch(ef.type){ case "unlockAge": if(ef.targetAge>State.FactionAges[factionId]) State.FactionAges[factionId]=ef.targetAge; break; case "gatherRateMul": { float mul = ef.mul!=0?ef.mul:1f; for(int ut=0; ut<State.UnitTypeCount; ut++){ var utd=State.UnitTypes[ut]; if((utd.Flags & 1)==0) continue; utd.GatherRatePerSec=(int)(utd.GatherRatePerSec*mul); if(utd.GatherFoodPerSec>0) utd.GatherFoodPerSec=(int)(utd.GatherFoodPerSec*mul); if(utd.GatherWoodPerSec>0) utd.GatherWoodPerSec=(int)(utd.GatherWoodPerSec*mul); if(utd.GatherStonePerSec>0) utd.GatherStonePerSec=(int)(utd.GatherStonePerSec*mul); if(utd.GatherMetalPerSec>0) utd.GatherMetalPerSec=(int)(utd.GatherMetalPerSec*mul); State.UnitTypes[ut]=utd; } break; } case "gatherRateAdd": { int add=(int)ef.add; for(int ut=0; ut<State.UnitTypeCount; ut++){ var utd=State.UnitTypes[ut]; if((utd.Flags & 1)==0) continue; utd.GatherRatePerSec+=add; if(utd.GatherFoodPerSec>0) utd.GatherFoodPerSec+=add; if(utd.GatherWoodPerSec>0) utd.GatherWoodPerSec+=add; if(utd.GatherStonePerSec>0) utd.GatherStonePerSec+=add; if(utd.GatherMetalPerSec>0) utd.GatherMetalPerSec+=add; State.UnitTypes[ut]=utd; } break; } case "armorAdd": for(int ut=0; ut<State.UnitTypeCount; ut++){ var utd=State.UnitTypes[ut]; utd.ArmorMelee+=(int)ef.add; utd.ArmorPierce+=(int)ef.add; State.UnitTypes[ut]=utd; } break; case "armorMul": { float m=ef.mul!=0?ef.mul:1f; for(int ut=0; ut<State.UnitTypeCount; ut++){ var utd=State.UnitTypes[ut]; utd.ArmorMelee=(int)(utd.ArmorMelee*m); utd.ArmorPierce=(int)(utd.ArmorPierce*m); State.UnitTypes[ut]=utd; } break; } case "attackMul": { float m=ef.mul!=0?ef.mul:1f; for(int ut=0; ut<State.UnitTypeCount; ut++){ var utd=State.UnitTypes[ut]; utd.AttackDamageBase=(int)(utd.AttackDamageBase*m); State.UnitTypes[ut]=utd; } break; } case "attackAdd": for(int ut=0; ut<State.UnitTypeCount; ut++){ var utd=State.UnitTypes[ut]; utd.AttackDamageBase+=(int)ef.add; State.UnitTypes[ut]=utd; } break; } } }
    }
}
