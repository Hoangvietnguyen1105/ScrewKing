using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScrewKing
{
    public sealed class BoardActor : MonoBehaviour
    {
        public BoardData Data;
        public int Layer;
        public SpriteRenderer Face, Outline;
        public readonly List<NailState> Nails = new List<NailState>();
        CocosSourceRuntime source;
        public int Key;
        public bool HasSource => sourceAdded;
        int sourceIndex;
        bool sourceAdded;
        public bool Shown, Interactive;
        public bool Cleared => Nails.All(n => n.Removed);
        Color color;
        float clickableAt;
        float revealedAt=-10;
        readonly List<SpriteRenderer> holes=new List<SpriteRenderer>();
        public float ScrewAlpha => !Interactive?0:Mathf.Clamp01((Time.time-revealedAt-.4f)/.4f);
        public bool Clickable => Interactive && Time.time>=clickableAt;

        public void Setup(BoardData data, ShapeData shape, int layer, Color tint, IEnumerable<NailState> nails, CocosSourceRuntime source,int key,int index)
        {
            this.source=source;Key=key;sourceIndex=index;
            Data = data; Layer = layer; color = tint; Nails.AddRange(nails);
            gameObject.layer = 8 + layer % 10;
            transform.position = new Vector3(data.x / 100f, (data.y + GameplayLayout.BoardCenterY) / 100f, 0);
            transform.rotation = Quaternion.Euler(0, 0, data.angle);
            Face = MakeSprite("Face", "Boards/icon/" + data.id, layer * 100 + 1, data.w, data.h);
            Outline = MakeSprite("Outline", "Boards/outline/" + data.id, layer * 100 + 2, data.w, data.h);
            foreach(var nail in Nails)
            {
                var hole=MakeSprite("Hole", "Screws/ScrewHole", layer*100+3, 32, 32);
                hole.transform.localPosition=new Vector3(nail.Data.x/100f,(nail.Data.y-2)/100f,0);
                holes.Add(hole);
            }
            // All fixtures, partitioning and joints are created by the APK's Box2D implementation.
        }
        SpriteRenderer MakeSprite(string name, string path, int order, float width, float height)
        {
            var child = new GameObject(name); child.transform.SetParent(transform, false);
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = Resources.Load<Sprite>("Art/" + path);
            renderer.sortingOrder = order;
            if (renderer.sprite)
                child.transform.localScale = new Vector3(width / 100f / renderer.sprite.bounds.size.x, height / 100f / renderer.sprite.bounds.size.y, 1);
            return renderer;
        }
        public Vector3 NailPosition(NailState nail) => transform.TransformPoint(new Vector3(nail.Data.x, nail.Data.y) / 100f);
        public void SetVisibility(bool shown, bool interactive)
        {
            if(interactive && !Interactive){revealedAt=Time.time;clickableAt=Time.time+.8f;}
            Shown = shown; Interactive = interactive;
            Face.enabled = Outline.enabled = shown;
            if(shown && !sourceAdded){source.AddBoard(Key,Layer,sourceIndex);sourceAdded=true;}
            if(sourceAdded)source.ShowBoard(Key,interactive);
            foreach(var hole in holes)hole.enabled=shown&&interactive;
            Face.color = interactive ? new Color(color.r,color.g,color.b,220f/255f) : new Color(.5f,.5f,.5f,150f/255f);
            Outline.color = new Color(1, 1, 1, interactive ? 1 : 0);
        }
        public void Release(NailState nail)
        {
            if(sourceAdded)source.Release(Key,ArrayIndex(nail));
        }
        int ArrayIndex(NailState nail)=>System.Array.IndexOf(Data.nail,nail.Data);
        public void BeginUnscrew(){if(sourceAdded)source.Dynamic(Key);}
        public bool Covers(Vector2 world,float radius=0)
        {
            return Shown && sourceAdded && source.Covers(Key,GameplayLayout.ViewToPhysics(world),radius);
        }
        public void RemoveSource(){if(sourceAdded){source.Remove(Key);sourceAdded=false;}Shown=false;Face.enabled=Outline.enabled=false;foreach(var hole in holes)hole.enabled=false;}
        void Update() {
            if(Shown && Interactive){float t=Mathf.Clamp01((Time.time-revealedAt)/.4f);Face.color=Color.Lerp(new Color(.5f,.5f,.5f,150f/255f),new Color(color.r,color.g,color.b,220f/255f),t);Outline.color=new Color(1,1,1,t);}
            if (Cleared && transform.position.y < -11) {RemoveSource();Destroy(gameObject);} }
    }
}
