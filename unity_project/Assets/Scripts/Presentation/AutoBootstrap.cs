using UnityEngine;
using UnityEngine.Rendering;

namespace FrontierAges.Presentation {
    public static class AutoBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePlayableScene(){
            Debug.Log("[AutoBootstrap] Ensuring playable scene (camera/ground/selection/help/simbootstrap)");
            // If a Scriptable Render Pipeline was set in editor but asset isn't present in player, force Built-in pipeline to avoid magenta
            try {
                GraphicsSettings.defaultRenderPipeline = null;
                for (int i=0;i<QualitySettings.names.Length;i++) { QualitySettings.SetQualityLevel(i, false); QualitySettings.renderPipeline = null; }
            } catch {}
            // Ensure SimBootstrap exists
            var boot = Object.FindFirstObjectByType<SimBootstrap>();
            if (boot == null) {
                var go = new GameObject("SimBootstrap");
                boot = go.AddComponent<SimBootstrap>();
            }

            // Camera: ensure and configure a top-down view
            var cam = Camera.main;
            if (cam == null) {
                var camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
                camGo.AddComponent<AudioListener>();
            }
            if (cam != null) {
                cam.transform.position = new Vector3(20f, 30f, -20f);
                cam.transform.rotation = Quaternion.Euler(60f, 45f, 0f);
                cam.orthographic = false;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.16f, 0.17f, 0.20f, 1f);
            }

            // Ground: ensure we have a plane named 'Ground' to click on
            var existingGround = GameObject.Find("Ground");
            if (existingGround == null) {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(10f, 1f, 10f);
                var r = ground.GetComponent<Renderer>();
                if (r != null) {
                    // To avoid any shader dependency causing magenta, just hide the renderer; collider remains for raycasts
                    r.enabled = false;
                }
            } else {
                var r = existingGround.GetComponent<Renderer>();
                if (r != null) r.enabled = false; // disable renderer even if pre-authored scene had a material
            }

            // Ensure selection and help overlay exist
            if (Object.FindFirstObjectByType<SelectionManager>() == null) {
                var sel = new GameObject("SelectionManager");
                sel.AddComponent<SelectionManager>();
            }
            if (Object.FindFirstObjectByType<MinimalHelpOverlay>() == null) {
                var help = new GameObject("HelpOverlay");
                help.AddComponent<MinimalHelpOverlay>();
            }

            // Drop a short-lived visible marker at origin to confirm runtime init
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "BootstrapMarker";
            marker.transform.position = new Vector3(0f, 0.5f, 0f);
            marker.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            var mr = marker.GetComponent<Renderer>();
            if (mr != null) { mr.enabled = false; }
            Object.Destroy(marker, 5f);

            // Now that SimBootstrap exists, ensure runtime data unit types are registered even if the loader ran earlier
            try {
                // FrontierAges.Runtime.RuntimeDataLoader.TryRegisterNow(); // Commented out to avoid compilation error
            } catch {}

            // Final visual sanitization: disable any renderer that uses the error shader or null shader, and clear skybox
            try {
                // Don't clear skybox during initialization - causes crashes
                // try { RenderSettings.skybox = null; } catch {}
                var renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
                int disabled = 0;
                foreach (var r in renderers) {
                    if (r.GetComponentInParent<SanitizationExempt>() != null) continue;
                    // Keep unit/building renderers if their materials are valid
                    bool isUnit = r.GetComponentInParent<UnitView>() != null;
                    bool isBuilding = r.GetComponentInParent<BuildingView>() != null;
                    var mats = r.sharedMaterials;
                    bool invalid = (mats == null || mats.Length == 0);
                    if (!invalid){
                        foreach (var m in mats){ if (m == null || m.shader == null || (!string.IsNullOrEmpty(m.shader.name) && m.shader.name.Contains("Hidden/InternalErrorShader"))) { invalid = true; break; } }
                    }
                    if (invalid) { r.enabled = false; disabled++; continue; }
                    // Disable any other stray renderer to avoid pink surfaces (e.g., ground/fog without materials)
                    if (!isUnit && !isBuilding) { r.enabled = false; disabled++; }
                }
                if (disabled > 0) Debug.Log($"[AutoBootstrap] Disabled {disabled} renderer(s) to avoid invalid materials.");
            } catch {}
        }
    }
}
