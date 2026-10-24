using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing
{
    public sealed class ScrewKingBoxView : MonoBehaviour
    {
        public ScrewKingHudPalette Palette;
        [Tooltip("Animation-only transform. Edit this prefab's root or the graphic children for persistent layout changes.")]
        public RectTransform Motion;
        public float PreviewX;
        public Image Body,LockedArt,Lid;
        public RectTransform LidMotion;
        public GameObject Seats;
        public Image[] Screws;
        public Button UnlockButton;
        public float ExitDistance=300,LidDropDistance=294;
        Vector2 motionOrigin,lidOrigin;
        Vector3 motionScale;
        bool captured;
        BoxState state;
        PortGame owner;
        void Capture()
        {
            if(captured)return;captured=true;
            motionOrigin=Motion.anchoredPosition-new Vector2(PreviewX,0);motionScale=Motion.localScale;
            if(LidMotion)lidOrigin=LidMotion.anchoredPosition;
        }
        public void Refresh(PortGame game,BoxState box,bool closing=false)
        {
            Capture();owner=game;state=box;var model=game.Model;
            Body.gameObject.SetActive(!box.Locked);LockedArt.gameObject.SetActive(box.Locked);Seats.SetActive(!box.Locked);
            UnlockButton.interactable=box.Locked&&game.UiHudInteractable;UnlockButton.gameObject.SetActive(box.Locked);
            Body.sprite=Palette.Box(box.Color);
            for(int i=0;i<Screws.Length;i++){
                bool occupied=closing||model.SeatSettled(box,i);Screws[i].gameObject.SetActive(occupied);
                if(occupied)Screws[i].sprite=Palette.Screw(box.Color);
            }
            float y=0,squash=1;Lid.gameObject.SetActive(closing);
            if(closing){
                float t=(float)(model.Clock-box.ClosedAt);
                y=Mathf.Clamp01((t-.2f)/.4f)*ExitDistance;
                squash=t<.2f?Mathf.Lerp(1,.7f,t/.2f):Mathf.LerpUnclamped(.7f,1,BackOut(Mathf.Clamp01((t-.2f)/.25f)));
                Lid.sprite=Palette.Lid(box.Color);LidMotion.anchoredPosition=lidOrigin+Vector2.up*LidDropDistance*(1-Mathf.Clamp01(t/.2f));
            }
            Motion.anchoredPosition=motionOrigin+new Vector2((float)box.X(model.Clock),y);Motion.localScale=Vector3.Scale(motionScale,new Vector3(1,squash,1));
        }
        public RectTransform Seat(int index)=>Screws[index].rectTransform;
        public void Unlock(){if(owner&&state!=null)owner.UiRequestUnlock(state);}
        static float BackOut(float t){float v=t-1;return 1+2.70158f*v*v*v+1.70158f*v*v;}
    }
}
