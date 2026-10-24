using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Game.Core.Domain;
using Game.Core.Services;
using MoodBoost.Sdk;
using MoodBoost.Sdk.Tracking;

namespace ScrewKing.Editor
{
    public static class SdkTrackingVerification
    {
        public static void Run()
        {
            var recording=new Recorder();
            using(var service=new TrackingService(new[]{recording})){
                service.InitializeAsync(CancellationToken.None).GetAwaiter().GetResult();
                int level=7;var player=new PlayerLevel();player.SetProvider(()=>level);
                var session=new MemorySession();
                var telemetry=new ScrewKingLevelTelemetry(new LevelTracking(service,player),session);
                telemetry.Begin(level,9,PlayType.Home);telemetry.Tick(.5f,6);telemetry.BoosterUsed(SourcePrompt.Clear);
                telemetry.Lost();telemetry.Resumed();telemetry.Tick(.25f,0);telemetry.Won();telemetry.Won();telemetry.Leave();
                Require(recording.Events.Select(e=>e.Name).SequenceEqual(new[]{"level_start","level_reopen","level_end"}),"one terminal result after revive");
                var end=recording.Events.Last().Data;
                Require(end["level"]=="7" && end["mode"]=="normal" && end["play_index"]=="1","base identity");
                Require(end["duration_play"]=="750" && end["items_cleared"]=="9" && end["action_seq"]=="2","elapsed time and boosters");
                Require(end["result"]=="win" && session.LoseIndex==0,"revived loss not counted");
                telemetry.Begin(level,9,PlayType.Restart);telemetry.Lost();telemetry.Leave();
                Require(recording.Events.Last().Data["result"]=="lose" && session.LoseIndex==1,"terminal loss counted once");
                telemetry.Begin(level,9,PlayType.Restart);telemetry.Tick(.1f,8);telemetry.Leave();
                Require(recording.Events.Last().Name=="level_exit" && session.ExitIndex==1,"abandon is exit");
                level=8;telemetry.Begin(level,12,PlayType.Next);telemetry.Lost();telemetry.Background(true);telemetry.Resumed();telemetry.Won();telemetry.Leave();
                Require(recording.Events.Last().Data["level"]=="8" && recording.Events.Last().Data["play_index"]=="1","next level identity resets");
                Require(session.LoseIndex==0,"ad pause does not commit loss");
                level=9;telemetry.Begin(level,12,PlayType.Next);telemetry.Lost();telemetry.Background(false);telemetry.Leave();
                Require(recording.Events.Last().Name=="level_end" && recording.Events.Last().Data["result"]=="lose","background flushes held loss");
            }
            Directory.CreateDirectory("../Builds/TikTok");File.WriteAllText("../Builds/TikTok/sdk-tracking-verification.json","{\"passed\":true,\"uses_base_tracking_service\":true,\"checks\":[\"identity\",\"single-terminal-event\",\"revive\",\"duration\",\"booster-sequence\",\"exit\",\"background\",\"level-reset\"]}");
            UnityEngine.Debug.Log("SDK_TRACKING_VERIFIED");
        }
        static void Require(bool pass,string message){if(!pass)throw new Exception(message);}
        sealed class Entry{public string Name;public Dictionary<string,string> Data;}
        sealed class Recorder:ITrackingProvider
        {
            public readonly List<Entry> Events=new List<Entry>();public string Name=>"recorder";
            public UniTask<bool> InitializeAsync(CancellationToken ct)=>UniTask.FromResult(true);
            public void LogEvent(string name,in TrackingParams parameters){var data=new Dictionary<string,string>();for(int i=0;i<parameters.Count;i++)data.Add(parameters.GetKey(i),parameters.GetValue(i).ToString());Events.Add(new Entry{Name=name,Data=data});}
            public void LogAdRevenue(in AdRevenue revenue){} public void SetUserId(string id){} public void SetUserProperty(string key,string value){} public void LogError(Exception error,string context){} public void Dispose(){}
        }
        sealed class MemorySession:I_LevelSessionService
        {
            public int Level{get;private set;}public int PlayIndex{get;private set;}public int LoseIndex{get;private set;}public int ExitIndex{get;private set;}public E_LevelPlayType PlayType{get;private set;}
            public void SetNextPlayType(E_LevelPlayType type){PlayType=type;}
            public void BeginPlay(int level){if(Level!=level){Level=level;PlayIndex=LoseIndex=ExitIndex=0;}PlayIndex++;}
            public void RecordLoss(){LoseIndex++;}public void RecordExit(){ExitIndex++;}
        }
    }
}
