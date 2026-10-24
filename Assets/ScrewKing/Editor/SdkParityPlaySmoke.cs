using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk.Unity;
using UnityEditor;
using UnityEngine;

namespace ScrewKing.Editor
{
    [InitializeOnLoad]
    public static class SdkParityPlaySmoke
    {
        const string Active="ScrewKing.SdkParitySmoke.Active";
        static SdkParityPlaySmoke(){if(SessionState.GetBool(Active,false))Install();}
        public static void Run(){TikTokBuilder.EnsureSdkSettings();PortBuilder.MakeScene();SessionState.SetBool(Active,true);Install();EditorApplication.isPlaying=true;}
        static void Install(){EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;}
        static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("SDK parity verification").AddComponent<SdkSmokeRunner>();}
        public static void Finish(bool passed,string message)
        {
            Directory.CreateDirectory("../Builds/TikTok");
            File.WriteAllText("../Builds/TikTok/sdk-play-smoke.json",JsonUtility.ToJson(new Report{passed=passed,message=message},true));
            SessionState.SetBool(Active,false);Debug.Log("SDK_PLAY_SMOKE "+passed+" "+message);EditorApplication.Exit(passed?0:1);
        }
        [Serializable] sealed class Report{public bool passed;public string message;}
    }
    public sealed class SdkSmokeRunner:MonoBehaviour
    {
        readonly List<string> tracking=new List<string>();
        readonly List<AdCompletion> ads=new List<AdCompletion>();
        readonly Dictionary<string,int?> saved=new Dictionary<string,int?>();
        string failure;
        TikTokPlatform platform;
        void Awake(){Application.logMessageReceived+=Log;foreach(var key in new[]{"ScrewKingPort.Level","Game.LevelSessionLevel","Game.LevelPlayIndex","Game.LevelLoseIndex","Game.LevelExitIndex","Game.LevelPlayType"})saved[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null;}
        void Log(string text,string stack,LogType type){if(text.Contains("[SDK/Tracking]"))tracking.Add(text);if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)failure=text;}
        IEnumerator Start()
        {
            var game=FindFirstObjectByType<PortGame>();platform=game.GetComponent<TikTokPlatform>();
            yield return Wait(()=>platform.BootReady && platform.Context.Runtime.IsReady && platform.Ready,15);
            if(!Check(failure==null && platform.Ready,"base SDK ready"))yield break;
            platform.Context.AdEvents.Completed+=AdFinished;
            if(!Check(ReferenceEquals(platform.Context,MoodBoostSdk.GetOrCreate(game.SdkSettings)),"single SDK context"))yield break;
            game.StartLevel(1,12345);yield return new WaitForSeconds(.8f);
            if(!Check(game.Model.Request(SourcePrompt.Slot),"slot request"))yield break;
            yield return null;game.WatchRewarded();
            yield return Wait(()=>FakeShowing(),5);SetFakeOutcome(AdResult.Skipped);
            yield return Wait(()=>!platform.Showing,5);
            if(!Check(game.Model.HoleCapacity==5 && ads.Last().Result==AdResult.Skipped,"skipped ad grants nothing"))yield break;
            yield return Wait(()=>platform.Ready,5);
            game.Model.Request(SourcePrompt.Slot);yield return null;game.WatchRewarded();
            yield return Wait(()=>FakeShowing(),5);
            yield return new WaitForSecondsRealtime(3.5f);SetFakeOutcome(AdResult.Completed);
            yield return Wait(()=>!platform.Showing,5);
            if(!Check(game.Model.HoleCapacity==6 && ads.Last().Result==AdResult.Completed,"completed ad grants one slot"))yield break;
            if(!Check(!AudioListener.pause,"audio restored"))yield break;
            // Play the actual level through PortGame.TryTake, respecting its cover checks.
            for(int i=0;i<9;i++){
                NailState nail=null;float until=Time.realtimeSinceStartup+8;
                while(nail==null && Time.realtimeSinceStartup<until){
                    foreach(var candidate in game.Model.Nails.Where(n=>!n.Removed).OrderByDescending(n=>n.Layer).ThenByDescending(n=>n.Id).ToArray()){if(game.TryTake(candidate)){nail=candidate;break;}}
                    if(nail==null)yield return new WaitForSeconds(.25f);
                }
                if(!Check(nail!=null,"take screw "+i))yield break;
                yield return new WaitForSeconds(1);
            }
            yield return new WaitForSeconds(1);
            if(!Check(game.Model.Won,"win after SDK integration"))yield break;
            bool interstitialReturned=false;platform.ShowLevelComplete(()=>interstitialReturned=true);
            yield return Wait(()=>interstitialReturned,5);
            if(!Check(ads.Last().Format==AdFormat.Interstitial && ads.Last().Result==AdResult.Blocked,"base first-level interstitial gate"))yield break;
            if(!Check(tracking.Count(x=>x.Contains("level_start {"))==1 && tracking.Count(x=>x.Contains("level_end {") && x.Contains("result=win"))==1,"one start and one win event"))yield break;
            if(!Check(tracking.Any(x=>x.Contains("ad_offer_shown")) && tracking.Any(x=>x.Contains("ad_complete")),"base ad tracking bridge"))yield break;
            if(!Check(failure==null,"console clean"))yield break;
            Finish(true,"Base singleton, fake rewarded skip/completed, single reward, audio restore, interstitial gate, level_start/end and ad tracking passed. No live TikTok traffic tested.");
        }
        IEnumerator Wait(Func<bool> condition,float seconds){float until=Time.realtimeSinceStartup+seconds;while(!condition() && Time.realtimeSinceStartup<until)yield return null;}
        static UnityEngine.Object FakeScreen()=>Resources.FindObjectsOfTypeAll<MonoBehaviour>().FirstOrDefault(x=>x && x.GetType().FullName=="MoodBoost.Sdk.Ads.Providers.FakeAdScreen");
        static bool FakeShowing(){var x=FakeScreen();return x && (bool)x.GetType().GetField("showing",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(x);}
        static void SetFakeOutcome(AdResult value){var x=FakeScreen();if(x)x.GetType().GetField("outcome",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(x,value);}
        void AdFinished(AdCompletion value){ads.Add(value);}
        bool Check(bool passed,string message){if(passed)return true;Finish(false,message+" "+failure);return false;}
        void Finish(bool pass,string message)
        {
            if(platform!=null){platform.Context.AdEvents.Completed-=AdFinished;platform.Telemetry.Leave();}
            foreach(var entry in saved){if(entry.Value.HasValue)PlayerPrefs.SetInt(entry.Key,entry.Value.Value);else PlayerPrefs.DeleteKey(entry.Key);}PlayerPrefs.Save();
            Application.logMessageReceived-=Log;
            File.WriteAllLines("../Builds/TikTok/sdk-tracking-events.log",tracking);
            SdkParityPlaySmoke.Finish(pass,message);
        }
    }
}
