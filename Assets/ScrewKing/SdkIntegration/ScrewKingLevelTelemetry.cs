using System;
using System.Collections.Generic;
using Game.Core.Domain;
using Game.Core.Services;
using MoodBoost.Sdk.Tracking;

namespace ScrewKing
{
    // Adapts Screw King's model to the base's unchanged event schema and session counters.
    public sealed class ScrewKingLevelTelemetry
    {
        readonly ILevelTracking tracking;
        readonly I_LevelSessionService session;
        readonly List<int> actions=new List<int>();
        LevelTrackingInfo info;
        bool active,ended,pendingLose;
        int total,remaining;
        double playedMs;
        public ScrewKingLevelTelemetry(ILevelTracking tracking,I_LevelSessionService session)
        {this.tracking=tracking;this.session=session;}
        public void Begin(int level,int items,PlayType playType)
        {
            Leave();
            session.SetNextPlayType((E_LevelPlayType)(int)playType);session.BeginPlay(level);
            info=new LevelTrackingInfo(playType,session.PlayIndex,session.LoseIndex,0,"normal");
            total=remaining=items;playedMs=0;active=true;ended=pendingLose=false;actions.Clear();
            tracking.LogLevelStart(info,-1);
        }
        public void Tick(float delta,int itemsRemaining)
        {
            if(!active || ended)return;
            remaining=itemsRemaining;playedMs+=Math.Max(0,delta)*1000;
        }
        public void BoosterUsed(SourcePrompt prompt)
        {
            if(prompt==SourcePrompt.Slot)actions.Add(1);
            else if(prompt==SourcePrompt.Clear || prompt==SourcePrompt.ClearRescue)actions.Add(2);
            else if(prompt==SourcePrompt.Hammer)actions.Add(3);
        }
        public void Won()
        {
            if(!active || ended)return;
            ended=true;pendingLose=false;tracking.LogLevelEnd(info,Stats(),LevelResult.Win);
        }
        public void Lost(){if(active && !ended)pendingLose=true;}
        public void Resumed()
        {
            if(!pendingLose || ended)return;
            pendingLose=false;tracking.LogLevelReopen(info,-1);
        }
        public void Background(bool adShowing){if(!adShowing)FlushLoss();}
        public void Leave()
        {
            if(!active)return;
            FlushLoss();
            if(!ended){ended=true;tracking.LogLevelExit(info,Stats(),session.ExitIndex);session.RecordExit();}
            active=false;
        }
        void FlushLoss()
        {
            if(!pendingLose || ended)return;
            ended=true;pendingLose=false;tracking.LogLevelEnd(info,Stats(),LevelResult.Lose,"no_move");session.RecordLoss();
        }
        LevelPlayStats Stats()=>new LevelPlayStats(-1,-1,-1,(long)playedMs,total,total-remaining,TrackingValues.Sequence(actions));
    }
}
