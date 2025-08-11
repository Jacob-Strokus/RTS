using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    public class FloatingCombatTextManager : MonoBehaviour {
        [Header("Floating Text")]
        public GameObject FloatingTextPrefab; // Prefab with FloatingCombatText
        public int PoolSize = 64;
        public float WorldScale = 0.001f; // convert sim milli units to world units
        [Header("Impact Particles")]
        public GameObject ImpactParticlePrefab; // optional particle prefab (destroy or pooled)
        public int ParticlePoolSize = 32;
        public float ParticleYOffset = 1.2f;

        private FloatingCombatText[] _textPool;
        private int _textPoolIndex;
        private GameObject[] _particlePool;
        private int _particlePoolIndex;
        private bool _initialized;

        void Awake(){ InitPools(); }
        private void InitPools(){ if(_initialized) return; _initialized=true; if(FloatingTextPrefab!=null && PoolSize>0){ _textPool = new FloatingCombatText[PoolSize]; for(int i=0;i<PoolSize;i++){ var go = Instantiate(FloatingTextPrefab, transform); go.SetActive(false); var fct = go.GetComponent<FloatingCombatText>(); if(!fct) fct = go.AddComponent<FloatingCombatText>(); fct.Pooled = true; fct.Manager = this; _textPool[i]=fct; } }
            if(ImpactParticlePrefab!=null && ParticlePoolSize>0){ _particlePool = new GameObject[ParticlePoolSize]; for(int i=0;i<ParticlePoolSize;i++){ var go = Instantiate(ImpactParticlePrefab, transform); go.SetActive(false); _particlePool[i]=go; } }
        }

        public void Spawn(DamageEvent de){ InitPools(); // ensure
            var pos = new Vector3(de.TargetX * WorldScale, 1.5f, de.TargetY * WorldScale);
            // Floating text
            if(_textPool!=null){ var fct = _textPool[_textPoolIndex]; _textPoolIndex = (_textPoolIndex+1)%_textPool.Length; fct.gameObject.SetActive(true); fct.transform.position = pos; fct.Init(de.Damage, de.WasKill==1); }
            // Particles
            if(_particlePool!=null){ var go = _particlePool[_particlePoolIndex]; _particlePoolIndex = (_particlePoolIndex+1)%_particlePool.Length; go.transform.position = new Vector3(pos.x, ParticleYOffset, pos.z); go.SetActive(true); var ps = go.GetComponent<ParticleSystem>(); if(ps){ ps.Clear(true); ps.Play(true); } else { // auto disable fallback
                    Invoke(nameof(DisableParticleLater), 1f); }
        }
        }
        private void DisableParticleLater(){ /* intentionally left blank for simplicity */ }
        public void Recycle(FloatingCombatText txt){ if(!txt.Pooled) Destroy(txt.gameObject); else { txt.gameObject.SetActive(false); } }
    }
}
