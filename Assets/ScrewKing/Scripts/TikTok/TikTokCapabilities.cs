using System;
using System.Collections.Generic;
using UnityEngine;
using MoodBoost.Sdk.Tracking;

namespace ScrewKing
{
    public sealed class TikTokCapabilities:MonoBehaviour
    {
        public const string LocalSaveKey="ScrewKing.Platform.Preview.v1";
        public TikTokSettings Settings {get;private set;}
        public TikTokMissionLedger Ledger {get;private set;}
        public TikTokMission? OpenMission {get;private set;}
        public bool Busy {get;private set;}
        // Ready once the host and TikTok save are up; TikTok tracks mission completion per user, so no login is needed.
        public bool Ready=>SaveReady&&IsLive;
        public bool SaveReady {get;private set;}
        public string Status {get;private set;}="Connecting…";
        public bool IsLive=>api!=null&&api.IsLive;
        public bool Visible=>Settings&&Settings.EnableGrowthFeatures&&(IsLive||Settings.ShowPlatformUiInPreview);
        public int EarnedCurrency=>Ledger==null?0:Ledger.Data.growthCoins;
        public bool ModalOpen=>OpenMission.HasValue;
        public bool AutoGuidePending=>Ledger!=null&&Ledger.Data.tutorialDone&&!Ledger.Data.introSeen;
        // TT.Save storage is already per TikTok user, so the ledger keys receipts by one fixed account.
        const string Account="tiktok";
        ITikTokMiniGameApi api;
        PortGame owner;
        int request;
        float deadline;
        bool platformSave;
        // Missions TikTok reported claimable this session (the player came back through the sidebar or the home screen icon); each opens its popup once.
        readonly Queue<TikTokMission> arrivals=new Queue<TikTokMission>();
        readonly HashSet<TikTokMission> announced=new HashSet<TikTokMission>();
        readonly Dictionary<TikTokMission,string> missionStatus=new Dictionary<TikTokMission,string>();
        public string StatusFor(TikTokMission mission)=>Ready&&missionStatus.TryGetValue(mission,out var text)?text:Status;
        void SetStatus(TikTokMission mission,string text){missionStatus[mission]=text;}
        readonly Queue<TikTokMission> probes=new Queue<TikTokMission>();
        readonly HashSet<TikTokMission> queued=new HashSet<TikTokMission>();
        public void Configure(PortGame game,TikTokSettings config,ITikTokMiniGameApi implementation=null)
        {
            if(api!=null){api.Resumed-=Resume;api.Dispose();}StopAllCoroutines();request++;Busy=false;SaveReady=false;OpenMission=null;probes.Clear();queued.Clear();missionStatus.Clear();arrivals.Clear();announced.Clear();
            owner=game;Settings=config;
#if MB_TIKTOK && UNITY_WEBGL && !UNITY_EDITOR
            api=implementation??new TikTokNativeApi();
#else
            api=implementation??new PreviewTikTokApi();
#endif
            api.Resumed+=Resume;
            int ticket=Begin();
            api.Initialize(ok=>Finish(ticket,()=>{
                string json;platformSave=api.IsLive;
                // A host without TT saving still gets both entry rewards; the ledger then lives in local storage.
                try{json=platformSave?api.LoadSave():PlayerPrefs.GetString(LocalSaveKey,"");}
                catch{platformSave=false;json=PlayerPrefs.GetString(LocalSaveKey,"");}
                try{
                    Ledger=new TikTokMissionLedger(json,Save);
                    SaveReady=true;
                    if(platformSave&&!string.IsNullOrEmpty(json)&&owner)owner.RestorePlatformProgress(Ledger.Data);
                    else Ledger.CaptureProgress();
                }catch{SaveReady=false;Status="Saving unavailable. Please restart TikTok.";return;}
                if(ok&&api.IsLive){Status="Choose a reward below.";Enqueue(TikTokMission.Shortcut);Enqueue(TikTokMission.Revisit);}
                else Status="Open in TikTok to use these rewards.";
            }));
        }
        bool Save(string json)
        {
            if(platformSave)return api.Save(json);
            PlayerPrefs.SetString(LocalSaveKey,json);PlayerPrefs.Save();return true;
        }
        int Begin(){Busy=true;deadline=Time.realtimeSinceStartup+18;return ++request;}
        void Finish(int ticket,Action action){if(!this||!Busy||ticket!=request)return;Busy=false;action();}
        public bool Supported(TikTokMission mission)=>IsLive&&api.Supports(mission);
        public bool Eligible(TikTokMission mission)=>Ready&&Ledger!=null&&Ledger.Eligible(Account,mission);
        public bool Claimed(TikTokMission mission)=>Ready&&Ledger!=null&&Ledger.Claimed(Account,mission);
        public int RewardAmount(TikTokMission mission)=>Mathf.Clamp(Settings?(mission==TikTokMission.Shortcut?Settings.ShortcutReward:Settings.RevisitReward):0,1,1000000);
        public bool CanAct(TikTokMission mission)=>!Busy&&SaveReady&&Supported(mission)&&!Claimed(mission);
        public void Open(TikTokMission mission)
        {
            if(!Visible)return;OpenMission=mission;Track("open",true,mission);
            if(!IsLive)Status="Open in TikTok to use these rewards.";
            else if(!Supported(mission))Status="This feature needs a newer TikTok version.";
            else if(Claimed(mission))SetStatus(mission,mission==TikTokMission.Shortcut?"Reward already received.":"Come back tomorrow for more.");
            else{SetStatus(mission,Eligible(mission)?"Your reward is ready!":"Follow the steps, then check your reward.");Enqueue(mission);}
        }
        public void Close(){OpenMission=null;}
        public void Primary(TikTokMission mission)
        {
            if(!CanAct(mission))return;
            if(Eligible(mission)){Claim(mission);return;}
            int ticket=Begin();SetStatus(mission,"Opening TikTok…");Track("navigate",true,mission);
            api.Navigate(mission,ok=>Finish(ticket,()=>{
                SetStatus(mission,ok?"Return through the new entry to claim your reward.":"Could not open. Please try again.");
                if(ok)Enqueue(mission); // Navigation alone never grants currency.
            }));
        }
        public void Check(TikTokMission mission)
        {
            if(Busy||!Ready||!Supported(mission)||Claimed(mission))return;
            int ticket=Begin();SetStatus(mission,"Checking reward…");
            api.CheckReward(mission,(ok,eligible)=>Finish(ticket,()=>{
                if(!ok){SetStatus(mission,"Could not check. Please try again.");Track("eligibility",false,mission);return;}
                if(!Ledger.ObserveEligibility(Account,mission,eligible)){SetStatus(mission,"Could not save. Please try again.");return;}
                SetStatus(mission,Eligible(mission)?"Your reward is ready!":"Open the game from the requested entry, then check again.");
                Track("eligibility",eligible,mission);
                if(Eligible(mission)&&announced.Add(mission))arrivals.Enqueue(mission);
            }));
        }
        public bool Claim(TikTokMission mission)
        {
            if(Busy||!Ready||!Supported(mission)||!Eligible(mission))return false;
            if(!Ledger.TryClaim(Account,mission,RewardAmount(mission))){SetStatus(mission,"Could not save. Please try again.");return false;}
            SetStatus(mission,"Received +"+RewardAmount(mission)+" screws!");Track("claim",true,mission,RewardAmount(mission));
            // The local receipt already blocks a second payout; the report only stops TikTok offering the reward again.
            api.ReportGranted(mission,ok=>Track("report",ok,mission));
            if(owner)owner.PlatformRewardSound();return true;
        }
        void Enqueue(TikTokMission mission){if(Supported(mission)&&!Claimed(mission)&&queued.Add(mission))probes.Enqueue(mission);}
        void OnApplicationPause(bool paused){if(!paused)Resume();}
        void Resume(){if(!Ready)return;Enqueue(TikTokMission.Shortcut);Enqueue(TikTokMission.Revisit);}
        void Update()
        {
            if(Busy&&Time.realtimeSinceStartup>deadline){Busy=false;request++;Status="Request timed out. Please try again.";missionStatus.Clear();}
            if(!Busy&&Ready&&probes.Count>0){var mission=probes.Dequeue();queued.Remove(mission);Check(mission);}
        }
        public bool SpendEarned(int amount)=>SaveReady&&Ledger!=null&&Ledger.TrySpendGrowth(amount);
        public void PersistProgress(){if(SaveReady&&Ledger!=null&&!Ledger.CaptureProgress())Status="Could not save. Please try again.";}
        public void TutorialCompleted(){if(SaveReady&&Ledger!=null)Ledger.CaptureProgress(tutorialDone:true);}
        public void PumpHomeGuide(bool unobstructed)
        {
            if(!unobstructed||!Settings||!Ready||Busy||ModalOpen)return;
            // Launched from the sidebar or the home screen icon: open that reward right away, like the GameFramework entry reward popup.
            while(arrivals.Count>0){var arrived=arrivals.Dequeue();if(Eligible(arrived)){Open(arrived);return;}}
            if(!Settings.AutoGuideAfterTutorial||!AutoGuidePending)return;
            var mission=Supported(TikTokMission.Shortcut)&&!Claimed(TikTokMission.Shortcut)?TikTokMission.Shortcut:TikTokMission.Revisit;
            if(Supported(mission)&&!Claimed(mission)&&Ledger.CaptureProgress(introSeen:true))Open(mission);
        }
        public bool LegalConfigured(bool privacy)=>Settings&&TikTokSettings.IsHttps(privacy?Settings.PrivacyUrl:Settings.TermsUrl);
        public void OpenLegal(bool privacy)
        {
            if(!LegalConfigured(privacy)||IsLive)return; // Native TTSDK does not implement Application.OpenURL; host loading page owns legal URLs.
            Application.OpenURL(privacy?Settings.PrivacyUrl:Settings.TermsUrl);
        }
        void Track(string action,bool success,TikTokMission? mission=null,int amount=0)
        {
            if(!owner||owner.PlatformTracking==null)return;
            var data=new TrackingParams().With("success",success).With("amount",amount);
            if(mission.HasValue)data=data.With("mission",mission.Value.ToString().ToLowerInvariant());
            owner.PlatformTracking.Log("tiktok_growth_"+action,data);
        }
        void OnDestroy(){request++;if(api!=null){api.Resumed-=Resume;api.Dispose();}}
    }
}
