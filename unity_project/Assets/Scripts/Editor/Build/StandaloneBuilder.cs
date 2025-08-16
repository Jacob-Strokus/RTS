#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
                if (EditorUtility.DisplayDialog("No Scenes in Build Settings", "No scenes are set in Build Settings. Build a bootstrap-only player?", "Yes", "Cancel"))
                {
                    // Attempt to create a temporary scene list using current open scene
                    var current = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene().path;
                    if (!string.IsNullOrEmpty(current)) scenes = new[] { current };
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
