using UnityEngine;
using UnityEngine.UI;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class HUDController : MonoBehaviour {
        public Text TickText;
        public Text UnitCountText;
    public Text ProductionText;
    public Slider ProductionSlider;
        public Text ResourceText;
        public Text SelectionText; // new: show first selected entity HP / queue
        private Simulator _sim;
        private SelectionManager _sel;
        void Start() { _sim = FindObjectOfType<SimBootstrap>()?.GetSimulator(); }
        void Update() {
            if (_sim == null) return;
            if (_sel == null) _sel = FindObjectOfType<SelectionManager>();
            if (TickText) TickText.text = $"Tick: {_sim.State.Tick}";
            if (UnitCountText) UnitCountText.text = $"Units: {_sim.State.UnitCount}";
            if (_sim.State.BuildingCount > 0) {
                var b = _sim.State.Buildings[0];
                if (b.HasActiveQueue == 1) {
                    if (ProductionText) ProductionText.text = "Training";
                    if (ProductionSlider) {
                        float total = b.QueueTotalMs > 0 ? b.QueueTotalMs : 1f;
                        ProductionSlider.value = 1f - (b.QueueRemainingMs / total);
                    }
                } else {
                    if (ProductionText) ProductionText.text = "Idle";
                    if (ProductionSlider) ProductionSlider.value = 0f;
                }
            } else {
                if (ProductionText) ProductionText.text = "No Buildings";
                if (ProductionSlider) ProductionSlider.value = 0f;
            }
            if (ResourceText) {
                var f = _sim.State.Factions[0];
                ResourceText.text = $"F:{f.Food} W:{f.Wood} S:{f.Stone} M:{f.Metal}";
            }
            if (SelectionText && _sel != null) {
                if (_sel.Selected.Count == 0) SelectionText.text = "No Selection";
                else {
                    int showId = default; foreach (var id in _sel.Selected) { showId = id; break; }
                    int idx = FindUnitIndex(showId);
                    if (idx >= 0) {
                        ref var u = ref _sim.State.Units[idx];
                        SelectionText.text = $"Unit {u.Id} HP {u.HP}";
                    } else SelectionText.text = "Selection gone";
                }
            }
        }
        private int FindUnitIndex(int id) { var ws=_sim.State; for (int i=0;i<ws.UnitCount;i++) if (ws.Units[i].Id==id) return i; return -1; }
    }
}
