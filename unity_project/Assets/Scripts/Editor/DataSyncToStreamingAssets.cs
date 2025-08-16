#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FrontierAges.EditorTools {
    // Copies repo-level data/*.json into Assets/StreamingAssets/data for use in standalone builds.
    public class DataSyncToStreamingAssets : IPreprocessBuildWithReport {
        public int callbackOrder => 0;

        private static string RepoRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        private static string RepoData => Path.Combine(RepoRoot, "data");
        private static string SAData => Path.Combine(Application.dataPath, "StreamingAssets", "data");

        [MenuItem("FrontierAges/Data/Sync to StreamingAssets")] public static void SyncMenu(){ Sync(); }

        public void OnPreprocessBuild(BuildReport report) { Sync(); }

        private static void Sync(){
            try{
                if(!Directory.Exists(RepoData)) { Debug.LogWarning($"[DataSync] Source folder not found: {RepoData}"); return; }
                string saRoot = Path.GetDirectoryName(SAData);
                if(!Directory.Exists(saRoot)) Directory.CreateDirectory(saRoot!);
                if(Directory.Exists(SAData)) Directory.Delete(SAData, true);
                Directory.CreateDirectory(SAData);
                CopyAll(new DirectoryInfo(RepoData), new DirectoryInfo(SAData));
                AssetDatabase.Refresh();
                Debug.Log($"[DataSync] Copied data to StreamingAssets: {SAData}");
            } catch(System.Exception ex){ Debug.LogError($"[DataSync] Failed: {ex.Message}"); }
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target){
            foreach(var dir in source.GetDirectories()){
                // mirror structure
                var nextTarget = target.CreateSubdirectory(dir.Name);
                CopyAll(dir, nextTarget);
            }
            foreach(var file in source.GetFiles()){
                // Only copy JSON and map assets typically, but we can copy all to be safe
                string dest = Path.Combine(target.FullName, file.Name);
                file.CopyTo(dest, true);
            }
        }
    }
}
#endif
