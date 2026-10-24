using UnityEngine;

namespace ScrewKing
{
    public sealed partial class PortGame
    {
        public const int LevelCurrencyReward = 20;
        public const string CurrencyKey = "ScrewKing.Currency";
        readonly ScrewKingRoundReward roundReward = new ScrewKingRoundReward();
        public int UiCurrency => (int)System.Math.Min(int.MaxValue,(long)Mathf.Max(0,PlayerPrefs.GetInt(CurrencyKey,0))+(PlatformFeatures?PlatformFeatures.EarnedCurrency:0));
        public bool UiRewardsAreSimulated => false;
        public bool UiDoubleRewardClaimed => roundReward.Doubled;
        public bool UiDoubleRewardPending => roundReward.Pending;
        public bool UiDoubleRewardAvailable => UiResultVisible && Model.Won && roundReward.BaseGranted && !roundReward.Doubled && !roundReward.Pending && !UiAdShowing && !UiChangingLevel && platform!=null && platform.Ready;
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
