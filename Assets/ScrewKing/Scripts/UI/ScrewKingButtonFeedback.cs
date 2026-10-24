using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrewKing
{
    [DisallowMultipleComponent, RequireComponent(typeof(Button))]
    public sealed class ScrewKingButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, ISubmitHandler
    {
        [Tooltip("Optional graphic to animate. Layout and the Button hit area keep their authored values after release.")]
        public RectTransform Visual;
        [Range(.8f,1f)] public float PressScale=.94f;
        [Range(1f,1.06f)] public float HoverScale=1.018f;
        [Range(0f,.08f)] public float Rebound=.035f;
        [Range(.1f,.5f)] public float ReleaseDuration=.24f;
        Button button;
        RectTransform target;
        Vector3 originalScale;
        Vector2 originalPosition;
        float factor=1,releaseFrom,releaseAge;
        bool pressed,hovered,active,releasing;
        bool Available=>button&&button.IsActive()&&button.IsInteractable();
        void Awake()
        {
            button=GetComponent<Button>();target=Visual?Visual:(RectTransform)transform;
        }
        void Begin()
        {
            if(active)return;
            originalScale=target.localScale;originalPosition=target.anchoredPosition;factor=1;active=true;
        }
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!Available)return;
            Begin();pressed=true;releasing=false;
        }
        public void OnPointerUp(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!pressed)return;
            pressed=false;Release();
        }
        public void OnPointerEnter(PointerEventData e)
        {
            // Touch has no persistent hover. Mouse/pen feedback remains deliberately subtle.
            if(e.pointerId>=0||!Available)return;
            Begin();hovered=true;
        }
        public void OnPointerExit(PointerEventData e)
        {
            hovered=false;if(!active)return;
            pressed=false;Release();
        }
        public void OnSubmit(BaseEventData e)
        {
            if(!Available)return;
            Begin();factor=PressScale;Apply();Release();
        }
        void Release(){releaseFrom=factor;releaseAge=0;releasing=true;}
        void Update()
        {
            if(!active)return;
            if(!Available){Restore();return;}
            float goal=hovered?HoverScale:1;
            if(releasing){
                releaseAge+=Time.unscaledDeltaTime;
                float t=Mathf.Clamp01(releaseAge/Mathf.Max(.01f,ReleaseDuration));
                factor=Mathf.LerpUnclamped(releaseFrom,goal,1-Mathf.Pow(1-t,3))+Rebound*Mathf.Sin(Mathf.PI*t);
                if(t>=1){releasing=false;if(!hovered){Restore();return;}}
            }else factor=Mathf.Lerp(factor,pressed?PressScale:goal,1-Mathf.Exp(-32*Time.unscaledDeltaTime));
            Apply();
        }
        void Apply()
        {
            target.localScale=originalScale*factor;
            // Prefabs often use a top-left pivot. Compensate so the visual shrinks around its centre.
            var center=target.rect.center;
            target.anchoredPosition=originalPosition+Vector2.Scale(center,new Vector2(originalScale.x,originalScale.y))*(1-factor);
        }
        void Restore()
        {
            if(active&&target){target.localScale=originalScale;target.anchoredPosition=originalPosition;}
            active=pressed=hovered=releasing=false;factor=1;
        }
        void OnDisable(){Restore();}
    }
}
