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
            
            // Check if RTSInputManager is present for different help text
            var rtsInputManager = FindObjectOfType<RTSInputManager>();
            bool hasRTSInput = rtsInputManager != null;
            
            // Debug logging only once per second to avoid spam
            if (Application.isPlaying && Time.frameCount % 60 == 0) {
                Debug.Log($"[MinimalHelpOverlay] Frame {Time.frameCount}: RTSInputManager found: {hasRTSInput}");
                if (rtsInputManager != null) {
                    Debug.Log($"[MinimalHelpOverlay] RTSInputManager object name: {rtsInputManager.gameObject.name}");
                }
            }
            
            string help;
            if (hasRTSInput) {
                // Age of Empires 4 style controls
                help = "=== AGE OF EMPIRES 4 CONTROLS ===\n" +
                       "F1: Toggle Help\n\n" +
                       "SELECTION:\n" +
                       "LMB: Select  |  Double: Select all type\n" +
                       "Drag: Box select  |  Ctrl+A: All units\n" +
                       "ESC: Deselect  |  Del: Delete selected\n\n" +
                       "CONTROL GROUPS:\n" +
                       "0-9: Select group  |  Ctrl+0-9: Set group\n\n" +
                       "BUILDING:\n" +
                       "B: Build mode  |  Period/Comma: Cycle type\n" +
                       "T: Train unit  |  Y: Multi-train\n" +
                       "P: Rally point  |  O: Clear rally\n\n" +
                       "CAMERA:\n" +
                       "[/]: Rotate  |  Backspace: Reset\n" +
                       "F5: Focus selection  |  Arrows: Pan\n\n" +
                       "UNITS:\n" +
                       "F1-F4: Select building types\n" +
                       "H: Town centers  |  Period: Idle workers";
            } else {
                // Legacy controls
                help = "F1: Toggle Help\n"+
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
            }
            
            try {
                GUI.Label(new Rect(pad, pad, 520, 380), help ?? string.Empty, _mono ?? GUIStyle.none);
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
