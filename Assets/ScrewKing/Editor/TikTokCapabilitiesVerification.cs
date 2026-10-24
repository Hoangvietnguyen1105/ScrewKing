using System;
using System.IO;
using UnityEngine;
namespace ScrewKing.Editor
{
    public static class TikTokCapabilitiesVerification
    {
        public static void Require(bool condition,string message){if(!condition)throw new Exception("TikTok capabilities: "+message);}
        public static void Run()
        {
            string saved="",day="2026-10-24";bool writable=true;
            Func<string,bool> save=json=>{if(!writable)return false;saved=json;return true;};
            var ledger=new TikTokMissionLedger("",save,()=>day);
            Require(!ledger.TryClaim("player",TikTokMission.Shortcut,100),"no reward before positive platform eligibility");
            writable=false;Require(!ledger.ObserveEligibility("player",TikTokMission.Shortcut,true)&&!ledger.Eligible("player",TikTokMission.Shortcut),"failed persistence rolls back eligibility");
            writable=true;Require(ledger.ObserveEligibility("player",TikTokMission.Shortcut,true),"eligible callback saved");
            writable=false;Require(!ledger.TryClaim("player",TikTokMission.Shortcut,100)&&ledger.Data.growthCoins==0&&!ledger.Claimed("player",TikTokMission.Shortcut),"failed persistence rolls back coins and receipt atomically");
            writable=true;Require(ledger.TryClaim("player",TikTokMission.Shortcut,100)&&!ledger.TryClaim("player",TikTokMission.Shortcut,100),"shortcut grants once");
            ledger=new TikTokMissionLedger(saved,save,()=>day);Require(ledger.Claimed("player",TikTokMission.Shortcut)&&ledger.Data.growthCoins==100,"restart preserves reward/receipt");
            Require(!ledger.Eligible("another-player",TikTokMission.Shortcut),"account isolation");
            ledger.ObserveEligibility("player",TikTokMission.Revisit,true);Require(ledger.TryClaim("player",TikTokMission.Revisit,50)&&!ledger.TryClaim("player",TikTokMission.Revisit,50),"revisit grants once per UTC day");
            day="2026-10-25";Require(!ledger.Eligible("player",TikTokMission.Revisit),"yesterday eligibility cannot pay today");
            ledger.ObserveEligibility("player",TikTokMission.Revisit,true);Require(ledger.TryClaim("player",TikTokMission.Revisit,50)&&ledger.Claimed("player",TikTokMission.Shortcut),"next day revisit works; shortcut remains one-time");
            Require(!TikTokSessionGateway.IsHttps("http://example.com")&&!TikTokSessionGateway.IsHttps("https://name:secret@example.com")&&TikTokSessionGateway.IsHttps("https://example.com/v1/tiktok/session"),"backend/legal HTTPS URL validation");
            var ui=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabBuilder.RootPath).GetComponent<ScrewKingUiController>();
            Require(ui.Shortcut&&ui.Revisit&&ui.ShortcutEntry&&ui.RevisitEntry,"serialized popup and Home entry references");
            Directory.CreateDirectory("../Builds/TikTok");File.WriteAllText("../Builds/TikTok/capability-ledger-tests.json","{\"passed\":true,\"atomic_save\":true,\"eligibility_required\":true,\"once_per_account\":true,\"daily_revisit\":true,\"restart\":true,\"prefab_wiring\":true}");
            Debug.Log("TIKTOK_CAPABILITY_LEDGER_VERIFIED");
        }
    }
}
