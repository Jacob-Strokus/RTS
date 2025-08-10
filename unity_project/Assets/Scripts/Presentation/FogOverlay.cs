using UnityEngine;
using FrontierAges.Sim;
using System.Collections.Generic;

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
    public int MapWidth = 128; // tiles actually used (culling)
    public int MapHeight = 128;
    private Material _matInstance;
        private int _lastTick=-1;
    void Awake(){ _mesh = new Mesh(); GetComponent<MeshFilter>().mesh = _mesh; _matInstance = new Material(GetComponent<MeshRenderer>().sharedMaterial); GetComponent<MeshRenderer>().material = _matInstance; BuildMesh(); }
    public void Init(Simulator sim, int mapW=128, int mapH=128){ Sim=sim; MapWidth=mapW; MapHeight=mapH; RebuildIfNeeded(); }
    public void RebuildIfNeeded(){ if (MapWidth!=_w || MapHeight!=_h) { _w=MapWidth; _h=MapHeight; BuildMesh(); } }
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
    void LateUpdate(){ if (Sim==null) return; if (Sim.State.Visibility==null) return; var dirty = Sim.GetVisionDirty(); if (dirty==null||dirty.Count==0) return; var meshColors = _mesh.colors; foreach (var (x,y) in dirty){ if (x>=_w||y>=_h) continue; int tileIndex=y*_w+x; int vi=tileIndex*4; if (vi+3>=meshColors.Length) continue; byte v=Sim.State.Visibility[x,y]; Color c=v==1? VisibleColor:HiddenColor; meshColors[vi]=c; meshColors[vi+1]=c; meshColors[vi+2]=c; meshColors[vi+3]=c; } _mesh.colors=meshColors; }
    }
}
