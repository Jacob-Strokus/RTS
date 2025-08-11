// Simple editor test harness to validate 500-tick hash determinism.
// First run will log mismatch; copy printed hash into GoldenHash below to set baseline.
using UnityEditor;
using UnityEngine;
using FrontierAges.Sim;

public static class SimDeterminismTest
{
    private const ulong GoldenHash = 0x12345678ABCDEF01UL; // TODO: replace after first successful baseline run

    [MenuItem("Sim/Run Determinism 500-Tick Test")] public static void Run()
    {
        var sim = new Simulator(new CommandQueue());
        // Seed factions
        for(int f=0; f<2; f++){ sim.State.Factions[f].PopCap = 50; }
        // Register minimal unit archetypes
        short worker = sim.RegisterUnitType(new UnitTypeData{ MoveSpeedMilliPerSec=2500, MaxHP=50, GatherRatePerSec=2, CarryCapacity=10, Flags=1, PopCost=1 });
        short soldier = sim.RegisterUnitType(new UnitTypeData{ MoveSpeedMilliPerSec=3000, MaxHP=60, AttackDamageBase=8, AttackRange=1500, AttackCooldownMs=1000, DTMelee=1f, PopCost=1 });
        // Spawn units
        for(int i=0;i<5;i++){ sim.SpawnUnit(worker, 0, 1000+i*300, 1000, 0); }
        for(int i=0;i<5;i++){ sim.SpawnUnit(worker, 1, 8000+i*300, 8000, 0); }
        for(int i=0;i<3;i++){ sim.SpawnUnit(soldier, 0, 1200+i*200, 1200, 0); }
        for(int i=0;i<3;i++){ sim.SpawnUnit(soldier, 1, 7800+i*200, 7800, 0); }
        // Resource nodes
        sim.SpawnResourceNode(0, 4000, 4000, 500);
        sim.SpawnResourceNode(1, 4200, 4000, 500);
        // Commands
        var rsId = sim.State.ResourceNodes[0].Id;
        for(int i=0;i<5;i++) sim.IssueGatherCommand(sim.State.Units[i].Id, rsId);
        for(int i=0;i<3;i++) sim.IssueMoveCommand(sim.State.Units[5+i].Id, 5000, 5000);
        for(int i=0;i<3;i++) sim.IssueMoveCommand(sim.State.Units[8+i].Id, 5000, 5000);
        for(int t=0;t<500;t++) sim.Tick();
        ulong h = sim.LastTickHash;
        if(h != GoldenHash){
            Debug.LogError($"[Determinism] Hash mismatch. Got 0x{h:X16} vs expected 0x{GoldenHash:X16}. If change is intentional, update GoldenHash.");
        } else {
            Debug.Log($"[Determinism] PASS hash=0x{h:X16}");
        }
        var evBuf = new System.Collections.Generic.List<Simulator.SimEvent>();
        int evCount = sim.DrainSimEvents(evBuf);
        Debug.Log($"Generated {evCount} sim events (first 5 shown)");
        for(int i=0;i<evBuf.Count && i<5;i++){
            var e = evBuf[i];
            Debug.Log($"Event {i}: {e.Type} tick={e.Tick} A={e.A} B={e.B} C={e.C} D={e.D}");
        }
    }
}
