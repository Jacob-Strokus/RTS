using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class UnitView : MonoBehaviour {
        public int EntityId; // assigned at spawn
        private Simulator _sim;
        private int _lastKnownTick;

        private float _lastHp;
        private float _hitFlash;
        private Renderer _rend;
        public GameObject HealthBarPrefab; // assign optional prefab with child Image fill
        private UnityEngine.UI.Image _hpFill;

        public void Init(int entityId, Simulator sim) {
            EntityId = entityId; _sim = sim; _lastKnownTick = -1;
        }

        void Start() { _rend = GetComponentInChildren<Renderer>(); }

        void Update() {
            if (_sim == null) return;
            var state = _sim.State;
            if (state.Tick == _lastKnownTick) return;
            _lastKnownTick = state.Tick;
            int idx = FindUnitIndex(EntityId, state);
            if (idx < 0) return; // entity gone
            ref var u = ref state.Units[idx];
            transform.position = new Vector3(u.X / (float)SimConstants.PositionScale, 0, u.Y / (float)SimConstants.PositionScale);

            if (_sim != null) {
                var ws = _sim.State; int uIdx = FindUnitIndex(EntityId, ws); if (uIdx>=0) { ref var uu = ref ws.Units[uIdx];
                    if (_hpFill) _hpFill.fillAmount = Mathf.Clamp01(uu.HP / (float)ws.UnitTypes[uu.TypeId].MaxHP);
                    if (_lastHp > 0 && uu.HP < _lastHp) { _hitFlash = 0.25f; }
                    _lastHp = uu.HP;
                }
            }
            if (_hitFlash > 0 && _rend) { _hitFlash -= Time.deltaTime; float t = _hitFlash / 0.25f; _rend.material.color = Color.Lerp(Color.white, Color.red, t); }
        }

        private int FindUnitIndex(int id, WorldState ws) {
            for (int i = 0; i < ws.UnitCount; i++) if (ws.Units[i].Id == id) return i; return -1;
        }

        public void AttachHealthBar(Canvas worldCanvas, GameObject prefab) {
            if (!prefab || !worldCanvas) return; var inst = GameObject.Instantiate(prefab, worldCanvas.transform); inst.transform.SetParent(worldCanvas.transform); inst.GetComponent<RectTransform>().sizeDelta = new Vector2(40,4); _hpFill = inst.GetComponentInChildren<UnityEngine.UI.Image>(); var follow = inst.AddComponent<WorldSpaceBillboard>(); follow.Target = this.transform; }

#if UNITY_EDITOR
        void OnDrawGizmosSelected() {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.25f);
        }
#endif
    }
}
