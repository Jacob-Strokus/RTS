using UnityEngine;

namespace FrontierAges.Presentation {
    public class WorldSpaceBillboard : MonoBehaviour {
        public Transform Target;
        public Vector3 Offset = new Vector3(0,1.6f,0);
        private Camera _cam;
        void Start(){ _cam = Camera.main; }
        void LateUpdate(){ if(!Target) { Destroy(gameObject); return; } if(!_cam) _cam = Camera.main; transform.position = Target.position + Offset; if(_cam) transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position); }
    }
}
