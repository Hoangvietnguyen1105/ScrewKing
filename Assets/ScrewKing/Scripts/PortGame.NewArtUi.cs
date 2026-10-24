using UnityEngine;
using System.Linq;

namespace ScrewKing
{
    public sealed partial class PortGame
    {
        [Tooltip("Editable nested screen/popup prefabs. Falls back to Resources/UI/Prefabs/ScrewKingUI if unassigned.")]
        public ScrewKingUiController UiRoot;
        bool musicOn=true,vibrationOn=true;
        void InitializeArtUi()
        {
            sound=PlayerPrefs.GetInt("ScrewKing.Sound",1)!=0;
            musicOn=PlayerPrefs.GetInt("ScrewKing.Music",1)!=0;
            vibrationOn=PlayerPrefs.GetInt("ScrewKing.Vibration",1)!=0;
            musicSource.mute=!musicOn;InitializePlatformFeatures();
            if(!UiRoot)UiRoot=FindFirstObjectByType<ScrewKingUiController>();
            if(!UiRoot){var prefab=Resources.Load<ScrewKingUiController>("UI/Prefabs/ScrewKingUI");if(prefab)UiRoot=Instantiate(prefab);}
            if(UiRoot)UiRoot.Bind(this);else Debug.LogError("ScrewKingUI prefab is missing. Use Screw King/UI/Create Missing Prefabs.");
        }
        public bool UiHudInteractable=>!menu&&Model!=null&&Model.Ready&&!settings&&!busy&&!UiAdShowing;
        public bool UiMessageVisible=>Time.time-lastMessageAt<2.2f&&!string.IsNullOrEmpty(message);
        public string UiMessage=>message;
        public float UiTransitionAlpha=>Mathf.Clamp01((transitionUntil-Time.time)/.35f);
        public void UiRequestUnlock(BoxState box)
        {
            if(!UiHudInteractable||box==null||!box.Locked)return;
            int index=Model.Boxes.IndexOf(box);if(index>=0)Model.Request(SourcePrompt.Unlock,index);
        }
        public bool UiGuideTarget(out Vector2 point)
        {
            point=default;
            if(!UiHudInteractable||level!=1||Model.Clock<1.8||Model.HammerArmed)return false;
            var nail=Model.Nails.Where(n=>!n.Removed&&ReferenceEquals(n.Board,guideBoard)&&nails[n.Id]&&nails[n.Id].enabled&&boards.Any(b=>b&&ReferenceEquals(b.Data,n.Board)&&b.Clickable)&&!IsBlocked(n)).OrderByDescending(n=>n.Layer).ThenBy(n=>n.Id).FirstOrDefault();
            if(nail==null)return false;
            platform.TutorialBegin();
            var position=nails[nail.Id].transform.position;point=new Vector2(position.x*100,position.y*100);return true;
        }
        public bool UiGuideScreenTarget(out Vector2 screenPoint)
        {
            screenPoint=default;
            if(!UiGuideTarget(out var point)||!camera2d)return false;
            screenPoint=camera2d.WorldToScreenPoint(new Vector3(point.x*.01f,point.y*.01f,0));
            return true;
        }
        public bool UiIsHome=>menu;
        public bool UiSettingsOpen=>settings;
        public int UiLevel=>level;
        public bool UiResultVisible=>!menu && Model!=null && ((Model.Won&&Model.Clock>=wonAt+.5)||Model.Lost);
        public bool UiSetting(ScrewKingUiSetting setting)=>setting==ScrewKingUiSetting.Music?musicOn:setting==ScrewKingUiSetting.Sound?sound:vibrationOn;
        public bool UiAdShowing=>platform!=null && platform.Showing;
        public bool UiChangingLevel=>sdkChangingLevel;
        public bool UiRewardAvailable=>Model!=null && Model.DoScrewCount==0 && platform!=null && platform.Ready && !platform.Showing;
        public string UiRewardButtonText=>"Watch video";
        public string UiRewardStatus=>UiAdShowing?"Video in progress…":UiRewardAvailable?"Watch a video to continue.":"Video unavailable · try again later";
        public void UiInvoke(ScrewKingUiCommand command)
        {
            Play("click");
            switch(command){
                case ScrewKingUiCommand.TikTokShortcut:PlatformFeatures?.Open(TikTokMission.Shortcut);break;
                case ScrewKingUiCommand.TikTokRevisit:PlatformFeatures?.Open(TikTokMission.Revisit);break;
                case ScrewKingUiCommand.TikTokMissionClose:PlatformFeatures?.Close();break;
                case ScrewKingUiCommand.TikTokMissionPrimary:if(PlatformFeatures&&PlatformFeatures.OpenMission.HasValue)PlatformFeatures.Primary(PlatformFeatures.OpenMission.Value);break;
                case ScrewKingUiCommand.TikTokMissionCheck:if(PlatformFeatures&&PlatformFeatures.OpenMission.HasValue)PlatformFeatures.Check(PlatformFeatures.OpenMission.Value);break;
                case ScrewKingUiCommand.Privacy:PlatformFeatures?.OpenLegal(true);break;
                case ScrewKingUiCommand.Terms:PlatformFeatures?.OpenLegal(false);break;
                case ScrewKingUiCommand.DoubleReward:UiDoubleReward();break;
                case ScrewKingUiCommand.Play:if(menu&&!settings&&(!PlatformFeatures||!PlatformFeatures.ModalOpen)){Play("enter");StartLevel(level);}break;
                case ScrewKingUiCommand.Previous:if(menu&&!settings)level=level<=1?89:level-1;break;
                case ScrewKingUiCommand.NextSelection:if(menu&&!settings)level=level>=89?1:level+1;break;
                case ScrewKingUiCommand.Settings:settings=true;break;
                case ScrewKingUiCommand.Resume:settings=false;break;
                case ScrewKingUiCommand.Quit:if(menu)settings=false;else GoHome();break;
                case ScrewKingUiCommand.CloseResult:if(!UiDoubleRewardPending&&!UiAdShowing)GoHome();break;
                case ScrewKingUiCommand.NextLevel:if(!menu&&Model!=null&&Model.Won&&!UiDoubleRewardPending&&!UiAdShowing){Play("enter");UiNextLevel();}break;
                case ScrewKingUiCommand.Retry:if(!menu&&Model!=null&&Model.Lost){Play("replay");UiRetryLevel();}break;
                case ScrewKingUiCommand.WatchReward:if(!menu&&Model!=null&&Model.Prompt!=SourcePrompt.None&&UiRewardAvailable)UiWatchReward();break;
                case ScrewKingUiCommand.CancelReward:if(!menu&&Model!=null&&!UiAdShowing)Model.RewardResult(false);break;
                case ScrewKingUiCommand.AddSlot:case ScrewKingUiCommand.ClearSlots:case ScrewKingUiCommand.GlassHammer:
                    if(UiHudInteractable){var prompt=command==ScrewKingUiCommand.AddSlot?SourcePrompt.Slot:command==ScrewKingUiCommand.ClearSlots?SourcePrompt.Clear:SourcePrompt.Hammer;if(!Model.Request(prompt))Say("This action is unavailable right now.");}break;
                case ScrewKingUiCommand.MusicOn:case ScrewKingUiCommand.MusicOff:musicOn=command==ScrewKingUiCommand.MusicOn;SaveUiSetting("ScrewKing.Music",musicOn);musicSource.mute=!musicOn;break;
                case ScrewKingUiCommand.SoundOn:case ScrewKingUiCommand.SoundOff:sound=command==ScrewKingUiCommand.SoundOn;SaveUiSetting("ScrewKing.Sound",sound);break;
                case ScrewKingUiCommand.VibrationOn:case ScrewKingUiCommand.VibrationOff:vibrationOn=command==ScrewKingUiCommand.VibrationOn;SaveUiSetting("ScrewKing.Vibration",vibrationOn);break;
            }
        }
        void SaveUiSetting(string key,bool value){PlayerPrefs.SetInt(key,value?1:0);PlayerPrefs.Save();PlatformFeatures?.PersistProgress();}
        void UiNextLevel(){if(sdkChangingLevel)return;sdkChangingLevel=true;int version=generation;platform.ShowLevelComplete(()=>{sdkChangingLevel=false;if(version==generation)StartLevel(level==89?1:level+1,-1,MoodBoost.Sdk.Tracking.PlayType.Next);});}
        void UiRetryLevel()=>StartLevel(level,-1,MoodBoost.Sdk.Tracking.PlayType.Restart);
        void UiWatchReward()=>WatchRewarded();
    }
}
