using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrewKing
{
    public sealed class ScrewKingUiController : MonoBehaviour
    {
        public ScrewKingUiView Home, Settings, Win, Lose, Reward, Loading;
        [Tooltip("Booster offer: video first, currency second. Booster prompts fall back to Reward when unassigned.")]
        public ScrewKingUiView BuyBooster;
        public ScrewKingGameplayHud Gameplay;
        public TikTokMissionView Shortcut,Revisit;
        public GameObject ShortcutEntry,RevisitEntry;
        [Tooltip("Keep this at 750 x 1334 for the original layout, or edit anchors/CanvasScaler for another layout.")]
        public RectTransform DesignArea;
        public EventSystem OwnEventSystem;
        PortGame owner;
        float openedAt;
        ScrewKingUiAction[] actions;
        GraphicRaycaster raycaster;
        readonly List<RaycastResult> hits=new List<RaycastResult>();
        public bool LoadingVisible {get;private set;}
        public void Bind(PortGame game)
        {
            owner=game;openedAt=Time.unscaledTime;
            actions=GetComponentsInChildren<ScrewKingUiAction>(true);
            raycaster=GetComponent<GraphicRaycaster>();
            if(OwnEventSystem && EventSystem.current && EventSystem.current!=OwnEventSystem)OwnEventSystem.gameObject.SetActive(false);
            var layout=GetComponent<ScrewKingSafeLayout>();if(!layout)layout=gameObject.AddComponent<ScrewKingSafeLayout>();layout.Bind(this);
            Refresh();
        }
        void LateUpdate(){if(owner)Refresh();}
        void Refresh()
        {
            bool home=owner.UiIsHome,settings=owner.UiSettingsOpen;
            bool won=owner.UiResultVisible && owner.Model.Won,lost=owner.UiResultVisible && owner.Model.Lost;
            bool reward=!home && owner.Model!=null && owner.Model.Prompt!=SourcePrompt.None;
            var features=owner.PlatformFeatures;
            bool platformModal=features&&features.ModalOpen;
            bool booster=reward && BuyBooster && owner.UiBoosterPrompt;
            float age=Time.unscaledTime-openedAt;
            LoadingVisible=home && Loading && age<Loading.DoorHold+Loading.DoorDuration;
            if(Gameplay)Gameplay.gameObject.SetActive(!home);
            Home.Show(home);Settings.Show(settings&&!reward&&!won&&!lost&&!platformModal);
            Win.Show(won&&!reward&&!platformModal);Lose.Show(lost&&!reward&&!platformModal);Reward.Show(reward&&!booster&&!platformModal);if(BuyBooster)BuyBooster.Show(booster&&!platformModal);Loading.Show(LoadingVisible);
            if(ShortcutEntry)ShortcutEntry.SetActive(features&&features.Visible&&(!features.IsLive||features.Supported(TikTokMission.Shortcut)));
            if(RevisitEntry)RevisitEntry.SetActive(features&&features.Visible&&(!features.IsLive||features.Supported(TikTokMission.Revisit)));
            if(Shortcut){bool visible=platformModal&&features.OpenMission==TikTokMission.Shortcut;Shortcut.Show(visible);if(visible)Shortcut.Refresh(features);}
            if(Revisit){bool visible=platformModal&&features.OpenMission==TikTokMission.Revisit;Revisit.Show(visible);if(visible)Revisit.Refresh(features);}
            if(features)features.PumpHomeGuide(home&&!settings&&!LoadingVisible);
            if(Home.LevelLabel)Home.LevelLabel.text="Level "+owner.UiLevel;
            if(Home.CounterLabel)Home.CounterLabel.text=owner.UiCurrency.ToString();
            if(Win.CounterLabel)Win.CounterLabel.text=owner.UiCurrency.ToString();
            if(Lose.CounterLabel)Lose.CounterLabel.text=owner.UiCurrency.ToString();
            if(Win.LocalSimulationNotice)Win.LocalSimulationNotice.SetActive(owner.UiRewardsAreSimulated);
            if(Win.StatusLabel)Win.StatusLabel.text=owner.UiDoubleRewardClaimed?"+40":"+20";
            if(Win.RewardButtonLabel)Win.RewardButtonLabel.text=owner.UiDoubleRewardPending?"…":owner.UiDoubleRewardClaimed?"x2 ✓":"x2";
            if(won){if(Win.LevelLabel)Win.LevelLabel.text="Level "+owner.UiLevel;Win.Animate();}
            if(lost){if(Lose.LevelLabel)Lose.LevelLabel.text="Level "+owner.UiLevel;Lose.Animate();}
            if(reward){
                var offer=booster?BuyBooster:Reward;
                var prompt=owner.Model.Prompt;
                string title="UNLOCK BOX",description="Open another box\nand keep playing.";
                Sprite icon=offer.BoxIcon;
                switch(prompt){
                    case SourcePrompt.Revive:title="REVIVE!";break;
                    case SourcePrompt.Slot:title="ADD SLOT";description="Make room with\none extra slot.";icon=offer.SlotIcon;break;
                    case SourcePrompt.Clear:case SourcePrompt.ClearRescue:title="CLEAR SLOTS";description="Clear your slots\nand keep playing.";icon=offer.ClearIcon;break;
                    case SourcePrompt.Hammer:title="GLASS HAMMER";description="Break a board\nto free the screws.";icon=offer.HammerIcon;break;
                }
                if(offer.TitleLabel)offer.TitleLabel.text=title;
                if(offer.DescriptionLabel)offer.DescriptionLabel.text=description;
                if(offer.OfferIcon&&icon)offer.OfferIcon.sprite=icon;
                if(offer.CancelButtonLabel)offer.CancelButtonLabel.text=prompt==SourcePrompt.Revive?"Give up":"Not now";
                offer.Animate();
                if(booster){
                    if(BuyBooster.CounterLabel)BuyBooster.CounterLabel.text=owner.UiCurrency.ToString();
                    if(BuyBooster.PriceLabel)BuyBooster.PriceLabel.text=PortGame.BoosterPrice.ToString();
                    if(BuyBooster.StatusLabel)BuyBooster.StatusLabel.text=owner.UiAdShowing?"Video in progress…":owner.UiCurrency<PortGame.BoosterPrice?"Need "+(PortGame.BoosterPrice-owner.UiCurrency)+" more screws":"";
                }else{
                    if(Reward.StatusLabel)Reward.StatusLabel.text=owner.UiRewardStatus;
                    if(Reward.RewardButtonLabel)Reward.RewardButtonLabel.text=owner.UiRewardButtonText;
                }
            }
            if(settings && Settings.Switches!=null)foreach(var toggle in Settings.Switches)if(toggle)toggle.SetValue(owner.UiSetting(toggle.Setting));
            if(LoadingVisible)Loading.AnimateDoors(age);
            foreach(var action in actions){
                var button=action.GetComponent<Button>();if(!button)continue;
                bool allowed=!LoadingVisible;
                if(action.Command==ScrewKingUiCommand.DoubleReward)allowed &= owner.UiDoubleRewardAvailable;
                if(action.Command==ScrewKingUiCommand.Privacy){bool visible=features&&!features.IsLive&&features.LegalConfigured(true);action.gameObject.SetActive(visible);allowed &= visible;}
                if(action.Command==ScrewKingUiCommand.Terms){bool visible=features&&!features.IsLive&&features.LegalConfigured(false);action.gameObject.SetActive(visible);allowed &= visible;}
                if(action.Command==ScrewKingUiCommand.TikTokMissionPrimary&&features&&features.OpenMission.HasValue)allowed &= features.CanAct(features.OpenMission.Value);
                if(action.Command==ScrewKingUiCommand.TikTokMissionCheck&&features&&features.OpenMission.HasValue)allowed &= !features.Busy&&features.SignedIn&&features.Supported(features.OpenMission.Value)&&!features.Claimed(features.OpenMission.Value);
                if(action.Command==ScrewKingUiCommand.Play)allowed &= !platformModal;
                if(action.Command==ScrewKingUiCommand.WatchReward)allowed &= owner.UiRewardAvailable;
                if(action.Command==ScrewKingUiCommand.CancelReward)allowed &= !owner.UiAdShowing;
                if(action.Command==ScrewKingUiCommand.BuyBooster)allowed &= owner.UiCanBuyBooster;
                if(action.Command==ScrewKingUiCommand.NextLevel)allowed &= !owner.UiChangingLevel&&!owner.UiDoubleRewardPending&&!owner.UiAdShowing;
                if(action.Command==ScrewKingUiCommand.AddSlot||action.Command==ScrewKingUiCommand.ClearSlots||action.Command==ScrewKingUiCommand.GlassHammer)allowed &= owner.UiHudInteractable;
                button.interactable=allowed;
            }
        }
        public void Invoke(ScrewKingUiCommand command){if(owner&&!LoadingVisible)owner.UiInvoke(command);}
        public bool BlocksPointer(Vector2 screenPoint)
        {
            if(!raycaster || !EventSystem.current)return false;
            hits.Clear();raycaster.Raycast(new PointerEventData(EventSystem.current){position=screenPoint},hits);return hits.Count>0;
        }
        [ContextMenu("Preview/Gameplay HUD")] public void PreviewGameplay()=>Preview(null);
        [ContextMenu("Preview/Home")] public void PreviewHome()=>Preview(Home);
        [ContextMenu("Preview/Settings")] public void PreviewSettings()=>Preview(Settings);
        [ContextMenu("Preview/Win")] public void PreviewWin()=>Preview(Win);
        [ContextMenu("Preview/Lose")] public void PreviewLose()=>Preview(Lose);
        [ContextMenu("Preview/Reward")] public void PreviewReward()=>Preview(Reward);
        [ContextMenu("Preview/Buy Booster")] public void PreviewBuyBooster()=>Preview(BuyBooster);
        [ContextMenu("Preview/Loading")] public void PreviewLoading()=>Preview(Loading);
        void Preview(ScrewKingUiView selected){if(Shortcut)Shortcut.gameObject.SetActive(false);if(Revisit)Revisit.gameObject.SetActive(false);if(Gameplay)Gameplay.gameObject.SetActive(selected!=Home&&selected!=Loading);foreach(var view in new[]{Home,Settings,Win,Lose,Reward,BuyBooster,Loading})if(view)view.gameObject.SetActive(view==selected);}
    }
}
