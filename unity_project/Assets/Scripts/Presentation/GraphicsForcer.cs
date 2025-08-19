using UnityEngine;
using UnityEngine.Rendering;

namespace FrontierAges.Presentation {
    // COMPLETELY DISABLED due to Unity crash bug
    public static class GraphicsForcer {
        // REMOVED: [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ForceBasicGraphics(){
            // DISABLED - This entire method causes Unity crashes during initialization
            Debug.Log("[GraphicsForcer] COMPLETELY DISABLED to prevent Unity crash");
        }
    }
}
