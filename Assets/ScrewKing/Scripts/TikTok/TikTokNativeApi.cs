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
        // CanIUse under-reports these on some TikTok builds and a disabled button fails review, so every call is attempted and a failure falls through to the next route.
        public bool Supports(TikTokMission m)=>IsLive;
        public void Navigate(TikTokMission mission,Action<bool> result)
        {
            if(mission==TikTokMission.Shortcut){Try(()=>TT.AddShortcut(ok=>result(ok)),()=>result(false));return;}
            // The profile sidebar: TikTok Minis' entrance mission first, then the TTSDK sidebar scene used by the GameFramework sample.
            Action sidebar=()=>Try(()=>TTSideBarImpl.Instance.NavigateToScene(TTSideBar.SceneEnum.SideBar,()=>result(true),()=>{},(code,msg)=>result(false)),()=>result(false));
            Try(()=>TT.StartEntranceMission(new TTStartEntranceMissionParam{success=data=>result(true),fail=error=>sidebar()}),sidebar);
        }
        public void CheckReward(TikTokMission mission,Action<bool,bool> result)
        {
            if(mission==TikTokMission.Shortcut){Try(()=>TT.GetShortcutMissionReward(new GetShortcutMissionRewardParam{Success=data=>result(!ReferenceEquals(data,null),!ReferenceEquals(data,null)&&data.CanReceiveReward),Fail=error=>result(false,false)}),()=>result(false,false));return;}
            // GetRevisitMissionReward needs TikTok 46+; older hosts answer through the entrance mission check.
            Action entrance=()=>Try(()=>TT.GetEntranceMissionReward(new TTGetEntranceMissionRewardParam{success=data=>result(!ReferenceEquals(data,null),!ReferenceEquals(data,null)&&data.canReceiveReward),fail=error=>result(false,false)}),()=>result(false,false));
            Try(()=>TT.GetRevisitMissionReward(new TTGetRevisitMissionRewardParam{success=data=>result(!ReferenceEquals(data,null),!ReferenceEquals(data,null)&&data.canReceiveReward),fail=error=>entrance()}),entrance);
        }
        public void ReportGranted(TikTokMission mission,Action<bool> result)
        {
            Try(()=>TT.ReportMissionRewardGranted(new TTReportMissionRewardGrantedParam{success=data=>result(true),fail=error=>result(false)}),()=>result(false));
        }
        static void Try(Action call,Action failed){try{call();}catch(Exception e){UnityEngine.Debug.LogWarning("TikTok mission call failed: "+e.Message);failed();}}
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
