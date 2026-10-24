using System.Collections.Generic;
using UnityEngine;

namespace ScrewKing
{
    // Uses the current board art for debris so the effect follows the reskin.
    public sealed class MainGameFragments : MonoBehaviour
    {
        struct Piece { public Transform Node; public Vector3 Start, Velocity; public float Spin; }
        readonly List<Piece> pieces=new List<Piece>();
        readonly List<Mesh> meshes=new List<Mesh>();
        Material material;
        float age;
        public static void Clear(){foreach(var effect in FindObjectsByType<MainGameFragments>(FindObjectsSortMode.None))Destroy(effect.gameObject);}
        public static void Burst(BoardActor board)
        {
            var sprite=board.Face.sprite;if(!sprite)return;
            var effect=new GameObject("Glass break fragments").AddComponent<MainGameFragments>();
            effect.material=new Material(Shader.Find("Sprites/Default"));effect.material.mainTexture=sprite.texture;
            // Split the rectangular sprite into a 4 x 4 grid. Transparent pixels retain their alpha.
            var bounds=sprite.bounds;
            var rect=sprite.textureRect;
            for(int row=0;row<4;row++)for(int col=0;col<4;col++)
            {
                float x0=Mathf.Lerp(bounds.min.x,bounds.max.x,col/4f),x1=Mathf.Lerp(bounds.min.x,bounds.max.x,(col+1)/4f);
                float y0=Mathf.Lerp(bounds.min.y,bounds.max.y,row/4f),y1=Mathf.Lerp(bounds.min.y,bounds.max.y,(row+1)/4f);
                var center=new Vector3((x0+x1)/2,(y0+y1)/2);
                var vertices=new[]{new Vector3(x0,y0)-center,new Vector3(x1,y0)-center,new Vector3(x1,y1)-center,new Vector3(x0,y1)-center};
                float u0=(rect.x+rect.width*col/4)/sprite.texture.width,u1=(rect.x+rect.width*(col+1)/4)/sprite.texture.width;
                float v0=(rect.y+rect.height*row/4)/sprite.texture.height,v1=(rect.y+rect.height*(row+1)/4)/sprite.texture.height;
                var mesh=new Mesh{vertices=vertices,uv=new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)},triangles=new[]{0,1,2,0,2,3}};
                mesh.colors=new[]{board.Face.color,board.Face.color,board.Face.color,board.Face.color};
                mesh.RecalculateBounds();effect.meshes.Add(mesh);
                var node=new GameObject("Shard");node.transform.SetParent(effect.transform);
                node.transform.position=board.Face.transform.TransformPoint(center);node.transform.rotation=board.transform.rotation;node.transform.localScale=board.Face.transform.lossyScale;
                node.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=node.AddComponent<MeshRenderer>();renderer.sharedMaterial=effect.material;renderer.sortingOrder=20000;
                effect.pieces.Add(new Piece{Node=node.transform,Start=node.transform.position,Velocity=new Vector3((col-1.5f)*1.7f,2+(row*.45f)),Spin=(col%2==0?1:-1)*(120+row*45)});
            }
        }
        void Update()
        {
            age+=Time.deltaTime;
            foreach(var p in pieces){p.Node.position=p.Start+p.Velocity*age+Vector3.down*(7*age*age);p.Node.Rotate(0,0,p.Spin*Time.deltaTime);}
            material.color=new Color(1,1,1,Mathf.Clamp01((.75f-age)/.35f));
            if(age>=.75f)Destroy(gameObject);
        }
        void OnDestroy(){foreach(var mesh in meshes)Destroy(mesh);if(material)Destroy(material);}
    }
}
