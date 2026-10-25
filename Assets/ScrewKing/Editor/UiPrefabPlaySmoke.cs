using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing.Editor
{
    [InitializeOnLoad]
    public static class UiPrefabPlaySmoke
    {
        const string Active="ScrewKing.UiPrefabSmoke";
        static UiPrefabPlaySmoke(){if(SessionState.GetBool(Active,false))Install();}
        static void Install(){EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;}
        public static void Run()
        {
            SessionState.SetBool("ScrewKing.Smoke.PlatformHad",PlayerPrefs.HasKey(TikTokCapabilities.LocalSaveKey));SessionState.SetString("ScrewKing.Smoke.PlatformSave",PlayerPrefs.GetString(TikTokCapabilities.LocalSaveKey,""));
            UiPrefabBuilder.GenerateAndValidate();
            var files=Directory.GetFiles(UiPrefabBuilder.Folder,"*.prefab",SearchOption.AllDirectories);var hashes=files.Select(Hash).ToArray();
            UiPrefabBuilder.CreateMissing();
            if(!hashes.SequenceEqual(files.Select(Hash)))throw new Exception("CreateMissing overwrote existing UI prefabs");
            PortBuilder.MakeScene();SessionState.SetBool(Active,true);Install();EditorApplication.isPlaying=true;
        }
        static string Hash(string path){using(var sha=SHA256.Create())return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));}
        static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("UI prefab checks").AddComponent<UiPrefabSmokeRunner>();}
        public static void Finish(bool passed,string message)
        {
            if(SessionState.GetBool("ScrewKing.Smoke.PlatformHad",false))PlayerPrefs.SetString(TikTokCapabilities.LocalSaveKey,SessionState.GetString("ScrewKing.Smoke.PlatformSave",""));else PlayerPrefs.DeleteKey(TikTokCapabilities.LocalSaveKey);PlayerPrefs.Save();
            SessionState.SetBool(Active,false);Directory.CreateDirectory("../Builds/NewArtUI");
            File.WriteAllText("../Builds/NewArtUI/prefab-play-smoke.json",JsonUtility.ToJson(new Report{passed=passed,message=message},true));
            Debug.Log("UI_PREFAB_PLAY_SMOKE "+passed+" "+message);EditorApplication.Exit(passed?0:1);
        }
        [Serializable] sealed class Report{public bool passed;public string message;}
    }
    public sealed class UiPrefabSmokeRunner:MonoBehaviour
    {
        readonly Dictionary<string,int?> saved=new Dictionary<string,int?>();
        string failure;
        PortGame game;
        ScrewKingUiController ui;
        void Awake()
        {
            foreach(var key in new[]{"ScrewKing.Currency","ScrewKing.Sound","ScrewKing.Music","ScrewKing.Vibration","ScrewKingPort.Level","Game.LevelSessionLevel","Game.LevelPlayIndex","Game.LevelLoseIndex","Game.LevelExitIndex","Game.LevelPlayType"})saved[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key):(int?)null;
            Application.logMessageReceived+=Log;
        }
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=text;}
        bool Check(bool ok,string description){if(ok&&failure==null)return true;Finish(false,description+" "+failure);return false;}
        void Click(ScrewKingUiView view,ScrewKingUiCommand command){view.GetComponentsInChildren<ScrewKingUiAction>(true).First(a=>a.Command==command).GetComponent<Button>().onClick.Invoke();}
        IEnumerator Start()
        {
            yield return null;game=FindFirstObjectByType<PortGame>();ui=game.UiRoot;
            if(!Check(ui!=null,"scene has an editable UI instance"))yield break;
            yield return new WaitForSecondsRealtime(1.3f);
            if(!Check(ui.Home.gameObject.activeSelf&&!ui.Loading.gameObject.activeSelf,"home / loading state"))yield break;
            var play=ui.Home.GetComponentsInChildren<ScrewKingUiAction>().First(a=>a.Command==ScrewKingUiCommand.Play).GetComponent<RectTransform>();
            play.anchoredPosition+=new Vector2(17,9);Vector2 edited=play.anchoredPosition;
            if(!Check(ui.Home.CounterLabel.text==game.UiCurrency.ToString()&&play.anchoredPosition==edited,"home binds currency, preserves authored layout"))yield break;
            if(!Check(!ui.Home.GetComponentsInChildren<ScrewKingUiAction>().Any(a=>a.Command==ScrewKingUiCommand.Previous||a.Command==ScrewKingUiCommand.NextSelection),"demo has no level selection arrows"))yield break;
            Click(ui.Home,ScrewKingUiCommand.Settings);yield return null;
            if(!Check(game.UiSettingsOpen&&ui.Settings.gameObject.activeSelf,"settings button"))yield break;
            var title=ui.Settings.GetComponentsInChildren<Text>().First(t=>t.name=="Title");title.text="Custom title";
            Click(ui.Settings,ScrewKingUiCommand.MusicOff);Click(ui.Settings,ScrewKingUiCommand.SoundOn);Click(ui.Settings,ScrewKingUiCommand.VibrationOff);yield return null;
            var music=ui.Settings.Switches.First(t=>t.Setting==ScrewKingUiSetting.Music);
            if(!Check(!game.UiSetting(ScrewKingUiSetting.Music)&&game.UiSetting(ScrewKingUiSetting.Sound)&&!game.UiSetting(ScrewKingUiSetting.Vibration)&&music.OffHighlight.activeSelf&&!music.OnHighlight.activeSelf&&title.text=="Custom title"&&PlayerPrefs.GetInt("ScrewKing.Music")==0,"independent settings, highlights, persistence and static labels"))yield break;
            Click(ui.Settings,ScrewKingUiCommand.Resume);yield return null;
            int currencyBefore=game.UiCurrency;
            game.StartLevel(1,12345);yield return new WaitForSecondsRealtime(1.5f);
            if(!Check(!game.UiIsHome&&game.Model!=null&&game.Model.Nails.Count==9,"home Play button"))yield break;
            var hud=ui.Gameplay;
            if(!Check(hud.IsAuthored&&hud.Boxes.Length==4&&hud.BufferSlots.Length==16,"authored HUD widget references"))yield break;
            var slotButton=hud.GetComponentsInChildren<ScrewKingUiAction>(true).First(a=>a.Command==ScrewKingUiCommand.AddSlot).GetComponent<Button>();
            var rect=slotButton.GetComponent<RectTransform>();rect.anchoredPosition+=new Vector2(13,7);Vector2 editedHudPosition=rect.anchoredPosition;
            var caption=slotButton.GetComponentInChildren<Text>();caption.text="Custom slot";
            yield return null;yield return null;
            if(!Check(rect.anchoredPosition==editedHudPosition&&caption.text=="Custom slot","HUD static layout/text edits preserved"))yield break;
            slotButton.onClick.Invoke();yield return null;yield return null;
            if(!Check(game.Model.Prompt==SourcePrompt.Slot&&ui.BuyBooster.gameObject.activeSelf&&!ui.Reward.gameObject.activeSelf,"authored booster OnClick opens PopupBuyBooster"))yield break;
            Click(ui.BuyBooster,ScrewKingUiCommand.CancelReward);yield return null;
            game.UiInvoke(ScrewKingUiCommand.Settings);yield return null;double clock=game.Model.Clock;yield return new WaitForSecondsRealtime(.3f);
            if(!Check(Math.Abs(game.Model.Clock-clock)<.0001,"settings pauses gameplay"))yield break;
            Click(ui.Settings,ScrewKingUiCommand.Resume);
            yield return new WaitForSecondsRealtime(.7f);
            // Tutorial must track the actual rendered screw, independently of the HUD layout.
            if(!Check(game.UiGuideScreenTarget(out var tutorialPoint)&&hud.Guide.gameObject.activeSelf,"level 1 guide targets a clickable screw"))yield break;
            var hudRect=(RectTransform)hud.transform;var hudPosition=hudRect.anchoredPosition;
            hudRect.anchoredPosition+=new Vector2(31,-19);
            float guideDeadline=Time.unscaledTime+.35f;
            while(Time.unscaledTime<guideDeadline){
                yield return null;
                if(!Check(game.UiGuideScreenTarget(out tutorialPoint),"guide remains available"))yield break;
                Vector2 shownPoint=RectTransformUtility.WorldToScreenPoint(null,hud.Guide.Motion.position);
                Vector2 circlePoint=RectTransformUtility.WorldToScreenPoint(null,hud.Guide.Circle.TransformPoint(hud.Guide.Circle.rect.center));
                if(!Check(Vector2.Distance(shownPoint,tutorialPoint)<.15f&&Vector2.Distance(circlePoint,tutorialPoint)<.15f,"guide/circle remain centred while pulsing and HUD moved"))yield break;
                if(!Check(!ui.BlocksPointer(tutorialPoint),"tutorial graphics do not block the screw"))yield break;
            }
            hudRect.anchoredPosition=hudPosition;yield return null;
            Vector2 tutorialWorld=Camera.main.ScreenToWorldPoint(tutorialPoint);
            var probe=game.Model.Nails.Where(n=>!n.Removed).OrderBy(n=>Vector2.Distance(game.Model.NailPosition(n)*.01f,tutorialWorld)).FirstOrDefault();
            if(probe!=null&&!game.TryTake(probe))probe=null;
            if(!Check(probe!=null,"flight probe accepted"))yield break;
            game.Model.MotionViewTarget(probe,out var targetBox,out int targetSeat,out int targetHole);
            if(!Check(targetBox!=null,"probe targets a box slot"))yield break;
            var destination=hud.Boxes[game.Model.Boxes.IndexOf(targetBox)].Seat(targetSeat);
            var oldSeatPosition=destination.anchoredPosition;destination.anchoredPosition+=new Vector2(17,-9);
            bool reachedAuthoredSlot=false;float flightDeadline=Time.unscaledTime+2;
            while(game.Model.MotionVisual(probe,out var point,out _,out _,out _)&&Time.unscaledTime<flightDeadline){
                game.Model.MotionDestination(probe,out var target);
                if(Vector2.Distance(point,target)<.01f){
                    var view=hud.GetComponentInChildren<ScrewKingFlyingScrewView>();
                    if(view&&Vector3.Distance(view.Motion.position,destination.TransformPoint(destination.rect.center))<.1f)reachedAuthoredSlot=true;
                }
                yield return null;
            }
            destination.anchoredPosition=oldSeatPosition;
            if(!Check(game.UiGuideScreenTarget(out var nextTutorialPoint)&&Vector2.Distance(nextTutorialPoint,tutorialPoint)>1,"tutorial advances after its screw is removed"))yield break;
            if(!Check(reachedAuthoredSlot,"flight destination follows a manually moved prefab seat"))yield break;
            float deadline=Time.unscaledTime+24;
            while(!game.Model.Won&&Time.unscaledTime<deadline){
                bool took=false;
                foreach(var nail in game.Model.Nails.Where(n=>!n.Removed).OrderByDescending(n=>n.Layer).ToArray())if(game.TryTake(nail)){took=true;break;}
                yield return new WaitForSecondsRealtime(took?.45f:.25f);
            }
            yield return new WaitForSecondsRealtime(1);
            if(!Check(!game.UiGuideScreenTarget(out _)&&!hud.Guide.gameObject.activeSelf,"tutorial ends after its board is removed"))yield break;
            if(!Check(game.Model.Won&&ui.Win.gameObject.activeSelf&&ui.Win.CounterLabel.text==game.UiCurrency.ToString()&&ui.Win.StatusLabel.text=="+20"&&ui.Win.GetComponentsInChildren<ScrewKingUiAction>().Any(a=>a.Command==ScrewKingUiCommand.DoubleReward),"real level win projects into PopupWin with wallet/+20/x2"))yield break;
            if(!Check(game.UiCurrency==currencyBefore+20&&PlayerPrefs.GetInt(PortGame.CurrencyKey)==game.UiCurrency,"win grants and saves exactly 20 currency"))yield break;
            yield return new WaitForSecondsRealtime(.5f);
            if(!Check(game.UiCurrency==currencyBefore+20,"win reward is not repeated each frame"))yield break;
            // Simulate SDK outcomes at the game callback boundary, without changing SDK providers.
            const System.Reflection.BindingFlags flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var round=(ScrewKingRoundReward)typeof(PortGame).GetField("roundReward",flags).GetValue(game);
            var complete=typeof(PortGame).GetMethod("CompleteDoubleReward",flags);
            int rewardVersion=(int)typeof(PortGame).GetField("generation",flags).GetValue(game);
            var wonModel=game.Model;
            if(!Check(round.BeginDouble(out int cancelled),"begin simulated double request"))yield break;
            complete.Invoke(game,new object[]{cancelled,false,rewardVersion,wonModel});yield return null;
            if(!Check(game.UiCurrency==currencyBefore+20&&!game.UiDoubleRewardClaimed&&!game.UiDoubleRewardPending,"cancelled double pays nothing"))yield break;
            if(!Check(round.BeginDouble(out int completed),"retry simulated double request"))yield break;
            complete.Invoke(game,new object[]{completed,true,rewardVersion,wonModel});yield return null;
            if(!Check(game.UiCurrency==currencyBefore+40&&game.UiDoubleRewardClaimed&&ui.Win.StatusLabel.text=="+40"&&ui.Win.CounterLabel.text==game.UiCurrency.ToString()&&!ui.Win.GetComponentsInChildren<ScrewKingUiAction>().First(a=>a.Command==ScrewKingUiCommand.DoubleReward).GetComponent<Button>().interactable,"completed double saves +40 total, updates UI and disables x2"))yield break;
            complete.Invoke(game,new object[]{completed,true,rewardVersion,wonModel});
            if(!Check(game.UiCurrency==currencyBefore+40,"duplicate completion pays nothing"))yield break;
            Click(ui.Win,ScrewKingUiCommand.NextLevel);yield return new WaitForSecondsRealtime(1);
            if(!Check(game.UiLevel==2&&game.Model.Nails.Count==120,"Next level button preserves game/SDK callback"))yield break;
            complete.Invoke(game,new object[]{completed,true,rewardVersion,wonModel});
            if(!Check(game.UiCurrency==currencyBefore+40&&!game.UiDoubleRewardClaimed,"late callback cannot reward a new round"))yield break;
            var before=game.Model;game.Model.Request(SourcePrompt.Slot);yield return null;
            if(!Check(ui.BuyBooster.gameObject.activeSelf,"booster prompt uses prefab"))yield break;
            Click(ui.BuyBooster,ScrewKingUiCommand.CancelReward);yield return null;
            if(!Check(game.Model.Prompt==SourcePrompt.None&&game.Model.HoleCapacity==5,"cancel grants nothing"))yield break;
            // Inject only the presentation state; gameplay loss rules are not under test here.
            typeof(PortModel).GetProperty("Lost").SetValue(game.Model,true);yield return null;yield return null;
            if(!Check(ui.Lose.gameObject.activeSelf,"loss state projects into PopupLose"))yield break;
            Click(ui.Lose,ScrewKingUiCommand.Retry);yield return null;
            if(!Check(game.UiLevel==2&&!ReferenceEquals(before,game.Model)&&!game.Model.Lost,"Retry button"))yield break;
            game.UiInvoke(ScrewKingUiCommand.Settings);yield return null;Click(ui.Settings,ScrewKingUiCommand.Quit);yield return null;
            if(!Check(game.UiIsHome&&ui.Home.gameObject.activeSelf,"Quit returns home"))yield break;
            if(!Check(game.UiCurrency==currencyBefore+40&&ui.Home.CounterLabel.text==game.UiCurrency.ToString(),"retry, loss and home do not grant currency"))yield break;
            Finish(true,"Tutorial target/circle alignment, moved HUD, click passthrough, advance/end; Currency save/once-per-win/no loss payout; simulated double callback cancelled/completed/duplicate/stale and +40 UI; demo Home/HUD alignment; Persistent prefab button events, loading/home, settings and pause, manual transform/static text preservation including HUD widgets, authored flight target, actual level-1 win/next, reward cancel, injected loss view/retry and quit passed. Existing prefab hashes unchanged.");
        }
        void Finish(bool passed,string message)
        {
            foreach(var item in saved){if(item.Value.HasValue)PlayerPrefs.SetInt(item.Key,item.Value.Value);else PlayerPrefs.DeleteKey(item.Key);}PlayerPrefs.Save();
            Application.logMessageReceived-=Log;UiPrefabPlaySmoke.Finish(passed,message);
        }
    }
}
