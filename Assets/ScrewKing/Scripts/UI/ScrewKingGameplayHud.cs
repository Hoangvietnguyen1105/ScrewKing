using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing
{
    // All graphics/layout are serialized prefabs. This component binds state and animates dedicated Motion nodes.
    public sealed class ScrewKingGameplayHud : MonoBehaviour
    {
        public Text LevelLabel,BoxCounter,MessageLabel;
        public string LevelFormat="Level {0}",CounterFormat="{0} / {1} BOXES";
        public Image ProgressFill;
        public bool ShowNormalHint;
        public GameObject NormalHint,HammerHint,HammerShade,HammerHighlight,MessagePanel;
        public CanvasGroup Transition;
        public ScrewKingBoxView[] Boxes;
        public ScrewKingBufferSlotView[] BufferSlots;
        public ScrewKingBoxView ClosingBoxPrefab;
        public ScrewKingFlyingScrewView FlyingScrewPrefab;
        public RectTransform ClosingLayer,FlightLayer;
        public ScrewKingGuideView Guide;
        public Vector2 ModelCenter=new Vector2(375,667);
        readonly Dictionary<BoxState,ScrewKingBoxView> closing=new Dictionary<BoxState,ScrewKingBoxView>();
        readonly Dictionary<NailState,Flight> flights=new Dictionary<NailState,Flight>();
        readonly Stack<ScrewKingBoxView> spareBoxes=new Stack<ScrewKingBoxView>();
        readonly Stack<ScrewKingFlyingScrewView> spareFlights=new Stack<ScrewKingFlyingScrewView>();
        readonly List<BoxState> oldBoxes=new List<BoxState>();
        readonly List<NailState> oldFlights=new List<NailState>();
        PortModel lastModel;
        sealed class Flight
        {
            public ScrewKingFlyingScrewView View;
            public Vector2 Source,SourceView;
            public BoxState Box;
            public int Seat,Hole;
        }
        public bool IsAuthored=>LevelLabel&&ProgressFill&&Boxes!=null&&Boxes.Length==4;
        public void Refresh(PortGame game)
        {
            var model=game.Model;if(model==null)return;
            if(lastModel!=model){ReleaseDynamic();lastModel=model;}
            LevelLabel.text=string.Format(LevelFormat,game.UiLevel);
            BoxCounter.text=string.Format(CounterFormat,model.CompletedBoxes,model.Nails.Count/3);
            ProgressFill.fillAmount=model.Nails.Count==0?0:(float)model.Cleared/model.Nails.Count;
            NormalHint.SetActive(ShowNormalHint&&!model.HammerArmed);HammerHint.SetActive(model.HammerArmed);HammerShade.SetActive(model.HammerArmed);HammerHighlight.SetActive(model.HammerArmed);
            MessagePanel.SetActive(game.UiMessageVisible);if(game.UiMessageVisible)MessageLabel.text=game.UiMessage;
            Transition.gameObject.SetActive(game.UiTransitionAlpha>0);Transition.alpha=game.UiTransitionAlpha;
            for(int i=0;i<Boxes.Length;i++){
                bool active=i<model.Boxes.Count;Boxes[i].gameObject.SetActive(active);if(active)Boxes[i].Refresh(game,model.Boxes[i]);
            }
            oldBoxes.Clear();foreach(var pair in closing)if(!model.ClosingBoxes.Contains(pair.Key))oldBoxes.Add(pair.Key);
            foreach(var key in oldBoxes){var view=closing[key];view.gameObject.SetActive(false);spareBoxes.Push(view);closing.Remove(key);}
            foreach(var box in model.ClosingBoxes){
                if(!closing.TryGetValue(box,out var view)){view=spareBoxes.Count>0?spareBoxes.Pop():Instantiate(ClosingBoxPrefab,ClosingLayer);closing.Add(box,view);view.gameObject.SetActive(true);}
                view.Refresh(game,box,true);
            }
            for(int i=0;i<BufferSlots.Length;i++){
                bool active=i<model.HoleCapacity;BufferSlots[i].gameObject.SetActive(active);if(active)BufferSlots[i].Refresh(model,i);
            }
            oldFlights.Clear();foreach(var pair in flights)if(!model.MotionVisual(pair.Key,out _,out _,out _,out _))oldFlights.Add(pair.Key);
            foreach(var key in oldFlights){var view=flights[key].View;view.gameObject.SetActive(false);spareFlights.Push(view);flights.Remove(key);}
            foreach(var nail in model.Nails){
                if(!model.MotionVisual(nail,out var point,out float lift,out float angle,out float scale))continue;
                model.MotionViewTarget(nail,out var box,out int seat,out int hole);
                if(!flights.TryGetValue(nail,out var flight)){
                    var view=spareFlights.Count>0?spareFlights.Pop():Instantiate(FlyingScrewPrefab,FlightLayer);view.gameObject.SetActive(true);
                    flight=new Flight{View=view};flights.Add(nail,flight);flight.Source=point;flight.SourceView=MapCanonical(point,model);
                    flight.Box=box;flight.Seat=seat;flight.Hole=hole;
                }
                if(flight.Box!=box||flight.Seat!=seat||flight.Hole!=hole){flight.Source=point;flight.SourceView=MapCanonical(point,model);flight.Box=box;flight.Seat=seat;flight.Hole=hole;}
                model.MotionDestination(nail,out var target);
                Vector2 targetView=TargetPoint(box,seat,hole,model,target);
                float distance=Vector2.Distance(flight.Source,target);
                float progress=distance<.001f?1:Mathf.Clamp01(1-Vector2.Distance(point,target)/distance);
                Vector2 warped=Canonical(point)+Vector2.Lerp(flight.SourceView-Canonical(flight.Source),targetView-Canonical(target),progress);
                var design=(RectTransform)transform;
                var world=design.TransformPoint(warped);
                Vector2 inParent=((RectTransform)flight.View.Motion.parent).InverseTransformPoint(world);
                flight.View.Refresh(nail.Color,inParent,lift,angle,scale);
            }
            bool guide=game.UiGuideScreenTarget(out var guidePoint);
            var canvas=Guide.GetComponentInParent<Canvas>();
            Camera uiCamera=canvas&&canvas.renderMode!=RenderMode.ScreenSpaceOverlay?canvas.worldCamera:null;
            Vector2 localGuidePoint=default;
            guide=guide&&RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)Guide.Motion.parent,guidePoint,uiCamera,out localGuidePoint);
            Guide.gameObject.SetActive(guide);
            if(guide)Guide.Refresh(localGuidePoint);
        }
        // Points are local to this 750x1334 HUD's centered RectTransform.
        Vector2 Canonical(Vector2 p)=>new Vector2(ModelCenter.x+p.x-750*.5f,1334*.5f-(ModelCenter.y-p.y));
        Vector2 Local(RectTransform point)=>((RectTransform)transform).InverseTransformPoint(point.TransformPoint(point.rect.center));
        Vector2 TargetPoint(BoxState box,int seat,int hole,PortModel model,Vector2 fallback)
        {
            int index=box!=null?model.Boxes.IndexOf(box):-1;
            if(index>=0&&index<Boxes.Length)return Local(Boxes[index].Seat(seat));
            if(box!=null&&closing.TryGetValue(box,out var closingView))return Local(closingView.Seat(seat));
            if(hole>=0&&hole<BufferSlots.Length)return Local(BufferSlots[hole].Destination);
            return Canonical(fallback);
        }
        Vector2 MapCanonical(Vector2 p,PortModel model)
        {
            for(int i=0;i<model.HoleCapacity;i++)if(Vector2.SqrMagnitude(model.HolePosition(i)-p)<.01f)return Local(BufferSlots[i].Destination);
            return Canonical(p);
        }
        void ReleaseDynamic()
        {
            foreach(var pair in closing){pair.Value.gameObject.SetActive(false);spareBoxes.Push(pair.Value);}closing.Clear();
            foreach(var pair in flights){pair.Value.View.gameObject.SetActive(false);spareFlights.Push(pair.Value.View);}flights.Clear();
        }
    }
}
