using UnityEngine;
using UnityEngine.Rendering;

namespace FrontierAges.Presentation {
    // Aggressively force basic graphics settings to eliminate any possible shader/rendering pipeline issues
    public static class GraphicsForcer {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ForceBasicGraphics(){
            Debug.Log("[GraphicsForcer] Forcing most basic graphics settings to eliminate magenta...");
            
            // Force lowest quality level (Very Low) which should use minimal rendering features
            QualitySettings.SetQualityLevel(0, true);
            
            // Ensure built-in pipeline
            GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            
            // Disable all advanced rendering features
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.shadowResolution = ShadowResolution.Low;
            QualitySettings.antiAliasing = 0;
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.Disable;
            QualitySettings.realtimeReflectionProbes = false;
            QualitySettings.softParticles = false;
            QualitySettings.softVegetation = false;
            QualitySettings.vSyncCount = 0;
            
            // DON'T clear skybox - this causes crashes during initialization
            // try { RenderSettings.skybox = null; } catch {}
            try { RenderSettings.fog = false; } catch {}
            
            Debug.Log($"[GraphicsForcer] Quality level: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            Debug.Log($"[GraphicsForcer] Render pipeline: {(GraphicsSettings.defaultRenderPipeline == null ? "Built-in" : GraphicsSettings.defaultRenderPipeline.name)}");
        }
    }
}
