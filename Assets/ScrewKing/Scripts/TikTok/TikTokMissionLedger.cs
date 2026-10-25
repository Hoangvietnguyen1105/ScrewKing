using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScrewKing
{
    public enum TikTokMission { Shortcut, Revisit }
    [Serializable] public sealed class TikTokMissionReceipt
    {
        public string account,eligibleDay,claimedDay;
        public TikTokMission mission;
    }
    [Serializable] public sealed class TikTokPlatformSave
    {
        public int schema=1,coins,bestLevel=1,music=1,sound=1,vibration=1,growthCoins;
        public bool tutorialDone,introSeen;
        public List<TikTokMissionReceipt> receipts=new List<TikTokMissionReceipt>();
    }
    public sealed class TikTokMissionLedger
    {
        public TikTokPlatformSave Data {get;private set;}
        readonly Func<string,bool> persist;
        readonly Func<string> today;
        public TikTokMissionLedger(string json,Func<string,bool> save,Func<string> utcDay=null)
        {
            persist=save;today=utcDay??(()=>DateTime.UtcNow.ToString("yyyy-MM-dd"));
            try{Data=string.IsNullOrEmpty(json)?new TikTokPlatformSave():JsonUtility.FromJson<TikTokPlatformSave>(json);}catch{Data=null;}
            if(Data==null||Data.schema!=1)throw new InvalidOperationException("Unsupported or damaged platform save.");
            if(Data.receipts==null)Data.receipts=new List<TikTokMissionReceipt>();
            Data.coins=Math.Max(0,Data.coins);Data.growthCoins=Math.Max(0,Data.growthCoins);Data.bestLevel=Mathf.Clamp(Data.bestLevel,1,89);
        }
        TikTokMissionReceipt Find(string account,TikTokMission mission,bool create=false)
        {
            var item=Data.receipts.Find(r=>r!=null&&r.account==account&&r.mission==mission);
            if(item==null&&create){item=new TikTokMissionReceipt{account=account,mission=mission};Data.receipts.Add(item);}return item;
        }
        public bool Claimed(string account,TikTokMission mission)
        {
            var item=Find(account,mission);return item!=null&&!string.IsNullOrEmpty(item.claimedDay)&&(mission==TikTokMission.Shortcut||item.claimedDay==today());
        }
        public bool Eligible(string account,TikTokMission mission)
        {
            var item=Find(account,mission);return !string.IsNullOrEmpty(account)&&!Claimed(account,mission)&&item!=null&&!string.IsNullOrEmpty(item.eligibleDay)&&(mission==TikTokMission.Shortcut||item.eligibleDay==today());
        }
        bool Transaction(Action change)
        {
            string before=JsonUtility.ToJson(Data);change();
            try{if(persist(JsonUtility.ToJson(Data)))return true;}catch{}
            Data=JsonUtility.FromJson<TikTokPlatformSave>(before);return false;
        }
        public bool ObserveEligibility(string account,TikTokMission mission,bool platformEligible)
        {
            if(string.IsNullOrEmpty(account)||!platformEligible||Claimed(account,mission))return true;
            return Transaction(()=>Find(account,mission,true).eligibleDay=today());
        }
        public bool TryClaim(string account,TikTokMission mission,int amount)
        {
            if(amount<=0||!Eligible(account,mission))return false;
            return Transaction(()=>{Find(account,mission,true).claimedDay=today();Data.growthCoins=(int)Math.Min(int.MaxValue,(long)Data.growthCoins+amount);});
        }
        public bool TrySpendGrowth(int amount)
        {
            if(amount<=0||amount>Data.growthCoins)return false;
            return Transaction(()=>Data.growthCoins-=amount);
        }
        public bool CaptureProgress(bool? tutorialDone=null,bool? introSeen=null)
        {
            return Transaction(()=>{
                Data.coins=Math.Max(0,PlayerPrefs.GetInt(PortGame.CurrencyKey,0));Data.bestLevel=Mathf.Clamp(PlayerPrefs.GetInt("ScrewKingPort.Level",1),1,89);
                Data.music=PlayerPrefs.GetInt("ScrewKing.Music",1);Data.sound=PlayerPrefs.GetInt("ScrewKing.Sound",1);Data.vibration=PlayerPrefs.GetInt("ScrewKing.Vibration",1);
                if(tutorialDone.HasValue)Data.tutorialDone=tutorialDone.Value;if(introSeen.HasValue)Data.introSeen=introSeen.Value;
            });
        }
    }
}
