using UnityEngine;
using UnityEngine.Rendering;

namespace FrontierAges.Presentation {
    // Minimalist bootstrap to guarantee a clean visual baseline (no pink), used by the Basic Test scene.
    public class BasicTestBootstrap : MonoBehaviour {
        void Awake(){
            Debug.Log("[BasicTestBootstrap] Minimal mode: disabling all renderers and forcing solid background.");
            try {
                // Force built-in pipeline and remove skybox/post
                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;
                // Don't clear skybox during early initialization - can cause crashes
                // try { RenderSettings.skybox = null; } catch {}

                var cam = Camera.main;
                if (cam == null) { var go = new GameObject("Main Camera"); cam = go.AddComponent<Camera>(); go.tag = "MainCamera"; }
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.17f, 0.20f, 1f);
                // Cull everything; OnGUI overlays will still render
                cam.cullingMask = 0;

                // Disable any existing renderers to avoid error-shader draws
                int disabled = 0;
                var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                foreach (var r in renderers) { if (r.enabled) { r.enabled = false; disabled++; } }
                if (disabled > 0) Debug.Log($"[BasicTestBootstrap] Disabled {disabled} renderer(s).");

                // Remove FogOverlay if present
                foreach (var fog in Object.FindObjectsByType<FogOverlay>(FindObjectsSortMode.None)) { fog.enabled = false; if (fog.TryGetComponent<Renderer>(out var rr)) rr.enabled = false; }
            } catch { }
        }
    }
}
