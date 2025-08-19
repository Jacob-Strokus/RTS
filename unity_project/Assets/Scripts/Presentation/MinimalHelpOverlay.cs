using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    // Lightweight, zero-setup overlay for help + key sim stats. Safe for auto-injected bootstrap.
    public class MinimalHelpOverlay : MonoBehaviour {
        private Simulator _sim;
        private bool _visible = true; // toggle with F1
    private GUIStyle _mono;
    private GUIStyle _monoSmall;

        void Start(){ var boot = FindFirstObjectByType<SimBootstrap>(); _sim = boot?.GetSimulator(); }

        private void EnsureStyles(){
            if (_mono != null && _monoSmall != null) return;
            try {
                // Be extremely defensive: GUI.skin can be null early/late in player lifecycle.
                var baseLabel = (GUI.skin != null && GUI.skin.label != null) ? GUI.skin.label : new GUIStyle();
                _mono = new GUIStyle(baseLabel){ fontSize = 14, richText = false, alignment = TextAnchor.UpperLeft };
                _monoSmall = new GUIStyle(baseLabel){ fontSize = 12, richText = false, alignment = TextAnchor.UpperLeft };
                if (_mono == null) _mono = GUIStyle.none; // ultimate fallback to avoid NRE
                if (_monoSmall == null) _monoSmall = GUIStyle.none;
            } catch {
                // If anything fails, fall back to safe styles and let OnGUI proceed.
                _mono ??= GUIStyle.none;
                _monoSmall ??= GUIStyle.none;
            }
        }

        void Update(){ if (Input.GetKeyDown(KeyCode.F1)) _visible = !_visible; }

        void OnGUI(){
            // Skip if GUI system isn't processing a valid event yet.
            if (!_visible) return;
            if (Event.current == null) return;
            EnsureStyles();
            float pad = 8f;
            // Help (left)
            string help =
                "F1: Toggle Help\n"+
                "RMB: Move selected\n"+
                "LMB: Select / Drag to box\n"+
                "B: Build mode, ,/. cycle type\n"+
                "T: Train unit (first bldg)\n"+
                "Y: Queue 3x train\n"+
                "P: Set rally at mouse, O: Clear\n"+
                "C/V: Cancel prod (head/tail)\n"+
                "G: Gather (workers)\n"+
                "H: Toggle auto-assign workers\n"+
                "A: Attack target  |  Shift+A: Attack-move\n"+
                "F5: Save snapshot  |  F9: Load snapshot\n"+
                "R: Start research (test)";
            try {
                GUI.Label(new Rect(pad, pad, 420, 280), help ?? string.Empty, _mono ?? GUIStyle.none);
            } catch { enabled = false; return; }

            // Stats (right)
            if(_sim!=null){
                var f = _sim.State.Factions[0];
                string stats = $"Tick: {_sim.State.Tick}\n"+
                               $"Hash: {_sim.LastTickHash:X16}\n"+
                               $"Avg μs: {_sim.State.AvgTickDurationMicro}\n\n"+
                               $"Food: {f.Food}  Wood: {f.Wood}\n"+
                               $"Stone: {f.Stone}  Metal: {f.Metal}\n"+
                               $"Pop: {f.Pop}/{f.PopCap}\n"+
                               $"Units: {_sim.State.UnitCount}  Buildings: {_sim.State.BuildingCount}";
                var w = 320f; var h = 160f; var x = Screen.width - w - pad; var y = pad;
                try {
                    GUI.Box(new Rect(x-4, y-4, w+8, h+8), GUIContent.none);
                    GUI.Label(new Rect(x, y, w, h), stats ?? string.Empty, _monoSmall ?? GUIStyle.none);
                } catch { enabled = false; return; }
            }
        }
    }
}
