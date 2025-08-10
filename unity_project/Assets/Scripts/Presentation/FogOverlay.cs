using UnityEngine;
using FrontierAges.Sim;

namespace FrontierAges.Presentation {
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FogOverlay : MonoBehaviour {
        public Simulator Sim;
        public float TileSize = 1f;
        public Color HiddenColor = new Color(0,0,0,0.6f);
        public Color VisibleColor = new Color(0,0,0,0f);
        private Mesh _mesh;
        private Color[] _colors;
        private int _w=128,_h=128;
        private int _lastTick=-1;
        void Awake(){ _mesh = new Mesh(); GetComponent<MeshFilter>().mesh = _mesh; BuildMesh(); }
        public void Init(Simulator sim){ Sim=sim; }
        void BuildMesh(){
            _mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            var verts = new System.Collections.Generic.List<Vector3>();
            var tris = new System.Collections.Generic.List<int>();
            _colors = new Color[_w*_h];
            for(int y=0;y<_h;y++) for(int x=0;x<_w;x++) {
                int baseIndex = verts.Count;
                verts.Add(new Vector3(x,0,y));
                verts.Add(new Vector3(x+1,0,y));
                verts.Add(new Vector3(x+1,0,y+1));
                verts.Add(new Vector3(x,0,y+1));
                tris.Add(baseIndex); tris.Add(baseIndex+2); tris.Add(baseIndex+1);
                tris.Add(baseIndex); tris.Add(baseIndex+3); tris.Add(baseIndex+2);
                _colors[(y*_w)+x] = HiddenColor;
            }
            _mesh.SetVertices(verts); _mesh.SetTriangles(tris,0); var meshColors = new System.Collections.Generic.List<Color>(verts.Count); for(int i=0;i<_w*_h;i++){ var c=_colors[i]; meshColors.Add(c); meshColors.Add(c); meshColors.Add(c); meshColors.Add(c);} _mesh.SetColors(meshColors); _mesh.RecalculateBounds();
        }
        void LateUpdate(){ if (Sim==null) return; if (Sim.State.Tick==_lastTick) return; _lastTick=Sim.State.Tick; var ws=Sim.State; if(ws.Visibility==null) return; var meshColors = _mesh.colors; for(int y=0;y<_h;y++) for(int x=0;x<_w;x++){ int tileIndex=y*_w+x; byte v= ws.Visibility[x,y]; Color c=v==1? VisibleColor:HiddenColor; int vi=tileIndex*4; if(vi+3<meshColors.Length){ meshColors[vi]=c; meshColors[vi+1]=c; meshColors[vi+2]=c; meshColors[vi+3]=c; } } _mesh.colors=meshColors; }
    }
}
