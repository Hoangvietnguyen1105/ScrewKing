using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing
{
    // Only dynamic content is bound here. Static labels, art and RectTransforms remain prefab-authored.
    public sealed class ScrewKingUiView : MonoBehaviour
    {
        public Text LevelLabel, CounterLabel, TitleLabel, StatusLabel, RewardButtonLabel;
        public Text DescriptionLabel,CancelButtonLabel,PriceLabel;
        public Image OfferIcon;
        public Sprite SlotIcon,ClearIcon,HammerIcon,BoxIcon;
        public ScrewKingUiToggle[] Switches;
        public GameObject LocalSimulationNotice;
        public RectTransform AnimatedContent;
        public float EntranceDuration=.3f;
        public RectTransform LeftDoor,RightDoor;
        public float DoorHold=.4f,DoorDuration=.65f;
        Vector3 contentScale;
        Vector2 leftClosed,rightClosed;
        float shownAt;
        bool captured;
        void Capture()
        {
            if(captured)return;captured=true;
            if(AnimatedContent)contentScale=AnimatedContent.localScale;
            if(LeftDoor)leftClosed=LeftDoor.anchoredPosition;
            if(RightDoor)rightClosed=RightDoor.anchoredPosition;
        }
        public void Show(bool show)
        {
            Capture();
            if(gameObject.activeSelf==show)return;
            if(show)shownAt=Time.unscaledTime;
            gameObject.SetActive(show);
            if(!show && AnimatedContent)AnimatedContent.localScale=contentScale;
        }
        public void Animate()
        {
            if(!AnimatedContent)return;
            float t=Mathf.Clamp01((Time.unscaledTime-shownAt)/Mathf.Max(.01f,EntranceDuration));
            float b=1+2.70158f*Mathf.Pow(t-1,3)+1.70158f*Mathf.Pow(t-1,2);
            AnimatedContent.localScale=contentScale*Mathf.LerpUnclamped(.8f,1,b);
        }
        public void AnimateDoors(float age)
        {
            Capture();float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(DoorHold,DoorHold+DoorDuration,age));
            if(LeftDoor)LeftDoor.anchoredPosition=leftClosed+Vector2.left*LeftDoor.rect.width*t;
            if(RightDoor)RightDoor.anchoredPosition=rightClosed+Vector2.right*RightDoor.rect.width*t;
        }
    }
}
