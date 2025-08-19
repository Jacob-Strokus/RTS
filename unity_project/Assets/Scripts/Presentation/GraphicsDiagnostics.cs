using UnityEngine;
using UnityEngine.Rendering;

namespace FrontierAges.Presentation {
    // Comprehensive graphics diagnostics to identify rendering issues
    public class GraphicsDiagnostics : MonoBehaviour {
        void Start() {
            Debug.Log("=== GRAPHICS DIAGNOSTICS START ===");
            LogSystemInfo();
            LogGraphicsSettings();
            LogShaderInfo();
            AttemptBasicRendering();
            Debug.Log("=== GRAPHICS DIAGNOSTICS END ===");
        }
        
        void LogSystemInfo() {
            Debug.Log("=== SYSTEM INFO ===");
            Debug.Log($"Unity Version: {Application.unityVersion}");
            Debug.Log($"Platform: {Application.platform}");
            Debug.Log($"Graphics Device: {SystemInfo.graphicsDeviceName}");
            Debug.Log($"Graphics Vendor: {SystemInfo.graphicsDeviceVendor}");
            Debug.Log($"Graphics API: {SystemInfo.graphicsDeviceType}");
            Debug.Log($"Graphics Driver: {SystemInfo.graphicsDeviceVersion}");
            Debug.Log($"Graphics Memory: {SystemInfo.graphicsMemorySize} MB");
            Debug.Log($"Graphics Multi-threaded: {SystemInfo.graphicsMultiThreaded}");
            Debug.Log($"Graphics Shader Level: {SystemInfo.graphicsShaderLevel}");
            Debug.Log($"Max Texture Size: {SystemInfo.maxTextureSize}");
            Debug.Log($"Supports Shadows: {SystemInfo.supportsShadows}");
            Debug.Log($"Supports Render Textures: {SystemInfo.supportsRenderTextures}");
            Debug.Log($"Supports Compute Shaders: {SystemInfo.supportsComputeShaders}");
        }
        
        void LogGraphicsSettings() {
            Debug.Log("=== GRAPHICS SETTINGS ===");
            Debug.Log($"Current Quality Level: {QualitySettings.GetQualityLevel()} ({QualitySettings.names[QualitySettings.GetQualityLevel()]})");
            Debug.Log($"Render Pipeline: {(GraphicsSettings.defaultRenderPipeline == null ? "Built-in" : GraphicsSettings.defaultRenderPipeline.name)}");
            Debug.Log($"Color Space: {QualitySettings.activeColorSpace}");
            Debug.Log($"V-Sync: {QualitySettings.vSyncCount}");
            Debug.Log($"Anti-Aliasing: {QualitySettings.antiAliasing}");
            Debug.Log($"Shadows: {QualitySettings.shadows}");
        }
        
        void LogShaderInfo() {
            Debug.Log("=== SHADER INFO ===");
            
            // Test Unity built-in shaders
            var shaders = new string[] {
                "Unlit/Color",
                "Unlit/Texture", 
                "Standard",
                "Legacy Shaders/Diffuse",
                "Sprites/Default",
                "UI/Default"
            };
            
            foreach (var shaderName in shaders) {
                var shader = Shader.Find(shaderName);
                if (shader != null) {
                    Debug.Log($"✓ Found shader: {shaderName} (supported: {shader.isSupported})");
                } else {
                    Debug.LogError($"✗ Missing shader: {shaderName}");
                }
            }
        }
        
        void AttemptBasicRendering() {
            Debug.Log("=== RENDERING TEST ===");
            
            try {
                // Create the most basic possible rendered object
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "DiagnosticCube";
                cube.transform.position = Vector3.zero;
                
                var renderer = cube.GetComponent<Renderer>();
                if (renderer == null) {
                    Debug.LogError("Cube has no renderer component!");
                    return;
                }
                
                // Try multiple shader approaches
                Material workingMaterial = null;
                
                // Try Unlit/Color (most basic)
                var unlitShader = Shader.Find("Unlit/Color");
                if (unlitShader != null && unlitShader.isSupported) {
                    workingMaterial = new Material(unlitShader);
                    workingMaterial.color = Color.green;
                    Debug.Log("✓ Using Unlit/Color shader");
                } else {
                    Debug.LogWarning("Unlit/Color shader not available or unsupported");
                }
                
                // Fallback to Standard
                if (workingMaterial == null) {
                    var standardShader = Shader.Find("Standard");
                    if (standardShader != null && standardShader.isSupported) {
                        workingMaterial = new Material(standardShader);
                        workingMaterial.color = Color.green;
                        Debug.Log("✓ Using Standard shader as fallback");
                    } else {
                        Debug.LogWarning("Standard shader not available or unsupported");
                    }
                }
                
                // Last resort - use default material
                if (workingMaterial == null) {
                    workingMaterial = renderer.material;
                    if (workingMaterial != null) {
                        workingMaterial.color = Color.green;
                        Debug.Log("✓ Using default renderer material");
                    } else {
                        Debug.LogError("No material available at all!");
                    }
                }
                
                if (workingMaterial != null) {
                    renderer.material = workingMaterial;
                    Debug.Log($"✓ Material assigned: {workingMaterial.shader?.name ?? "Unknown"}");
                    
                    // Test material properties
                    if (workingMaterial.HasProperty("_Color")) {
                        workingMaterial.SetColor("_Color", Color.green);
                        Debug.Log("✓ Color property set to green");
                    } else {
                        Debug.LogWarning("Material has no _Color property");
                    }
                } else {
                    Debug.LogError("Failed to create any working material!");
                }
                
                // Set up camera if needed
                var cam = Camera.main;
                if (cam == null) {
                    var camGO = new GameObject("DiagnosticCamera");
                    cam = camGO.AddComponent<Camera>();
                    camGO.tag = "MainCamera";
                    camGO.AddComponent<AudioListener>();
                    Debug.Log("✓ Created diagnostic camera");
                }
                
                cam.transform.position = new Vector3(0, 0, -3);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.cullingMask = -1; // Render everything
                
                Debug.Log("✓ Basic rendering setup complete");
                
            } catch (System.Exception ex) {
                Debug.LogError($"Rendering test failed: {ex.Message}");
                Debug.LogError($"Stack trace: {ex.StackTrace}");
            }
        }
        
        void OnGUI() {
            var y = 10f;
            var lineHeight = 20f;
            
            GUI.Label(new Rect(10, y, 600, lineHeight), "=== GRAPHICS DIAGNOSTICS ===");
            y += lineHeight;
            
            GUI.Label(new Rect(10, y, 600, lineHeight), $"Graphics: {SystemInfo.graphicsDeviceName}");
            y += lineHeight;
            
            GUI.Label(new Rect(10, y, 600, lineHeight), $"API: {SystemInfo.graphicsDeviceType}");
            y += lineHeight;
            
            GUI.Label(new Rect(10, y, 600, lineHeight), $"Driver: {SystemInfo.graphicsDeviceVersion}");
            y += lineHeight;
            
            GUI.Label(new Rect(10, y, 600, lineHeight), $"Shader Level: {SystemInfo.graphicsShaderLevel}");
            y += lineHeight;
            
            GUI.Label(new Rect(10, y, 600, lineHeight), $"Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            y += lineHeight;
            
            y += 10;
            GUI.Label(new Rect(10, y, 600, lineHeight), "Expected: GREEN CUBE on BLACK background");
            y += lineHeight;
            
            GUI.Label(new Rect(10, y, 600, lineHeight), "If MAGENTA: Graphics driver/compatibility issue");
            y += lineHeight;
            
            y += 10;
            if (GUI.Button(new Rect(10, y, 120, 30), "Force Very Low")) {
                QualitySettings.SetQualityLevel(0, true);
                Debug.Log("Forced Very Low quality");
            }
            
            if (GUI.Button(new Rect(140, y, 120, 30), "Force Built-in")) {
                GraphicsSettings.defaultRenderPipeline = null;
                QualitySettings.renderPipeline = null;
                Debug.Log("Forced Built-in render pipeline");
            }
            
            if (GUI.Button(new Rect(270, y, 80, 30), "Quit")) {
                Application.Quit();
            }
        }
    }
}
