using System;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing.Editor
{
    [InitializeOnLoad]
    public static class TikTokCapabilitiesPlaySmoke
    {
        const string Active="ScrewKing.CapabilitiesSmoke";
        static TikTokCapabilitiesPlaySmoke(){if(SessionState.GetBool(Active,false))Install();}
        static void Install(){EditorApplication.playModeStateChanged-=State;EditorApplication.playModeStateChanged+=State;}
        public static void Run()
        {
            TikTokCapabilitiesBuilder.CreateAndVerify();
            SessionState.SetBool("ScrewKing.CapsHadSave",PlayerPrefs.HasKey(TikTokCapabilities.LocalSaveKey));SessionState.SetString("ScrewKing.CapsSave",PlayerPrefs.GetString(TikTokCapabilities.LocalSaveKey,""));
            foreach(var key in new[]{PortGame.CurrencyKey,"ScrewKingPort.Level","ScrewKing.Sound","ScrewKing.Music","ScrewKing.Vibration"}){
                SessionState.SetBool("CapsHas"+key,PlayerPrefs.HasKey(key));SessionState.SetInt("CapsInt"+key,PlayerPrefs.GetInt(key,0));
            }
            PortBuilder.MakeScene();SessionState.SetBool(Active,true);Install();EditorApplication.isPlaying=true;
        }
        static void State(PlayModeStateChange state){if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("TikTok capability checks").AddComponent<TikTokCapabilitiesSmokeRunner>();}
        public static void Finish(bool passed,string message)
        {
            SessionState.SetBool(Active,false);
            foreach(var key in new[]{PortGame.CurrencyKey,"ScrewKingPort.Level","ScrewKing.Sound","ScrewKing.Music","ScrewKing.Vibration"}){
                if(SessionState.GetBool("CapsHas"+key,false))PlayerPrefs.SetInt(key,SessionState.GetInt("CapsInt"+key,0));else PlayerPrefs.DeleteKey(key);
            }
            if(SessionState.GetBool("ScrewKing.CapsHadSave",false))PlayerPrefs.SetString(TikTokCapabilities.LocalSaveKey,SessionState.GetString("ScrewKing.CapsSave",""));else PlayerPrefs.DeleteKey(TikTokCapabilities.LocalSaveKey);
            PlayerPrefs.Save();Directory.CreateDirectory("../Builds/TikTok");File.WriteAllText("../Builds/TikTok/capability-play-tests.json",JsonUtility.ToJson(new Result{passed=passed,message=message},true));
            Debug.Log("TIKTOK_CAPABILITY_PLAY_SMOKE "+passed+" "+message);EditorApplication.Exit(passed?0:1);
        }
        [Serializable] sealed class Result{public bool passed;public string message;}
    }
    public sealed class FakeCapabilitiesApi:ITikTokMiniGameApi
    {
        public bool IsLive=>true;
        public event Action Resumed;
        public bool ShortcutEligible,RevisitEligible,Writable=true,DelayNavigation;
        public int Reports;
        public string Json="";
        public Action<bool> NavigationCallback;
        public void Initialize(Action<bool> result)=>result(true);
        public bool Supports(TikTokMission mission)=>true;
        public void Navigate(TikTokMission mission,Action<bool> result){if(DelayNavigation)NavigationCallback=result;else result(true);}
        public void CheckReward(TikTokMission mission,Action<bool,bool> result)=>result(true,mission==TikTokMission.Shortcut?ShortcutEligible:RevisitEligible);
        public void ReportGranted(TikTokMission mission,Action<bool> result){Reports++;if(mission==TikTokMission.Shortcut)ShortcutEligible=false;else RevisitEligible=false;result(true);}
        public string LoadSave()=>Json;
        public bool Save(string json){if(!Writable)return false;Json=json;return true;}
        public void Resume()=>Resumed?.Invoke();
        public void Dispose(){Resumed=null;}
    }
    public sealed class TikTokCapabilitiesSmokeRunner:MonoBehaviour
    {
        string failure;
        bool done;
        void Awake(){Application.logMessageReceived+=Log;}
        void Log(string text,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)failure=text;}
        bool Check(bool condition,string message)
        {
            if(condition&&failure==null)return true;
            End(false,message+" "+failure);return false;
        }
        void End(bool passed,string message){if(done)return;done=true;Application.logMessageReceived-=Log;TikTokCapabilitiesPlaySmoke.Finish(passed,message);}
        IEnumerator Start()
        {
            yield return new WaitForSecondsRealtime(1.4f);
            var game=FindFirstObjectByType<PortGame>();var ui=game.UiRoot;var features=game.PlatformFeatures;
            var settings=ScriptableObject.CreateInstance<TikTokSettings>();settings.AppId="test-public-id";settings.TermsUrl="https://unit.invalid/terms";settings.PrivacyUrl="https://unit.invalid/privacy";settings.AutoGuideAfterTutorial=false;
            var api=new FakeCapabilitiesApi();features.Configure(game,settings,api);yield return null;yield return null;yield return null;
            if(!Check(features.Ready,"host and native save ready without login"))yield break;
            int coins=game.UiCurrency;
            settings.AutoGuideAfterTutorial=true;features.TutorialCompleted();features.PumpHomeGuide(false);
            if(!Check(!features.ModalOpen,"tutorial follow-up waits until Home is unobstructed"))yield break;
            features.PumpHomeGuide(true);yield return null;
            if(!Check(features.OpenMission==TikTokMission.Shortcut&&features.Ledger.Data.introSeen,"one-time tutorial follow-up opens shortcut guide"))yield break;
            features.Close();features.PumpHomeGuide(true);
            if(!Check(!features.ModalOpen,"tutorial follow-up does not spam after closing"))yield break;
            settings.AutoGuideAfterTutorial=false;
            ui.Invoke(ScrewKingUiCommand.TikTokShortcut);yield return null;
            if(!Check(ui.Shortcut.gameObject.activeSelf&&!ui.Revisit.gameObject.activeSelf&&ui.Shortcut.AmountLabel.text=="+100 screws","Home shortcut entry opens editable popup"))yield break;
            ui.Shortcut.PrimaryButton.onClick.Invoke();yield return null;yield return null;
            if(!Check(game.UiCurrency==coins&&!features.Eligible(TikTokMission.Shortcut),"navigation success does not grant reward"))yield break;
            api.ShortcutEligible=true;ui.Shortcut.CheckButton.onClick.Invoke();yield return null;
            if(!Check(features.Eligible(TikTokMission.Shortcut)&&ui.Shortcut.PrimaryLabel.text=="Get reward","positive platform eligibility enables claim"))yield break;
            ui.Shortcut.PrimaryButton.onClick.Invoke();ui.Shortcut.PrimaryButton.onClick.Invoke();yield return null;
            if(!Check(game.UiCurrency==coins+100&&features.Claimed(TikTokMission.Shortcut)&&!ui.Shortcut.PrimaryButton.interactable&&api.Reports==1,"shortcut pays once, reports the grant and updates wallet/button"))yield break;
            ui.Invoke(ScrewKingUiCommand.TikTokMissionClose);ui.Invoke(ScrewKingUiCommand.TikTokRevisit);yield return null;
            api.RevisitEligible=true;features.Check(TikTokMission.Revisit);yield return null;
            api.Writable=false;
            if(!Check(!features.Claim(TikTokMission.Revisit)&&game.UiCurrency==coins+100,"save failure pays nothing and remains retryable"))yield break;
            api.Writable=true;ui.Revisit.PrimaryButton.onClick.Invoke();yield return null;
            if(!Check(game.UiCurrency==coins+150&&features.Claimed(TikTokMission.Revisit)&&api.Reports==2,"revisit reward, grant report and wallet"))yield break;
            string saved=api.Json;var restarted=new FakeCapabilitiesApi{Json=saved};features.Configure(game,settings,restarted);yield return null;yield return null;yield return null;
            if(!Check(game.UiCurrency==coins+150&&features.Claimed(TikTokMission.Shortcut)&&features.Claimed(TikTokMission.Revisit),"restart preserves credits and both receipts"))yield break;
            restarted.Resume();yield return null;
            if(!Check(game.UiCurrency==coins+150,"foreground return does not auto-pay a duplicate reward"))yield break;
            var arriving=new FakeCapabilitiesApi{ShortcutEligible=true};features.Configure(game,settings,arriving);yield return null;yield return null;yield return null;features.PumpHomeGuide(true);yield return null;
            if(!Check(features.OpenMission==TikTokMission.Shortcut&&ui.Shortcut.gameObject.activeSelf&&ui.Shortcut.PrimaryLabel.text=="Get reward","launch from the home screen icon opens its reward popup"))yield break;
            features.Close();features.PumpHomeGuide(true);yield return null;
            if(!Check(!features.ModalOpen,"arrival popup opens once per session"))yield break;
            var staleApi=new FakeCapabilitiesApi{DelayNavigation=true};features.Configure(game,settings,staleApi);yield return null;yield return null;yield return null;
            features.Primary(TikTokMission.Shortcut);var stale=staleApi.NavigationCallback;
            if(!Check(features.Busy&&stale!=null,"delayed native navigation"))yield break;
            typeof(TikTokCapabilities).GetField("deadline",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(features,Time.realtimeSinceStartup-1);
            yield return null;stale(true);yield return null;
            if(!Check(!features.Busy&&!features.Eligible(TikTokMission.Shortcut),"timeout ignores late navigation callback"))yield break;
            features.Configure(game,settings,new PreviewTikTokApi());stale(true);yield return null;
            if(!Check(!features.Busy&&!features.Ready&&!features.Eligible(TikTokMission.Shortcut),"late callback from disposed host cannot create eligibility or pay reward"))yield break;
            if(!Check(features.LegalConfigured(true)&&features.LegalConfigured(false),"legal links route only configured HTTPS URLs"))yield break;
            ui.Invoke(ScrewKingUiCommand.TikTokRevisit);yield return null;
            if(!Check(ui.Revisit.gameObject.activeSelf&&!ui.Revisit.PrimaryButton.interactable,"preview UI never pretends platform success"))yield break;
            ui.Invoke(ScrewKingUiCommand.TikTokMissionClose);yield return null;
            if(!Check(game.UiIsHome&&!ui.Revisit.gameObject.activeSelf,"close returns to Home"))yield break;
            End(true,"Fake-host integration: no-login mission rewards with grant report, arrival popup on entry launch, Home/popup wiring, navigation never pays, eligibility required, atomic claim/rollback, duplicate/restart/resume protection, stale callback rejection and preview/HTTPS guards passed. Live TikTok credentials were not used.");
        }
    }
}
