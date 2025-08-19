using UnityEngine;

namespace FrontierAges.Presentation {
    public static class AutoBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsurePlayableScene(){
            Debug.Log("[AutoBootstrap] Ensuring playable scene (camera/ground/selection/help/simbootstrap)");
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
                    try { var mat = r.material; if (mat != null) mat.color = new Color(0.22f, 0.24f, 0.26f, 1f); } catch {}
                }
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
            if (mr != null) {
                try { var m = mr.material; if (m != null) m.color = new Color(1f, 0f, 1f, 1f); } catch {}
            }
            Object.Destroy(marker, 5f);
        }
    }
}
