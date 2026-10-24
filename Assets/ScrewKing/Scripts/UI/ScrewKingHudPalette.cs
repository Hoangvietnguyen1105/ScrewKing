using UnityEngine;

namespace ScrewKing
{
    [CreateAssetMenu(menuName="Screw King/UI/HUD Palette")]
    public sealed class ScrewKingHudPalette : ScriptableObject
    {
        [Tooltip("Index is the original gameplay color ID. Index 0 is unused.")]
        public Sprite[] Screws,Boxes,Lids;
        public Sprite Screw(int id)=>Get(Screws,id);
        public Sprite Box(int id)=>Get(Boxes,id);
        public Sprite Lid(int id)=>Get(Lids,id);
        static Sprite Get(Sprite[] list,int id)=>list!=null&&id>=0&&id<list.Length?list[id]:null;
    }
}
