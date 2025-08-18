// Editor utilities for determinism workflow: capture and update golden hash
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text.RegularExpressions;
using FrontierAges.Sim;

public static class DeterminismTools
{
    [MenuItem("Sim/Capture Golden Hash (500 ticks)")]
    public static void CaptureGoldenHash()
    {
        var sim = new Simulator(new CommandQueue());
        // Match the scenario in SimDeterminismTest.Run
        for(int f=0; f<2; f++){ sim.State.Factions[f].PopCap = 50; }
        short worker = sim.RegisterUnitType(new UnitTypeData{ MoveSpeedMilliPerSec=2500, MaxHP=50, GatherRatePerSec=2, CarryCapacity=10, Flags=1, PopCost=1 });
        short soldier = sim.RegisterUnitType(new UnitTypeData{ MoveSpeedMilliPerSec=3000, MaxHP=60, AttackDamageBase=8, AttackRange=1500, AttackCooldownMs=1000, DTMelee=1f, PopCost=1 });
        for(int i=0;i<5;i++){ sim.SpawnUnit(worker, 0, 1000+i*300, 1000, 0); }
        for(int i=0;i<5;i++){ sim.SpawnUnit(worker, 1, 8000+i*300, 8000, 0); }
        for(int i=0;i<3;i++){ sim.SpawnUnit(soldier, 0, 1200+i*200, 1200, 0); }
        for(int i=0;i<3;i++){ sim.SpawnUnit(soldier, 1, 7800+i*200, 7800, 0); }
        sim.SpawnResourceNode(0, 4000, 4000, 500);
        sim.SpawnResourceNode(1, 4200, 4000, 500);
        var rsId = sim.State.ResourceNodes[0].Id;
        for(int i=0;i<5;i++) sim.IssueGatherCommand(sim.State.Units[i].Id, rsId);
        for(int i=0;i<3;i++) sim.IssueMoveCommand(sim.State.Units[5+i].Id, 5000, 5000);
        for(int i=0;i<3;i++) sim.IssueMoveCommand(sim.State.Units[8+i].Id, 5000, 5000);
        for(int t=0;t<500;t++) sim.Tick();
        ulong h = sim.LastTickHash;
        if(EditorUtility.DisplayDialog("Golden Hash", $"Computed 500-tick hash: 0x{h:X16}\nUpdate SimDeterminismTest GoldenHash?", "Update", "Cancel"))
        {
            UpdateGoldenHashConstant(h);
        }
        else
        {
            Debug.Log($"[Determinism] 500-tick hash: 0x{h:X16}");
        }
    }

    private static void UpdateGoldenHashConstant(ulong newHash)
    {
        // Path relative to project
        string path = "Assets/Scripts/Editor/SimDeterminismTest.cs";
        string fullPath = Path.Combine(Application.dataPath, "Scripts/Editor/SimDeterminismTest.cs");
        if(!File.Exists(fullPath)) { Debug.LogError($"File not found: {fullPath}"); return; }
        string text = File.ReadAllText(fullPath);
        string pattern = @"private const ulong GoldenHash = 0x[0-9A-Fa-f]+UL;";
        string replacement = $"private const ulong GoldenHash = 0x{newHash:X16}UL;";
        string newText = Regex.Replace(text, pattern, replacement);
        if(newText==text){ Debug.LogWarning("GoldenHash constant not found or unchanged."); }
        File.WriteAllText(fullPath, newText);
        AssetDatabase.Refresh();
        Debug.Log($"[Determinism] GoldenHash updated to 0x{newHash:X16}");
    }
}
#endif
