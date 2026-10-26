#if MB_TIKTOK && UNITY_WEBGL && !UNITY_EDITOR
using System;
using TTSDK;
namespace ScrewKing
{
    public sealed class TikTokNativeApi:ITikTokMiniGameApi
    {
        [Serializable] public sealed class Blob {public string json;}
        const string SaveKey="ScrewKing.Platform.v1";
        bool initCompleted,disposed,nativeReady;
        TTAppLifeCycle lifecycle;
        TTAppLifeCycle.OnShowEventWithDict onShow;
        public bool IsLive=>nativeReady&&TT.InContainerEnv;
        public event Action Resumed;
        public void Initialize(Action<bool> result)
        {
            TT.InitSDK((code,env)=>{
                if(disposed||initCompleted)return;initCompleted=true;
                if(code!=0||!TT.InContainerEnv){result(false);return;}
                nativeReady=true;
                if(TTSDK.CanIUse.GetAppLifeCycle){lifecycle=TT.GetAppLifeCycle();if(lifecycle!=null){onShow=options=>Resumed?.Invoke();lifecycle.OnShow+=onShow;}}
                result(true);
            });
        }
        // Revisit prefers GetRevisitMissionReward (TikTok 46+) and falls back to the older entrance mission check.
        public bool Supports(TikTokMission m)=>m==TikTokMission.Shortcut?
            TTSDK.CanIUse.AddShortcut&&TTSDK.CanIUse.GetShortcutMissionReward:
            TTSDK.CanIUse.StartEntranceMission&&(TTSDK.CanIUse.GetRevisitMissionReward||TTSDK.CanIUse.GetEntranceMissionReward);
        public void Navigate(TikTokMission mission,Action<bool> result)
        {
            if(!Supports(mission)){result(false);return;}
            if(mission==TikTokMission.Shortcut)TT.AddShortcut(ok=>result(ok));
            else TT.StartEntranceMission(new TTStartEntranceMissionParam{success=data=>result(true),fail=error=>result(false)});
        }
        public void CheckReward(TikTokMission mission,Action<bool,bool> result)
        {
            if(!Supports(mission)){result(false,false);return;}
            if(mission==TikTokMission.Shortcut)TT.GetShortcutMissionReward(new GetShortcutMissionRewardParam{Success=data=>result(!ReferenceEquals(data,null),!ReferenceEquals(data,null)&&data.CanReceiveReward),Fail=error=>result(false,false)});
            else if(TTSDK.CanIUse.GetRevisitMissionReward)TT.GetRevisitMissionReward(new TTGetRevisitMissionRewardParam{success=data=>result(!ReferenceEquals(data,null),!ReferenceEquals(data,null)&&data.canReceiveReward),fail=error=>result(false,false)});
            else TT.GetEntranceMissionReward(new TTGetEntranceMissionRewardParam{success=data=>result(!ReferenceEquals(data,null),!ReferenceEquals(data,null)&&data.canReceiveReward),fail=error=>result(false,false)});
        }
        public void ReportGranted(TikTokMission mission,Action<bool> result)
        {
            if(!TTSDK.CanIUse.ReportMissionRewardGranted){result(false);return;}
            TT.ReportMissionRewardGranted(new TTReportMissionRewardGrantedParam{success=data=>result(true),fail=error=>result(false)});
        }
        public string LoadSave()
        {
            if(!TTSDK.CanIUse.LoadSaving||!TTSDK.CanIUse.Save)throw new InvalidOperationException("TikTok saving is unavailable.");
            var data=TT.LoadSaving<Blob>(SaveKey);return data==null?"":data.json;
        }
        public bool Save(string json)=>TTSDK.CanIUse.Save&&TT.Save(new Blob{json=json},SaveKey);
        public void Dispose(){disposed=true;if(lifecycle!=null)lifecycle.OnShow-=onShow;Resumed=null;}
    }
}
#endif
