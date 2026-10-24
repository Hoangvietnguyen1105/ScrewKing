using System.Collections.Generic;
using UnityEngine;

namespace ScrewKing
{
    // Screens are authored at 750x1334. On taller or wider screens this keeps them centred, makes the
    // backgrounds cover the whole screen and pins the top/bottom bars to the safe edges of the device.
    [DisallowMultipleComponent]
    public sealed class ScrewKingSafeLayout : MonoBehaviour
    {
        const float DesignWidth=750,DesignHeight=1334;
        static readonly HashSet<string> Cover=new HashSet<string>{"WoodBackground"};
        static readonly HashSet<string> Fill=new HashSet<string>{"Dimmer","InputBlocker","Transition"};
        static readonly HashSet<string> Top=new HashSet<string>{"CurrencyBadge","ScrewIcon","CurrencyCounter","CounterCaption","SettingsButton","CurrencyIcon","CurrencyBalance","TikTokShortcutEntry","TikTokRevisitEntry","Header"};
        static readonly HashSet<string> Bottom=new HashSet<string>{"Boosters","NormalHint","HammerHint","Footer","HomeTabBackground","HomeIcon","HomeCaption","ShopComingSoon","CardsComingSoon","ShopCaption","CardsCaption"};
        [Tooltip("Design pixels kept clear at the top in the TikTok player: status bar plus TikTok's menu capsule. Screen.safeArea does not report them in WebGL.")]
        public float WebTopReserve=170;
        [Tooltip("Design pixels kept clear at the bottom in the TikTok player (home indicator).")]
        public float WebBottomReserve=40;
        sealed class Item { public RectTransform Rect; public Vector2 Position,Size; public int Kind; }
        const int KindCover=0,KindFill=1,KindTop=2,KindBottom=3,KindDoorTop=4,KindDoorBottom=5;
        readonly List<Item> items=new List<Item>();
        RectTransform canvasRect;
        Vector2 lastScreen,lastCanvas;Rect lastSafe;

        public void Bind(ScrewKingUiController ui)
        {
            // Authored positions are captured once; binding again must not treat shifted positions as authored.
            if(canvasRect==(RectTransform)ui.transform){Apply();return;}
            canvasRect=(RectTransform)ui.transform;items.Clear();lastScreen=Vector2.zero;
            foreach(var view in new MonoBehaviour[]{ui.Home,ui.Settings,ui.Win,ui.Lose,ui.Reward,ui.Loading,ui.Shortcut,ui.Revisit})
                if(view)Collect(view.transform);
            if(ui.Gameplay){var authored=ui.Gameplay.transform.Find("AuthoredHUD");Collect(authored?authored:ui.Gameplay.transform);}
            if(ui.Loading)foreach(var door in new[]{ui.Loading.LeftDoor,ui.Loading.RightDoor}){
                if(!door)continue;
                Add(door.Find("TopWood"),KindDoorTop);Add(door.Find("BottomWood"),KindDoorBottom);
            }
            Apply();
        }
        void Collect(Transform root)
        {
            foreach(Transform child in root){
                if(Cover.Contains(child.name))Add(child,KindCover);
                else if(Fill.Contains(child.name))Add(child,KindFill);
                else if(Top.Contains(child.name))Add(child,KindTop);
                else if(Bottom.Contains(child.name))Add(child,KindBottom);
            }
        }
        void Add(Transform target,int kind)
        {
            if(target is RectTransform rect)items.Add(new Item{Rect=rect,Position=rect.anchoredPosition,Size=rect.sizeDelta,Kind=kind});
        }
        void LateUpdate()
        {
            // CanvasScaler resizes the canvas after the first frame, so watch its size as well as the screen.
            if(canvasRect && (lastCanvas!=canvasRect.rect.size || lastScreen.x!=Screen.width || lastScreen.y!=Screen.height || lastSafe!=Screen.safeArea))Apply();
        }
        void Apply()
        {
            if(!canvasRect || Screen.height<=0)return;
            Canvas.ForceUpdateCanvases();
            var canvas=canvasRect.rect.size;
            if(canvas.x<=0 || canvas.y<=0)return;
            lastScreen=new Vector2(Screen.width,Screen.height);lastSafe=Screen.safeArea;lastCanvas=canvas;
            float extraX=Mathf.Max(0,(canvas.x-DesignWidth)*.5f),extraY=Mathf.Max(0,(canvas.y-DesignHeight)*.5f);
            float pixels=canvas.y/Screen.height;
            float safeTop=(Screen.height-lastSafe.yMax)*pixels,safeBottom=lastSafe.yMin*pixels;
            if(Application.platform==RuntimePlatform.WebGLPlayer){safeTop=Mathf.Max(safeTop,WebTopReserve);safeBottom=Mathf.Max(safeBottom,WebBottomReserve);}
            // Positive moves the bar towards its screen edge, negative pushes it inwards out of a notch.
            float topShift=extraY-safeTop,bottomShift=extraY-safeBottom;
            float cover=Mathf.Max(canvas.x/DesignWidth,canvas.y/DesignHeight);
            foreach(var item in items){
                if(!item.Rect)continue;
                switch(item.Kind){
                    case KindCover:
                        var size=new Vector2(DesignWidth,DesignHeight)*cover;
                        item.Rect.sizeDelta=size;item.Rect.anchoredPosition=item.Position+new Vector2(-(size.x-DesignWidth)*.5f,(size.y-DesignHeight)*.5f);break;
                    case KindFill:
                        item.Rect.sizeDelta=new Vector2(DesignWidth+extraX*2,DesignHeight+extraY*2);item.Rect.anchoredPosition=item.Position+new Vector2(-extraX,extraY);break;
                    case KindTop:item.Rect.anchoredPosition=item.Position+Vector2.up*topShift;break;
                    case KindBottom:item.Rect.anchoredPosition=item.Position+Vector2.down*bottomShift;break;
                    case KindDoorTop:item.Rect.sizeDelta=item.Size+Vector2.up*extraY;item.Rect.anchoredPosition=item.Position+Vector2.up*extraY;break;
                    case KindDoorBottom:item.Rect.sizeDelta=item.Size+Vector2.up*extraY;break;
                }
            }
        }
    }
}
