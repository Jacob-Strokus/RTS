using UnityEngine;
using System.Collections;

namespace FrontierAges.Presentation {
    // Disables any renderer that would show as magenta (missing shader) and enforces a solid camera background.
    public static class VisualSanitizer {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Run() {
            try {
                // Enforce solid background and remove skybox references that may rely on missing shaders
                var cam = Camera.main;
                if (cam != null) { cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.16f,0.17f,0.20f,1f); }
                try { RenderSettings.skybox = null; } catch {}

                DisableInvalidRenderers("initial pass");

                // Queue a delayed second pass (some components set materials in Start)
                var runnerGo = new GameObject("VisualSanitizerRunner");
                Object.DontDestroyOnLoad(runnerGo);
                runnerGo.hideFlags = HideFlags.HideAndDontSave;
                runnerGo.AddComponent<VisualSanitizerRunner>();
            } catch { }
        }

        private static bool IsInvalid(Material m){
            if (m == null) return true;
            var sh = m.shader; if (sh == null) return true;
            var name = sh.name; if (!string.IsNullOrEmpty(name) && name.Contains("Hidden/InternalErrorShader")) return true;
            return false;
        }

        internal static void DisableInvalidRenderers(string phase){
            int disabled = 0;
            var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in renderers) {
                // Skip already disabled
                if (!r.enabled) continue;
                // Respect SanitizationExempt on self or parent
                if (r.GetComponentInParent<SanitizationExempt>() != null) continue;
                // Validate all shared materials
                var mats = r.sharedMaterials;
                bool anyInvalid = (mats == null || mats.Length == 0);
                if (!anyInvalid) {
                    for (int i=0;i<mats.Length;i++) { if (IsInvalid(mats[i])) { anyInvalid = true; break; } }
                }
                if (anyInvalid) {
                    r.enabled = false; disabled++;
                    continue;
                }
            }
            if (disabled > 0) Debug.Log($"[VisualSanitizer] Disabled {disabled} renderer(s) with invalid materials during {phase}.");
        }
    }

    internal class VisualSanitizerRunner : MonoBehaviour {
        private int _frames;
        private void LateUpdate(){
            _frames++;
            if (_frames == 1) FrontierAges.Presentation.VisualSanitizer.DisableInvalidRenderers("frame 1");
            if (_frames == 5) FrontierAges.Presentation.VisualSanitizer.DisableInvalidRenderers("frame 5");
            if (_frames >= 5) Destroy(gameObject);
        }
    }
}
