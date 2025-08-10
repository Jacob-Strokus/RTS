// Editor stub that will later read JSON from /data and convert to ScriptableObjects
// Placeholder only – real implementation will use UnityEditor APIs.
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FrontierAges.EditorTools {
    public static class JsonImporterStub {
        [MenuItem("FrontierAges/Validate Data (Stub)")] public static void Validate() {
            Debug.Log("Data validation stub – implement JSON parsing later.");
        }
    }
}
#endif
