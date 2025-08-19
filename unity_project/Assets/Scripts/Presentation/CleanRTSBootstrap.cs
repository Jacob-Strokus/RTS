using UnityEngine;

namespace FrontierAges.Clean {
    // Ultra-minimal RTS foundation with guaranteed safe rendering
    public class CleanRTSBootstrap : MonoBehaviour {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ForceCleanPipeline(){
            // Aggressively clear any render pipeline that might cause pink
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            // Don't clear skybox during early initialization - can cause crashes
            // try { RenderSettings.skybox = null; } catch {}
        }

        void Awake(){
            Debug.Log("[CleanRTS] Starting with guaranteed clean visuals...");
            
            // Force camera to solid background, cull nothing initially
            var cam = Camera.main;
            if (cam == null) { var go = new GameObject("Camera"); cam = go.AddComponent<Camera>(); go.tag = "MainCamera"; go.AddComponent<AudioListener>(); }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f, 1f);
            cam.transform.position = new Vector3(0, 20, -15);
            cam.transform.rotation = Quaternion.Euler(45, 0, 0);
            
            // Create safe green ground using only Unity primitives + built-in materials
            CreateSafeGround();
            
            // Add minimal UI overlay
            gameObject.AddComponent<CleanRTSUI>();
        }

        private void CreateSafeGround(){
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "SafeGround";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(10, 1, 10);
            
            // Use Unity's built-in unlit shader (always available)
            var renderer = ground.GetComponent<Renderer>();
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = new Color(0.2f, 0.6f, 0.2f, 1f); // Green
            renderer.material = mat;
            
            Debug.Log("[CleanRTS] Created safe ground with Unlit/Color shader");
        }
    }

    // Minimal UI to show the system is working
    public class CleanRTSUI : MonoBehaviour {
        void OnGUI(){
            GUI.Box(new Rect(10, 10, 300, 100), "");
            GUI.Label(new Rect(20, 25, 280, 20), "Clean RTS Foundation - Working!");
            GUI.Label(new Rect(20, 45, 280, 20), "Green ground should be visible");
            GUI.Label(new Rect(20, 65, 280, 20), "No magenta = success");
            
            if (GUI.Button(new Rect(20, 85, 100, 20), "Test Click")){
                Debug.Log("[CleanRTS] UI test button clicked");
            }
        }
    }
}
