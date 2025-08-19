using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FrontierAges.Presentation {
    public class RendererDebugOverlay : MonoBehaviour {
        private bool _show;
        private List<string> _lines = new List<string>();
        private float _lastUpdate;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Inject(){ var go = new GameObject("RendererDebugOverlay"); Object.DontDestroyOnLoad(go); go.AddComponent<RendererDebugOverlay>(); }

        void Update(){
            if (Input.GetKeyDown(KeyCode.F2)) { _show = !_show; Refresh(); }
            if (Input.GetKeyDown(KeyCode.F3)) { DisableAll("F3 hotkey"); Refresh(); }
            if (_show && Time.time - _lastUpdate > 2f) Refresh();
        }

        private void Refresh(){
            _lastUpdate = Time.time; _lines.Clear();
            var rends = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Where(r=>r.enabled).ToArray();
            _lines.Add($"Renderers enabled: {rends.Length}");
            foreach (var r in rends) {
                string sh = "(null)";
                var mats = r.sharedMaterials;
                if (mats!=null && mats.Length>0 && mats[0]!=null && mats[0].shader!=null) sh = mats[0].shader.name;
                _lines.Add($"- {r.GetType().Name} '{r.gameObject.name}' shader='{sh}'");
            }
            Debug.Log(string.Join("\n", _lines));
        }

        private void DisableAll(string reason){
            int n=0; foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)) { if (r.enabled) { r.enabled=false; n++; } }
            var cam = Camera.main; if (cam) { cam.cullingMask = 0; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f,0.17f,0.20f,1f); }
            Debug.Log($"[RendererDebugOverlay] Disabled {n} renderers ({reason}).");
        }

        void OnGUI(){ if(!_show) return; var y=20f; foreach(var l in _lines){ GUI.Label(new Rect(20,y,1400,20), l); y+=18f; } GUI.Label(new Rect(20,y+5,600,20), "F2: toggle overlay  |  F3: disable all renderers"); }
    }
}
