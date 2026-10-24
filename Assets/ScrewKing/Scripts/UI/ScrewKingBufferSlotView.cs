using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing
{
    public sealed class ScrewKingBufferSlotView : MonoBehaviour
    {
        public ScrewKingHudPalette Palette;
        public RectTransform Motion;
        public Image Screw;
        [Tooltip("Canonical model position represented by this slot in Edit mode. Root layout edits remain additive.")]
        public Vector2 PreviewModelPosition;
        Vector2 origin;
        bool captured;
        public void Refresh(PortModel model,int index)
        {
            if(!captured){captured=true;origin=Motion.anchoredPosition;}
            Motion.anchoredPosition=origin+model.HolePosition(index)-PreviewModelPosition;
            var seat=model.Seats[index];Screw.gameObject.SetActive(seat.Screw!=null&&!seat.Moving);
            if(seat.Screw!=null)Screw.sprite=Palette.Screw(seat.Screw.Color);
        }
        public RectTransform Destination=>Screw.rectTransform;
    }
}
