using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing
{
    public sealed class ScrewKingFlyingScrewView : MonoBehaviour
    {
        public ScrewKingHudPalette Palette;
        public RectTransform Motion,Lift,HeadRotation,BodyStretch;
        public Image Head;
        public float BodyMinHeight=5,BodyMaxHeight=40;
        Vector2 liftOrigin,bodySize;
        Vector3 scaleOrigin,headEuler;
        bool captured;
        public void Refresh(int color,Vector2 parentPosition,float lift,float angle,float scale)
        {
            if(!captured){captured=true;liftOrigin=Lift.anchoredPosition;bodySize=BodyStretch.sizeDelta;scaleOrigin=Motion.localScale;headEuler=HeadRotation.localEulerAngles;}
            Motion.anchoredPosition=parentPosition;Motion.localScale=scaleOrigin*scale;
            Lift.anchoredPosition=liftOrigin+Vector2.up*lift;
            HeadRotation.localEulerAngles=headEuler+new Vector3(0,0,angle);
            BodyStretch.sizeDelta=new Vector2(bodySize.x,Mathf.Lerp(BodyMinHeight,BodyMaxHeight,Mathf.Clamp01(lift/50)));
            Head.sprite=Palette.Screw(color);
        }
    }
}
