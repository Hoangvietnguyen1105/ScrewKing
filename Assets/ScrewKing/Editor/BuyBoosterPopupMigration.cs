using System.IO;
using UnityEditor;
using UnityEngine;

namespace ScrewKing.Editor
{
    // Runs once after scripts reload: adds PopupBuyBooster to the UI root and fixes the wrapping reward title.
    // Idempotent, so later reloads are no-ops and designer edits to the new popup are kept.
    [InitializeOnLoad]
    public static class BuyBoosterPopupMigration
    {
        // Also retried on return to Edit mode: a recompile triggered by pressing Play reloads mid-transition, where Run() must skip.
        static BuyBoosterPopupMigration()
        {
            EditorApplication.delayCall+=()=>{if(!Application.isBatchMode)Run();};
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredEditMode&&!Application.isBatchMode)Run();};
        }
        [MenuItem("Screw King/UI/Add Buy Booster Popup")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(UiPrefabBuilder.RootPath))return;
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabBuilder.RootPath);var ui=root?root.GetComponent<ScrewKingUiController>():null;
            bool missing=!File.Exists(UiPrefabBuilder.Folder+"/PopupBuyBooster.prefab")||ui&&!ui.BuyBooster;
            if(missing){UiPrefabBuilder.CreateMissing();UiPrefabBuilder.UpgradeBuyBooster();}
            bool title=UiPrefabBuilder.FixRewardTitle();
            if(missing||title){AssetDatabase.SaveAssets();Debug.Log("BUY_BOOSTER_POPUP_READY created="+missing+" rewardTitleFixed="+title);}
        }
    }
}
