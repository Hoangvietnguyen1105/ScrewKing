using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ScrewKing
{
    [Serializable] public class NailData { public float x, y; public int type; }
    [Serializable] public class BoardData { public int id, color; public float x, y, w, h, angle; public NailData[] nail; }
    [Serializable] public class LayerData { public BoardData[] boards; }
    [Serializable] public class LevelData
    {
        public int id, show, gray;
        public int[] diff, types, box;
        public LayerData[] layers;
    }
    [Serializable] public class ShapeSize { public float width, height; }
    [Serializable] public class ShapeData { public int id; public Vector2[] points; public ShapeSize size; }
    [Serializable] public class ShapeCatalog { public ShapeData[] shapes; }

    public sealed class NailState
    {
        public int Id, Layer, Color;
        public BoardData Board;
        public NailData Data;
        public bool Removed;
    }
    public sealed class BoxState
    {
        public int Color, InFlight;
        public bool Closing;
        public double ClosedAt;
        public readonly List<int> Screws = new List<int>();
        public bool Locked => Color == 0;
        public double FromX=-470, TargetX=-470, MoveStart, MoveEnd;
        public double X(double time) => time<=MoveStart?FromX:time>=MoveEnd?TargetX:FromX+(TargetX-FromX)*(time-MoveStart)/(MoveEnd-MoveStart);
        public bool IsCenter(double time) => Math.Abs(X(time))<=253.75;
    }
    public sealed class HoleState { public NailState Screw; public bool Moving; public int Index; }
    public enum SourcePrompt { None, Slot, Swap, Hammer, Clear, Unlock, ClearRescue, Revive }

    /// <summary>Source state machine. Reservation, arrival, full-box and refill are separate events.</summary>
    public sealed class PortModel
    {
        public readonly LevelData Level;
        public readonly List<NailState> Nails = new List<NailState>();
        public readonly List<BoxState> Boxes = new List<BoxState>();
        public readonly List<BoxState> ClosingBoxes = new List<BoxState>();
        public readonly List<HoleState> Seats = new List<HoleState>();
        public List<int> Holes => Seats.Where(s=>s.Screw!=null).Select(s=>s.Screw.Color).ToList();
        public readonly List<int> PendingBoxes = new List<int>();
        public readonly Dictionary<int,int> Cache = new Dictionary<int,int>();
        public int HoleCapacity => Seats.Count;
        public int Cleared, CompletedBoxes, DoScrewCount, FullBoxCount;
        public double Clock { get; private set; }
        public bool Won { get; private set; }
        public bool Lost { get; private set; }
        public bool Ready => Clock>=.6 && Prompt==SourcePrompt.None && !Won && !Lost;
        public int Remaining => Nails.Count(n=>!n.Removed);
        public SourcePrompt Prompt { get; private set; }
        public bool HammerArmed { get; private set; }
        bool offeredClear;
        BoxState requestedLock;
        public Func<NailState,Vector2> NailPosition;
        public Action<NailState> OnLifted;
        public Action<bool> OnLanded;
        public Action<string> OnSound;
        public Action<NailState,Vector2> OnMotionStarted;
        public Func<int[],int> Pick;
        public int RandomDraws { get; private set; }
        readonly List<Scheduled> events=new List<Scheduled>();
        readonly List<Motion> motions=new List<Motion>();
        int order;
        sealed class Scheduled { public double At; public int Order; public Action Action; }
        sealed class Motion { public NailState Nail; public BoxState Box; public HoleState Hole; public Vector2 Position; public int Seat, Stage; public double Elapsed, FlightElapsed; public Action Callback; }
        public bool HasPending => events.Count>0 || motions.Count>0 || ClosingBoxes.Count>0 || Boxes.Any(b=>Clock<b.MoveEnd);
        static readonly double[] BoxPositions={246,82,-82,-246};
        public PortModel(LevelData data,int seed,CocosSourceRuntime source=null)
        {
            bool owned=source==null;
            if(owned)source=new CocosSourceRuntime();
            SourceGeneration generated;
            try{generated=source.Generate(data,seed);}finally{if(owned)source.Dispose();}
            Level=generated.level;RandomDraws=generated.randomDraws;
            int id=0;
            for(int layer=0;layer<Level.layers.Length;layer++)
                foreach(var board in Level.layers[layer].boards)
                    foreach(var nail in board.nail)
                        Nails.Add(new NailState{Id=id++,Layer=layer,Color=nail.type,Board=board,Data=nail});
            PendingBoxes.AddRange(generated.queue);
            foreach(int color in generated.initial)Boxes.Add(new BoxState{Color=color});
            for(int i=0;i<5;i++)Seats.Add(new HoleState{Index=i});
            // updateCfg -> checkLayerStatus(.1 + .4 reveal) -> doBoxAct(1s, .3s movement).
            MoveBoxes(1.5,true);
        }
        void Schedule(double delay,Action action)=>events.Add(new Scheduled{At=Clock+delay,Order=order++,Action=action});
        public void Tick(float delta)
        {
            Clock+=delta;
            foreach(var motion in motions.ToArray())AdvanceMotion(motion,delta);
            while(true)
            {
                var due=events.Where(e=>e.At<=Clock+1e-9).OrderBy(e=>e.At).ThenBy(e=>e.Order).FirstOrDefault();
                if(due==null)break;events.Remove(due);due.Action();
            }
        }
        public void Settle()
        {
            for(int i=0;i<3600 && (HasPending || Clock<2);i++)Tick(1f/60);
            if(HasPending)throw new InvalidOperationException("Source event queue did not settle.");
        }
        public bool Take(NailState nail)
        {
            if(!Ready || nail.Removed)return false;
            var hole=Seats.FirstOrDefault(s=>s.Screw==null);
            if(hole==null)return false; // Original unscrew checks this before matching box.
            var box=Boxes.FirstOrDefault(b=>b.IsCenter(Clock) && b.Color==nail.Color && b.Screws.Count<3);
            nail.Removed=true;
            BeginMotion(nail,box,box==null?hole:null,Position(nail),()=>
            {
                if(box!=null)CheckBoxFull();else {CheckHoleToBox(hole);CheckGameFinish();}
            });
            return true;
        }
        Vector2 Position(NailState nail)
        {
            if(NailPosition!=null)return NailPosition(nail);
            double angle=nail.Board.angle*Math.PI/180;
            return new Vector2((float)(nail.Board.x+nail.Data.x*Math.Cos(angle)-nail.Data.y*Math.Sin(angle)),(float)(nail.Board.y+GameplayLayout.BoardCenterY+nail.Data.x*Math.Sin(angle)+nail.Data.y*Math.Cos(angle)));
        }
        public Vector2 HolePosition(int i)
        {
            int count=Math.Min(8,HoleCapacity);
            if(i<8)return new Vector2((i-(count-1)*.5f)*64,HoleCapacity>8?369.8f:359.4f);
            return new Vector2(-224+(i-8)*64,317.8f);
        }
        Vector2 Target(Motion m)
        {
            if(m.Hole!=null)return HolePosition(m.Hole.Index);
            float[] xs={0,-30,30};float[] ys={115,65,65};
            return new Vector2((float)m.Box.X(Clock)+xs[m.Seat],409.5f+ys[m.Seat]);
        }
        void BeginMotion(NailState nail,BoxState box,HoleState hole,Vector2 position,Action callback)
        {
            var m=new Motion{Nail=nail,Box=box,Hole=hole,Position=position,Callback=callback};
            if(box!=null){m.Seat=box.Screws.Count;box.Screws.Add(nail.Color);box.InFlight++;}
            else {hole.Screw=nail;hole.Moving=true;}
            motions.Add(m);DoScrewCount++;
            OnMotionStarted?.Invoke(nail,position);
        }
        public bool MotionPosition(NailState nail,out Vector2 position)
        {
            var motion=motions.FirstOrDefault(m=>ReferenceEquals(m.Nail,nail));
            position=motion!=null?motion.Position:default;
            return motion!=null;
        }
        public bool MotionDestination(NailState nail,out Vector2 target)
        {
            var motion=motions.FirstOrDefault(m=>ReferenceEquals(m.Nail,nail));
            target=motion!=null?Target(motion):default;
            return motion!=null;
        }
        // Read-only UI binding; keeps the source motion/timing calculation unchanged.
        public bool MotionViewTarget(NailState nail,out BoxState box,out int seat,out int hole)
        {
            var motion=motions.FirstOrDefault(m=>ReferenceEquals(m.Nail,nail));
            box=motion?.Box;seat=motion!=null?motion.Seat:-1;hole=motion?.Hole!=null?motion.Hole.Index:-1;return motion!=null;
        }
        // Rendering reads the exact same stage and position that drive gameplay callbacks.
        public bool MotionVisual(NailState nail, out Vector2 position, out float lift, out float angle, out float scale)
        {
            var m=motions.FirstOrDefault(v=>ReferenceEquals(v.Nail,nail));
            position=default;lift=angle=0;scale=.8f;
            if(m==null)return false;
            position=m.Stage==2?Target(m):m.Position;
            float t=Mathf.Clamp01((float)m.Elapsed/.15f);
            lift=m.Stage==0?50*t:m.Stage==1?50:50*(1-t);
            angle=m.Stage==0?540*t:m.Stage==1?540+(float)m.FlightElapsed*3600:540*(1-t);
            scale=m.Stage==0?1:Mathf.Lerp(1,.8f,Mathf.Clamp01((float)(m.FlightElapsed+(m.Stage==2?m.Elapsed:0))/.2f));
            return true;
        }
        public bool SeatSettled(BoxState box,int seat) => seat<box.Screws.Count &&
            !motions.Any(m=>ReferenceEquals(m.Box,box) && m.Seat==seat);
        void AdvanceMotion(Motion m,float delta)
        {
            if(!motions.Contains(m))return;
            m.Elapsed+=delta;
            if(m.Stage==0)
            {
                if(m.Elapsed+1e-8<.2)return;
                m.Stage=1;m.Elapsed=0;OnLifted?.Invoke(m.Nail);return;
            }
            if(m.Stage==1)
            {
                // ComScrew.updateTimer: 3000px/s, moving destination; no invented flight curve.
                m.FlightElapsed+=delta;
                Vector2 target=Target(m);float distance=Vector2.Distance(m.Position,target),step=3000*delta;
                if(distance>step){m.Position=Vector2.MoveTowards(m.Position,target,step);return;}
                m.Position=target;m.Stage=2;m.Elapsed=0;return;
            }
            if(m.Elapsed+1e-8<.2)return;
            motions.Remove(m);DoScrewCount--;
            if(m.Box!=null)m.Box.InFlight--;else m.Hole.Moving=false;
            OnLanded?.Invoke(m.Box!=null);
            m.Callback?.Invoke();
        }
        void MoveBoxes(double delay,bool enter)
        {
            int offset=enter?0:4-Boxes.Count;
            for(int i=0;i<Boxes.Count;i++)
            {
                var box=Boxes[i];if(!enter&&box.Locked)continue;
                box.FromX=box.X(Clock);box.TargetX=BoxPositions[i+offset];box.MoveStart=Clock+delay;box.MoveEnd=Clock+delay+.3;
            }
            // Preserve source doBoxAct's immediate callback at the end of its method.
        }
        BoxState AddBox(int color)
        {
            var box=new BoxState{Color=color};FillCache(box);Boxes.Add(box);return box;
        }
        void EnterBox(int color=0)
        {
            if(color==0 && PendingBoxes.Count>0){color=PendingBoxes[0];PendingBoxes.RemoveAt(0);}
            if(color!=0)
            {
                var box=AddBox(color);MoveBoxes(0,true);
                foreach(var seat in Seats.ToArray())
                    if(!seat.Moving && seat.Screw!=null && seat.Screw.Color==color && box.Screws.Count<3)Transfer(seat,box);
                CheckBoxFull();
            }
            else {MoveBoxes(0,false);foreach(var seat in Seats.ToArray())if(!seat.Moving)CheckHoleToBox(seat);}
        }
        void Transfer(HoleState hole,BoxState box)
        {
            var nail=hole.Screw;Vector2 pos=HolePosition(hole.Index);hole.Screw=null;hole.Moving=false;
            BeginMotion(nail,box,null,pos,()=>{CheckGameFinish();CheckBoxFull();});
        }
        void CheckHoleToBox(HoleState hole)
        {
            if(hole.Screw==null)return;
            var box=Boxes.FirstOrDefault(b=>b.Color==hole.Screw.Color && b.Screws.Count<3);
            if(box!=null)Transfer(hole,box);else CheckGameFinish();
        }
        void CheckBoxFull()
        {
            foreach(var box in Boxes.ToArray().Reverse())
                if(box.Screws.Count==3 && box.InFlight==0)
                {
                    Boxes.Remove(box);box.Closing=true;box.ClosedAt=Clock;ClosingBoxes.Add(box);
                    OnSound?.Invoke("full");Schedule(.2,()=>OnSound?.Invoke("exit"));
                    Cleared+=3;CompletedBoxes++;FullBoxCount++;
                    Schedule(.4,()=>EnterBox());
                    Schedule(.6,()=>{ClosingBoxes.Remove(box);FullBoxCount--;CheckGameFinish();});
                }
        }
        void CheckGameFinish()
        {
            if(Won || Lost || Prompt==SourcePrompt.Revive)return;
            if(Boxes.All(b=>b.Locked) && Remaining==0 && Holes.Count==0 && Cache.Values.Sum()==0 && FullBoxCount==0 && DoScrewCount==0){Won=true;return;}
            if(Seats.Any(s=>s.Screw==null || s.Moving) || FullBoxCount!=0 || DoScrewCount!=0)return;
            int locked=Boxes.Count(b=>b.Locked);
            if(!offeredClear && locked==0){offeredClear=true;Prompt=SourcePrompt.ClearRescue;return;}
            if(locked>0 && PendingBoxes.Count>0){requestedLock=Boxes.Last(b=>b.Locked);Prompt=SourcePrompt.Revive;return;}
            Lost=true;
        }
        void FillCache(BoxState box)
        {
            if(box.Locked)return;
            while(box.Screws.Count<3 && Cache.TryGetValue(box.Color,out int count) && count>0){Cache[box.Color]=count-1;box.Screws.Add(box.Color);}
        }
        void RemoveQueueColor(int color){int i=PendingBoxes.IndexOf(color);if(i>=0)PendingBoxes.RemoveAt(i);}
        int MostCommon(bool includeEmpty)
        {
            var counts=new Dictionary<int,int>();int most=0,best=0;
            foreach(var seat in Seats){int color=seat.Screw?.Color??0;if(color==0&&!includeEmpty)continue;counts.TryGetValue(color,out int n);counts[color]=++n;if(n>best){best=n;most=color;}}
            return most;
        }
        public int MostCommonHole()=>MostCommon(true);
        void Unlock(BoxState box,int color=0,bool remove=true)
        {
            bool removeColor=remove||color==0;
            if(color==0)
            {
                var occupied=Holes;
                color=occupied.Count>0?(Pick!=null?Pick(occupied.ToArray()):occupied[UnityEngine.Random.Range(0,occupied.Count)]):0;
                if(color==0 && PendingBoxes.Count>0){color=PendingBoxes[0];PendingBoxes.RemoveAt(0);removeColor=false;}
            }
            if(removeColor)RemoveQueueColor(color);
            Boxes.Remove(box);if(color!=0)EnterBox(color);
        }
        public bool Request(SourcePrompt prompt,int boxIndex=-1)
        {
            if(!Ready)return false;
            if(prompt==SourcePrompt.Slot && HoleCapacity>=16)return false;
            if(prompt==SourcePrompt.Swap && PendingBoxes.Count==0)return false;
            if(prompt==SourcePrompt.Clear && Holes.Count==0)return false;
            if(prompt==SourcePrompt.Unlock)
            {
                requestedLock=boxIndex>=0 && boxIndex<Boxes.Count?Boxes[boxIndex]:Boxes.FirstOrDefault(b=>b.Locked);
                if(requestedLock==null || !requestedLock.Locked)return false;
            }
            Prompt=prompt;return true;
        }
        public void RewardResult(bool succeeded)
        {
            var prompt=Prompt;Prompt=SourcePrompt.None;
            if(!succeeded){if(prompt==SourcePrompt.ClearRescue)CheckGameFinish();else if(prompt==SourcePrompt.Revive)Lost=true;return;}
            switch(prompt)
            {
                case SourcePrompt.Slot:AddHole();break;
                case SourcePrompt.Swap:ChangeBox();break;
                case SourcePrompt.Hammer:HammerArmed=true;break;
                case SourcePrompt.Clear:case SourcePrompt.ClearRescue:ClearHoles();break;
                case SourcePrompt.Unlock:Unlock(requestedLock,MostCommonHole());break;
                case SourcePrompt.Revive:
                    int color=MostCommonHole();RemoveQueueColor(color);Unlock(requestedLock,color,false);break;
            }
        }
        public void AddHole(){if(HoleCapacity<16)Seats.Add(new HoleState{Index=Seats.Count});}
        public void ClearHoles()
        {
            foreach(var seat in Seats)
            {
                if(seat.Screw!=null){int color=seat.Screw.Color;Cache.TryGetValue(color,out int n);Cache[color]=n+1;}
                seat.Screw=null;seat.Moving=false;
            }
            // Original clearNail only stores in cache; it does NOT immediately fill every active box.
        }
        public void ChangeBox()
        {
            var box=Boxes.Where(b=>!b.Locked).OrderBy(b=>b.Screws.Count).ThenByDescending(b=>Boxes.IndexOf(b)).FirstOrDefault();
            if(box==null)return;
            PendingBoxes.Add(box.Color);
            foreach(int color in box.Screws){Cache.TryGetValue(color,out int n);Cache[color]=n+1;}
            Boxes.Remove(box);int next=MostCommon(false);RemoveQueueColor(next);EnterBox(next);
        }
        public void Hammer(IEnumerable<NailState> affected)
        {
            var colors=new HashSet<int>();
            foreach(var nail in affected.Where(n=>!n.Removed))
            {nail.Removed=true;Cache.TryGetValue(nail.Color,out int n);Cache[nail.Color]=n+1;colors.Add(nail.Color);}
            HammerArmed=false;
            if(PendingBoxes.Count==0){foreach(var box in Boxes.ToArray().Reverse())FillCache(box);CheckBoxFull();return;}
            foreach(int color in colors)
            {
                int budget=PendingBoxes.Count(c=>c==color)*3;
                if(budget==0 || Cache[color]>budget)
                {var box=Boxes.LastOrDefault(b=>b.Color==color);if(box!=null)FillCache(box);}
            }
            CheckBoxFull();
        }
        public void Resolve(){CheckBoxFull();foreach(var seat in Seats.ToArray())if(!seat.Moving)CheckHoleToBox(seat);}
        public void UnlockBox(){var box=Boxes.FirstOrDefault(b=>b.Locked);if(box!=null)Unlock(box,MostCommonHole());}
        public int AccountedScrews => Remaining+Holes.Count+Boxes.Sum(b=>b.Screws.Count)+Cache.Values.Sum()+Cleared;
    }
}
