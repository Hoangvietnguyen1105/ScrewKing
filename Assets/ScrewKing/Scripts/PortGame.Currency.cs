using UnityEngine;

namespace ScrewKing
{
    public sealed partial class PortGame
    {
        public const int LevelCurrencyReward = 20;
        public const string CurrencyKey = "ScrewKing.Currency";
        public const int BoosterPrice = 900;
        readonly ScrewKingRoundReward roundReward = new ScrewKingRoundReward();
        public int UiCurrency => (int)System.Math.Min(int.MaxValue,(long)Mathf.Max(0,PlayerPrefs.GetInt(CurrencyKey,0))+(PlatformFeatures?PlatformFeatures.EarnedCurrency:0));
        public bool UiRewardsAreSimulated => false;
        public bool UiDoubleRewardClaimed => roundReward.Doubled;
        public bool UiDoubleRewardPending => roundReward.Pending;
        public bool UiDoubleRewardAvailable => UiResultVisible && Model.Won && roundReward.BaseGranted && !roundReward.Doubled && !roundReward.Pending && !UiAdShowing && !UiChangingLevel && platform!=null && platform.Ready;
        public bool UiBoosterPrompt => !menu && Model != null && (Model.Prompt==SourcePrompt.Slot||Model.Prompt==SourcePrompt.Clear||Model.Prompt==SourcePrompt.Hammer);
        public bool UiCanBuyBooster => UiBoosterPrompt && UiCurrency >= BoosterPrice && !UiAdShowing;
        // Ads stay the main source; currency is the paid alternative for the same booster prompt.
        void UiBuyBooster()
        {
            if(!UiCanBuyBooster)return;
            // Level coins are spent first; mission coins live in the platform ledger and are spent transactionally.
            int local=Mathf.Max(0,PlayerPrefs.GetInt(CurrencyKey,0)),fromLocal=Mathf.Min(local,BoosterPrice),fromMissions=BoosterPrice-fromLocal;
            if(fromMissions>0&&(!PlatformFeatures||!PlatformFeatures.SpendEarned(fromMissions))){Say("Could not save. Please try again.");return;}
            var action=Model.Prompt;AddCurrency(-fromLocal);Play("coin");
            Model.RewardResult(true);platform.Telemetry.BoosterUsed(action);PlayRewardSound(action);
        }
        void AddCurrency(int amount)
        {
            PlayerPrefs.SetInt(CurrencyKey, (int)System.Math.Min(int.MaxValue, (long)Mathf.Max(0,PlayerPrefs.GetInt(CurrencyKey,0)) + amount));
            PlayerPrefs.Save();PlatformFeatures?.PersistProgress();
        }
        void GrantRoundCurrency(){if(roundReward.GrantBase()){AddCurrency(LevelCurrencyReward);Play("coin");}}
        void UiDoubleReward()
        {
            if(!UiDoubleRewardAvailable||!roundReward.BeginDouble(out int ticket))return;
            var current=Model;int version=generation;
            platform.ShowRewarded("level_reward_double",completed=>
            {
                CompleteDoubleReward(ticket,completed,version,current);
            });
        }
        void CompleteDoubleReward(int ticket,bool completed,int version,PortModel current)
        {
            if(version!=generation||!ReferenceEquals(current,Model)||menu)return;
            if(roundReward.CompleteDouble(ticket,completed)){AddCurrency(LevelCurrencyReward);Play("coin");}
        }
        void OfferDoubleReward(){platform.Offer("level_reward_double");}
    }
}
