using UnityEngine;

namespace ScrewKing
{
    public sealed class ScrewKingGuideView : MonoBehaviour
    {
        public RectTransform Motion,Circle,Hand;
        public float PulseAmplitude=.08f,PulseSpeed=5,HandBob=6;
        Vector3 circleScale;
        Vector2 handOrigin,circleOrigin;
        bool captured;
        public void Refresh(Vector2 position)
        {
            if(!captured){captured=true;circleScale=Circle.localScale;circleOrigin=Circle.anchoredPosition;handOrigin=Hand.anchoredPosition;}
            Motion.anchoredPosition=position;
            float pulse=1+PulseAmplitude*Mathf.Sin(Time.time*PulseSpeed);
            Circle.localScale=circleScale*pulse;
            // Preserve the authored centre even if the graphic has a corner pivot.
            Circle.anchoredPosition=circleOrigin+Vector2.Scale(Circle.rect.center,new Vector2(circleScale.x,circleScale.y))*(1-pulse);
            Hand.anchoredPosition=handOrigin+Vector2.down*Mathf.Sin(Time.time*PulseSpeed)*HandBob;
        }
    }
}
