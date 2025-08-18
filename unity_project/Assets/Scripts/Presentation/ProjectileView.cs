using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class ProjectileView : MonoBehaviour {
        public int EntityId; // id
        public int TargetUnitId;
        public void Init(int id, int targetId){ EntityId=id; TargetUnitId=targetId; }
        private void OnDrawGizmos(){ Gizmos.color = Color.yellow; Gizmos.DrawSphere(transform.position, 0.1f); }
    }
}
