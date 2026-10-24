using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using MoodBoost.Sdk;
using MoodBoost.Sdk.Ads;
using MoodBoost.Sdk.Haptics;
using MoodBoost.Sdk.Tracking;
using MoodBoost.Sdk.Unity;
using Game.Game.Services;
using UnityEngine;

namespace ScrewKing
{
    // Game-side facade. Vendor SDK calls and ad/tracking bridges remain in the unchanged base SDK.
    public sealed class TikTokPlatform : MonoBehaviour
    {
        public SdkContext Context { get; private set; }
        public ScrewKingLevelTelemetry Telemetry { get; private set; }
        public bool BootReady { get; private set; }
        public bool Ready=>Context!=null && Context.Ads.IsReady(AdFormat.Rewarded);
        public bool Showing=>requestInProgress || adOnScreen;
        bool requestInProgress,adOnScreen,backgrounded,tutorialOpen;
        int tutorialIndex;
        public bool Backgrounded=>backgrounded;
        public void Configure(SdkSettings settings,Func<int> level)
        {
            RemoteConfigDefaults.Set(settings!=null?settings.RemoteConfigDefaults.Select(x=>new System.Collections.Generic.KeyValuePair<string,string>(x.key,x.value)):null);
            Context=MoodBoostSdk.GetOrCreate(settings);
            Context.PlayerLevel.SetProvider(level);
            Telemetry=new ScrewKingLevelTelemetry(Context.LevelTracking,new PlayerPrefsLevelSessionService());
            Context.AdEvents.AdRequested+=OnAdRequested;
            Context.AdEvents.Completed+=OnAdCompleted;
            Context.AppLifecycle.Paused+=OnPaused;
            Context.AppLifecycle.Resumed+=OnResumed;
            Initialize().Forget();
        }
        async UniTaskVoid Initialize()
        {
            Context.Runtime.InitializeAsync(CancellationToken.None).Forget();
            // Same boot latch as the base: provider initialization continues after the boot wait cap.
            await Context.Runtime.WaitForModuleAsync(SdkInitIds.TrackingAuthorization,CancellationToken.None);
            bool timedOut=await Context.Runtime.WaitForModuleAsync(SdkInitIds.RemoteConfig,CancellationToken.None)
                .TimeoutWithoutException(TimeSpan.FromSeconds(10),DelayType.UnscaledDeltaTime);
            StartupFunnel.MarkSdkReady(timedOut);BootReady=true;
        }
        public void ScreenRevealed(){StartupFunnel.MarkSplashLoaded();StartupFunnel.MarkHomeRevealed();}
        public void Offer(SourcePrompt prompt)=>Offer(Placement(prompt));
        public void Offer(string placement)=>Context.Tracking.LogAdOfferShown(TrackedAdFormat.Rewarded,placement);
        public static string Placement(SourcePrompt prompt)
        {
            switch(prompt){
                case SourcePrompt.Slot:return "add_slot";
                case SourcePrompt.Clear:case SourcePrompt.ClearRescue:return "clear";
                case SourcePrompt.Hammer:return "hammer";
                case SourcePrompt.Unlock:return "unlock_box";
                case SourcePrompt.Revive:return "revive";
                default:return "swap";
            }
        }
        public void ShowRewarded(SourcePrompt prompt,Action<bool> callback)=>ShowRewarded(Placement(prompt),callback);
        public void ShowRewarded(string placement,Action<bool> callback)=>Rewarded(placement,callback).Forget();
        async UniTaskVoid Rewarded(string placement,Action<bool> callback)
        {
            if(requestInProgress){callback(false);return;}
            requestInProgress=true;
            try{
                var result=await Context.Ads.ShowRewardedAsync(placement,CancellationToken.None);
                callback?.Invoke(result==AdResult.Completed);
            }catch(Exception error){Debug.LogException(error);callback?.Invoke(false);}
            finally{requestInProgress=false;}
        }
        public void ShowLevelComplete(Action callback)=>Interstitial(callback).Forget();
        async UniTaskVoid Interstitial(Action callback)
        {
            if(requestInProgress)return;
            requestInProgress=true;
            try{await Context.Ads.ShowInterstitialAsync("level_complete",CancellationToken.None);}
            catch(Exception error){Debug.LogException(error);}
            finally{requestInProgress=false;callback?.Invoke();}
        }
        public void Vibrate(bool hammer)=>Context?.Haptics.PlayWithCooldown(hammer?HapticKind.HeavyImpact:HapticKind.LightImpact,.05f);
        public void TutorialBegin(){if(tutorialOpen)return;tutorialOpen=true;tutorialIndex=0;Context.Tracking.LogTutorialStart();}
        public void TutorialStep(bool complete)
        {
            if(!tutorialOpen)return;
            Context.Tracking.LogTutorialAction("click","unscrew",++tutorialIndex);
            if(complete){Context.Tracking.LogTutorialFinish(++tutorialIndex);tutorialOpen=false;}
        }
        public void TutorialReset(){tutorialOpen=false;}
        void OnAdRequested(AdFormat format,string placement){if(format!=AdFormat.Banner && format!=AdFormat.Mrec)adOnScreen=true;}
        void OnAdCompleted(AdCompletion result){if(result.Result!=AdResult.Blocked)adOnScreen=false;}
        void OnPaused(){backgrounded=true;Telemetry?.Background(Showing);}
        void OnResumed(){backgrounded=false;}
        void OnDestroy()
        {
            Telemetry?.Leave();
            if(Context==null)return;
            Context.AdEvents.AdRequested-=OnAdRequested;Context.AdEvents.Completed-=OnAdCompleted;
            Context.AppLifecycle.Paused-=OnPaused;Context.AppLifecycle.Resumed-=OnResumed;
        }
    }
}
