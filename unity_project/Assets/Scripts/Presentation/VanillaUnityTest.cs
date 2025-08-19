using UnityEngine;

namespace FrontierAges.Presentation {
    // Ultra-simple Unity test with NO graphics modifications at all
    public class VanillaUnityTest : MonoBehaviour {
        void Start() {
            Debug.Log("=== VANILLA UNITY TEST (NO MODIFICATIONS) ===");
            Debug.Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
            Debug.Log($"Current Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            
            // Create simple cube with Unity's default material - NO shader modifications
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = Vector3.zero;
            
            // Use whatever material Unity gives us by default
            var renderer = cube.GetComponent<Renderer>();
            Debug.Log($"Default material: {renderer.material?.name ?? "NULL"}");
            Debug.Log($"Default shader: {renderer.material?.shader?.name ?? "NULL"}");
            
            // Just change the color, don't touch the shader
            if (renderer.material != null) {
                renderer.material.color = Color.green;
            }
            
            // Simple camera setup
            var cam = Camera.main;
            if (cam == null) {
                var camGO = new GameObject("VanillaCamera");
                cam = camGO.AddComponent<Camera>();
                camGO.tag = "MainCamera";
                camGO.AddComponent<AudioListener>();
            }
            cam.transform.position = new Vector3(0, 0, -3);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            
            Debug.Log("=== VANILLA TEST COMPLETE ===");
        }
        
        void OnGUI() {
            GUI.Label(new Rect(10, 10, 400, 20), "=== VANILLA UNITY TEST ===");
            GUI.Label(new Rect(10, 30, 400, 20), "NO graphics modifications applied");
            GUI.Label(new Rect(10, 50, 400, 20), "Should see: GREEN CUBE (if Unity works normally)");
            GUI.Label(new Rect(10, 70, 400, 20), "If MAGENTA: Base Unity rendering is broken");
            if (GUI.Button(new Rect(10, 100, 80, 30), "Quit")) {
                Application.Quit();
            }
        }
    }
}
