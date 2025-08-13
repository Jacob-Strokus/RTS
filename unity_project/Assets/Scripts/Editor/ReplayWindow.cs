// Unity Editor Replay Window for Simulator timeline
// Provides record toggle, play/pause, tick slider, jump buttons, hash display, and event list filtering.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using FrontierAges.Sim;
using System.Linq;
using System.Collections.Generic;

public class ReplayWindow : EditorWindow
{
    private Simulator _sim;
    private bool _autoPlay;
    private int _targetTick;
    private Vector2 _eventsScroll;
    private string _filter = "All";
    private bool _showDivergence;
    private readonly List<Simulator.SimEvent> _simEventsBuf = new List<Simulator.SimEvent>(1024);
    private readonly List<Simulator.DamageEvent> _dmgEventsBuf = new List<Simulator.DamageEvent>(1024);
    private double _lastEditorTime;

    [MenuItem("Sim/Replay Window")]
    public static void Open(){ var w = GetWindow<ReplayWindow>("Replay"); w.Show(); }

    private void OnEnable(){ _lastEditorTime = EditorApplication.timeSinceStartup; EditorApplication.update += OnEditorUpdate; }
    private void OnDisable(){ EditorApplication.update -= OnEditorUpdate; }

    private void OnEditorUpdate(){ if(_autoPlay && _sim!=null){ double now = EditorApplication.timeSinceStartup; if(now - _lastEditorTime > 0.05){ StepTick(); _lastEditorTime = now; Repaint(); } } }

    private void EnsureSim(){ if(_sim!=null) return; _sim = new Simulator(new CommandQueue()); _sim.EnableReplayRecording(50); SeedDemo(); }

    private void SeedDemo(){ // Simple demo so window works in empty scenes
        for(int f=0; f<2; f++){ _sim.State.Factions[f].PopCap = 50; }
        short soldier = _sim.RegisterUnitType(new UnitTypeData{ MoveSpeedMilliPerSec=2800, MaxHP=60, AttackDamageBase=6, AttackRange=1400, AttackCooldownMs=900, DTMelee=1f, PopCost=1 });
        for(int i=0;i<8;i++){ _sim.SpawnUnit(soldier, (short)(i%2), 2000+i*500, 2000, 0); }
        for(int t=0;t<100;t++) _sim.Tick();
    }

    private void OnGUI(){ EnsureSim(); if(_sim==null) return; var rep = _sim.Replay;
        EditorGUILayout.BeginHorizontal();
        if(GUILayout.Button(rep==null?"Enable Recording":"Disable Recording", GUILayout.Width(160))){ if(rep==null) _sim.EnableReplayRecording(50); else _sim.DisableReplayRecording(); }
        if(GUILayout.Button(_autoPlay?"Pause":"Play", GUILayout.Width(80))){ _autoPlay=!_autoPlay; }
        if(GUILayout.Button("<<", GUILayout.Width(40))){ Jump(-50); }
        if(GUILayout.Button("<", GUILayout.Width(40))){ Jump(-1); }
        if(GUILayout.Button(">", GUILayout.Width(40))){ Jump(+1); }
        if(GUILayout.Button(">>", GUILayout.Width(40))){ Jump(+50); }
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Tick: {_sim.State.Tick}");
        GUILayout.Label($"Hash: 0x{_sim.LastTickHash:X8}");
        EditorGUILayout.EndHorizontal();

        if(rep!=null){ int maxTick = rep.LastRecordedTick; int newTick = EditorGUILayout.IntSlider("Go to Tick", _targetTick, 0, Mathf.Max(maxTick,0)); if(newTick!=_targetTick){ _targetTick=newTick; _sim.TryLoadReplayTick(_targetTick); }
            // Divergence indicator
            var ts = (rep.Ticks.Count>0 && _targetTick<rep.Ticks[rep.Ticks.Count-1].Tick+1) ? rep.Ticks.FirstOrDefault(t=>t.Tick==_sim.State.Tick) : default;
            if(ts.Tick==_sim.State.Tick){ ulong cur = _sim.LastTickHash; string match = ((int)(cur & 0xFFFFFFFF))==ts.Hash?"OK":"MISMATCH"; var c = GUI.color; GUI.color = match=="OK"?Color.green:Color.red; GUILayout.Label($"Divergence: {match}"); GUI.color=c; }
            DrawFilters();
            DrawEventLists(rep);
            DrawExportImport(rep);
        } else {
            EditorGUILayout.HelpBox("Recording disabled. Enable to capture timeline.", MessageType.Info);
        }
    }

    private void DrawFilters(){ EditorGUILayout.BeginHorizontal(); GUILayout.Label("Filter:", GUILayout.Width(40)); string[] opts = new[]{"All","UnitSpawned","UnitDied","ResourceCollected","ResearchComplete","Damage"}; int idx = System.Array.IndexOf(opts, _filter); int newIdx = GUILayout.Toolbar(Mathf.Max(idx,0), opts); if(newIdx>=0 && newIdx<opts.Length) _filter = opts[newIdx]; EditorGUILayout.EndHorizontal(); }

    private void DrawEventLists(Simulator.ReplayTimeline rep){ if(rep==null) return; _eventsScroll = EditorGUILayout.BeginScrollView(_eventsScroll);
        int start = Mathf.Max(rep.Ticks.Count-500, 0); // limit rows
        for(int i=start;i<rep.Ticks.Count;i++){
            var ts = rep.Ticks[i];
            bool showHeader = true;
            // Sim events
            int seStart = ts.FirstSimEventIndex; int seCount = ts.SimEventCount;
            for(int j=0;j<seCount;j++){ var e = rep.SimEvents[seStart+j]; if(_filter!="All" && _filter!=e.Type.ToString()) continue; if(showHeader){ EditorGUILayout.LabelField($"Tick {ts.Tick} (hash {ts.Hash})"); showHeader=false; }
                EditorGUILayout.LabelField($"  {e.Type}: A={e.A} B={e.B} C={e.C} D={e.D}"); }
            // Damage events
            int dmStart = ts.FirstDamageIndex; int dmCount = ts.DamageCount;
            if(_filter=="All" || _filter=="Damage"){
                for(int j=0;j<dmCount;j++){ var d = rep.DamageEvents[dmStart+j]; if(showHeader){ EditorGUILayout.LabelField($"Tick {ts.Tick} (hash {ts.Hash})"); showHeader=false; }
                    EditorGUILayout.LabelField($"  Damage: attacker={d.AttackerUnitId} target={d.TargetUnitId} dmg={d.Damage} type={d.DType} kill={(d.WasKill!=0)}"); }
            }
        }
        EditorGUILayout.EndScrollView(); }

    private void DrawExportImport(Simulator.ReplayTimeline rep){ EditorGUILayout.Space(); EditorGUILayout.BeginHorizontal(); if(GUILayout.Button("Export JSON", GUILayout.Width(120))){ string p = EditorUtility.SaveFilePanel("Export Replay", Application.dataPath, "replay.json", "json"); if(!string.IsNullOrEmpty(p)){ var json = JsonUtility.ToJson(new ReplaySerializable(rep)); System.IO.File.WriteAllText(p, json); EditorUtility.RevealInFinder(p); } } if(GUILayout.Button("Import JSON", GUILayout.Width(120))){ string p = EditorUtility.OpenFilePanel("Import Replay", Application.dataPath, "json"); if(!string.IsNullOrEmpty(p)){ var json = System.IO.File.ReadAllText(p); var rs = JsonUtility.FromJson<ReplaySerializable>(json); var loaded = rs?.ToRuntime(); if(loaded!=null){ _sim.LoadReplay(loaded); _targetTick=loaded.LastRecordedTick; _sim.TryLoadReplayTick(_targetTick); } } } EditorGUILayout.EndHorizontal(); }

    [System.Serializable]
    private class ReplaySerializable{ public List<Simulator.DamageEvent> damage; public List<Simulator.SimEvent> events_; public List<Simulator.ReplayTimeline.TickSummary> ticks; public List<Simulator.Snapshot> snaps; public int lastTick; public int snapInterval;
        public ReplaySerializable(){}
        public ReplaySerializable(Simulator.ReplayTimeline rep){ damage = new List<Simulator.DamageEvent>(rep.DamageEvents); events_ = new List<Simulator.SimEvent>(rep.SimEvents); ticks = new List<Simulator.ReplayTimeline.TickSummary>(rep.Ticks); snaps = new List<Simulator.Snapshot>(rep.Snapshots); lastTick = rep.LastRecordedTick; snapInterval = rep.SnapshotInterval; }
        public Simulator.ReplayTimeline ToRuntime(){ var r = new Simulator.ReplayTimeline(); r.SnapshotInterval = snapInterval>0?snapInterval:50; r.Begin(0); r.DamageEvents.AddRange(damage??new List<Simulator.DamageEvent>()); r.SimEvents.AddRange(events_??new List<Simulator.SimEvent>()); r.Ticks.AddRange(ticks??new List<Simulator.ReplayTimeline.TickSummary>()); r.Snapshots.AddRange(snaps??new List<Simulator.Snapshot>()); r.LastRecordedTick = lastTick; return r; }
    }

    private void Jump(int delta){ int t = Mathf.Clamp(_sim.State.Tick + delta, 0, _sim.Replay!=null?_sim.Replay.LastRecordedTick:_sim.State.Tick); _sim.TryLoadReplayTick(t); _targetTick=t; }
    private void StepTick(){ _sim.Tick(); _targetTick=_sim.State.Tick; }
}
#endif
