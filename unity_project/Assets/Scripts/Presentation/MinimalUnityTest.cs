using UnityEngine;

namespace FrontierAges.Presentation {
    // Absolute minimal Unity test - just a green cube with diagnostic info
    public class MinimalUnityTest : MonoBehaviour {
        void Start() {
            // Add comprehensive graphics diagnostics
            gameObject.AddComponent<GraphicsDiagnostics>();
            Debug.Log("=== MINIMAL UNITY TEST START ===");
            Debug.Log($"Unity Version: {Application.unityVersion}");
            Debug.Log($"Platform: {Application.platform}");
            Debug.Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
            Debug.Log($"Graphics Driver: {SystemInfo.graphicsDeviceVersion}");
            
            // Force absolute basic graphics
            QualitySettings.SetQualityLevel(0, true); // Very Low
            UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = null;
            QualitySettings.renderPipeline = null;
            
            // Create green cube using most basic Unity components
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = Vector3.zero;
            
            // Try multiple shader approaches
            var renderer = cube.GetComponent<Renderer>();
            Material material = null;
            
            // Try Unlit/Color first (most basic)
            var unlitShader = Shader.Find("Unlit/Color");
            if (unlitShader != null) {
                material = new Material(unlitShader);
                material.color = Color.green;
                Debug.Log("SUCCESS: Using Unlit/Color shader");
            } else {
                // Fallback to Standard (should always exist)
                var standardShader = Shader.Find("Standard");
                if (standardShader != null) {
                    material = new Material(standardShader);
                    material.color = Color.green;
                    Debug.Log("FALLBACK: Using Standard shader");
                } else {
                    Debug.LogError("CRITICAL: No shaders found!");
                }
            }
            
            if (material != null) {
                renderer.material = material;
                Debug.Log($"Material assigned. Shader: {material.shader?.name ?? "NULL"}");
            }
            
            // Setup camera
            var cam = Camera.main;
            if (cam == null) {
                var camGO = new GameObject("MinimalCamera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0, 0, -3);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            
            Debug.Log("=== MINIMAL UNITY TEST COMPLETE ===");
        }
        
        void OnGUI() {
            GUI.Label(new Rect(10, 10, 400, 20), "=== MINIMAL UNITY GRAPHICS TEST ===");
            GUI.Label(new Rect(10, 30, 400, 20), $"Unity: {Application.unityVersion}");
            GUI.Label(new Rect(10, 50, 400, 20), $"Graphics: {SystemInfo.graphicsDeviceName}");
            GUI.Label(new Rect(10, 70, 400, 20), $"API: {SystemInfo.graphicsDeviceType}");
            GUI.Label(new Rect(10, 90, 400, 20), "Expected: GREEN CUBE on BLACK background");
            GUI.Label(new Rect(10, 110, 400, 20), "If you see MAGENTA, the issue is Unity/driver level");
            if (GUI.Button(new Rect(10, 140, 80, 30), "Quit")) {
                Application.Quit();
            }
        }
    }
}
