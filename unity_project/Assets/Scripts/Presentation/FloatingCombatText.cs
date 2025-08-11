using UnityEngine;
#if TMP_PRESENT
using TMPro;
#endif

namespace FrontierAges.Presentation {
    public class FloatingCombatText : MonoBehaviour {
        public float RiseSpeed = 1.5f; // units per second
        public float Lifetime = 1.0f;
        public float FadeTime = 0.3f;
        private float _age;
        private string _text;
        private bool _critical;
        private TextMesh _legacy;
        [System.NonSerialized] public bool Pooled; // set by manager
        [System.NonSerialized] public FloatingCombatTextManager Manager;
#if TMP_PRESENT
        private TMP_Text _tmp;
#endif
        public void Init(int dmg, bool kill){ _text = dmg.ToString(); _critical = kill; Setup(); }
        private void Setup(){
#if TMP_PRESENT
            _tmp = GetComponent<TMP_Text>();
            if(_tmp==null){ var go = new GameObject("TMP"); go.transform.SetParent(transform,false); _tmp = go.AddComponent<TextMeshPro>(); }
            _tmp.text = _text; _tmp.fontSize = _critical?48:32; _tmp.color = _critical?Color.yellow:Color.white;
#else
            _legacy = GetComponent<TextMesh>(); if(_legacy==null){ _legacy = gameObject.AddComponent<TextMesh>(); }
            _legacy.text = _text; _legacy.fontSize = _critical?48:32; _legacy.color = _critical?Color.yellow:Color.white;
#endif
        }
        void Update(){ _age += Time.deltaTime; transform.position += Vector3.up * RiseSpeed * Time.deltaTime; float remain = Lifetime - _age; if(remain <= FadeTime){ float t = Mathf.Clamp01(remain/ FadeTime); SetAlpha(t); } if(_age>=Lifetime){ _age=0f; if(Manager!=null) Manager.Recycle(this); else Destroy(gameObject); return; } transform.LookAt(Camera.main? Camera.main.transform: transform.position + Vector3.forward); transform.rotation = Quaternion.LookRotation(transform.position - (Camera.main?Camera.main.transform.position:Vector3.zero)); }
        public void OnEnable(){ _age=0f; }
        private void SetAlpha(float a){
#if TMP_PRESENT
            if(_tmp){ var c = _tmp.color; c.a = a; _tmp.color=c; }
#else
            if(_legacy){ var c = _legacy.color; c.a = a; _legacy.color=c; }
#endif
        }
    }
}
