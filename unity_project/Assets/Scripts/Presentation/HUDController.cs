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
        private Simulator _sim;
        void Start() { _sim = FindObjectOfType<SimBootstrap>()?.GetSimulator(); }
        void Update() {
            if (_sim == null) return;
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
        }
    }
}
