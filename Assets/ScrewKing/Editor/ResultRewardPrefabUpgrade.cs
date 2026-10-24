using System;
using UnityEditor;
using UnityEngine;

namespace ScrewKing.Editor
{
    public static class ResultRewardPrefabUpgrade
    {
        public static void RunThenSmoke()
        {
            VerifyRoundRewards();
            foreach(bool win in new[]{true,false}){
                string path=UiPrefabBuilder.Folder+(win?"/PopupWin.prefab":"/PopupLose.prefab");
                var root=PrefabUtility.LoadPrefabContents(path);
                try{UiPrefabBuilder.ApplyResultEconomy(root,win);PrefabUtility.SaveAsPrefabAsset(root,path);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            AssetDatabase.SaveAssets();UiPrefabPlaySmoke.Run();
        }
        static void Require(bool condition,string message){if(!condition)throw new Exception("Currency reward: "+message);}
        static void VerifyRoundRewards()
        {
            var r=new ScrewKingRoundReward();
            Require(!r.BeginDouble(out _),"cannot double before win");
            Require(r.GrantBase()&&!r.GrantBase(),"base grants once");
            Require(r.BeginDouble(out int first)&&!r.BeginDouble(out _),"reject overlapping requests");
            Require(!r.CompleteDouble(first,false)&&!r.Doubled&&!r.Pending,"cancel/fail grants nothing and allows retry");
            Require(r.BeginDouble(out int second),"retry after failure");
            Require(!r.CompleteDouble(first,true)&&r.Pending,"old callback cannot complete a new request");
            Require(r.CompleteDouble(second,true)&&r.Doubled&&!r.Pending,"completed grants double");
            Require(!r.CompleteDouble(second,true)&&!r.BeginDouble(out _),"duplicate callback/click cannot grant again");
            int stale=0,latest=0;
            r.Reset();Require(r.GrantBase()&&r.BeginDouble(out stale),"new round eligible");
            r.Reset();Require(r.GrantBase()&&r.BeginDouble(out latest),"next round eligible");
            Require(!r.CompleteDouble(stale,true)&&r.Pending&&!r.Doubled,"callback from previous round rejected");
            Require(r.CompleteDouble(latest,true),"current round still completes");
            Debug.Log("DOUBLE_REWARD_STATE_VERIFIED completed/cancel/failed/duplicate/overlap/stale/reset");
        }
    }
}
