#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace FrontierAges.EditorTools.Build {
    public static class StandaloneBuilder {
        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        private static string BuildRoot => Path.Combine(RepoRoot, "build");
        private static string BinRoot => Path.Combine(RepoRoot, "bin");

        [MenuItem("FrontierAges/Build/Build Windows (x64)")]
        public static void BuildWindows64(){
            PrepareFolders();
            // Ensure data synced
            try { DataSyncToStreamingAssetsInvoker.TrySync(); } catch { /* best effort */ }
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            string outDir = Path.Combine(BuildRoot, $"windows-x64-{stamp}");
            Directory.CreateDirectory(outDir);

            var scenes = FindEnabledScenes();
            if (scenes.Length == 0) {
                // Batch-mode safe fallback: try current scene, otherwise any scene under Assets
                var current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                if (!string.IsNullOrEmpty(current)) {
                    scenes = new[] { current };
                } else {
                    var anyScenes = AssetDatabase.FindAssets("t:Scene").Select(AssetDatabase.GUIDToAssetPath).Where(p => !string.IsNullOrEmpty(p)).ToArray();
                    if (anyScenes.Length > 0) scenes = new[] { anyScenes[0] };
                }
                // If still empty and we are not in batch mode, offer a dialog
                if (scenes.Length == 0 && !Application.isBatchMode) {
                    if (EditorUtility.DisplayDialog("No Scenes in Build Settings", "No scenes are set in Build Settings. Build a bootstrap-only player using the currently open scene?", "Yes", "Cancel"))
                    {
                        current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                        if (!string.IsNullOrEmpty(current)) scenes = new[] { current };
                    }
                }
                // If still empty, auto-generate a minimal bootstrap scene to allow building
                if (scenes.Length == 0) {
                    string bootstrapPath = EnsureBootstrapScene();
                    if (!string.IsNullOrEmpty(bootstrapPath)) {
                        scenes = new[] { bootstrapPath };
                        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(bootstrapPath, true) };
                        Debug.Log($"[Build] Created minimal bootstrap scene at {bootstrapPath}");
                    }
                }
            }
            var options = new BuildPlayerOptions {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, "FrontierAges.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded) {
                Debug.Log($"[Build] Success -> {outDir}");
                // Copy to bin as 'latest'
                string latest = Path.Combine(BinRoot, "windows-x64-latest");
                if (Directory.Exists(latest)) Directory.Delete(latest, true);
                CopyAll(new DirectoryInfo(outDir), new DirectoryInfo(latest));
                EditorUtility.RevealInFinder(outDir);
            } else {
                Debug.LogError($"[Build] Failed: {report.summary.result} errors={report.summary.totalErrors}");
            }
        }

        private static string[] FindEnabledScenes(){
            return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).Where(p => !string.IsNullOrEmpty(p)).ToArray();
        }

        private static void PrepareFolders(){ Directory.CreateDirectory(BuildRoot); Directory.CreateDirectory(BinRoot); }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target){
            Directory.CreateDirectory(target.FullName);
            foreach (var dir in source.GetDirectories()) CopyAll(dir, target.CreateSubdirectory(dir.Name));
            foreach (var file in source.GetFiles()) file.CopyTo(Path.Combine(target.FullName, file.Name), true);
        }

        // Create a minimal scene with camera/light and a SimBootstrap component, save it, return its path
        private static string EnsureBootstrapScene(){
            try {
                string scenesDir = "Assets/Scenes";
                if (!AssetDatabase.IsValidFolder(scenesDir)) {
                    AssetDatabase.CreateFolder("Assets", "Scenes");
                }
                string scenePath = Path.Combine(scenesDir, "Bootstrap.unity").Replace('\\','/');
                // Create default scene (includes camera and light)
                var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                // Add empty GO with SimBootstrap via reflection to avoid hard assembly ref issues
                var go = new GameObject("SimBootstrap");
                var type = System.Type.GetType("FrontierAges.Presentation.SimBootstrap, Gameplay.Presentation");
                if (type != null) go.AddComponent(type);
                // Save scene
                if (!EditorSceneManager.SaveScene(scene, scenePath)) {
                    Debug.LogWarning("[Build] Failed to save auto-generated Bootstrap scene.");
                    return null;
                }
                AssetDatabase.Refresh();
                return scenePath;
            } catch (System.Exception ex) {
                Debug.LogWarning($"[Build] Could not auto-create bootstrap scene: {ex.Message}");
                return null;
            }
        }
    }

    // Safe wrapper to call DataSync without hard reference order issues
    internal static class DataSyncToStreamingAssetsInvoker {
        public static void TrySync(){
            var t = Type.GetType("FrontierAges.EditorTools.DataSyncToStreamingAssets, Assembly-CSharp-Editor");
            var m = t?.GetMethod("SyncMenu", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            m?.Invoke(null, null);
        }
    }
}
#endif
