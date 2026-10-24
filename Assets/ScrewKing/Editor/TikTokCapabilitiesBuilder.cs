using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
namespace ScrewKing.Editor
{
    public static class TikTokCapabilitiesBuilder
    {
        const string ShortcutPath=UiPrefabBuilder.Folder+"/PopupTikTokShortcut.prefab";
        const string RevisitPath=UiPrefabBuilder.Folder+"/PopupTikTokRevisit.prefab";
        [MenuItem("Screw King/TikTok/Create Capability Prefabs")]
        public static void CreateMissing()
        {
            const string settingsPath="Assets/ScrewKing/Resources/TikTok/Settings.asset";
            if(!AssetDatabase.LoadAssetAtPath<TikTokSettings>(settingsPath)){
                Directory.CreateDirectory(Path.GetDirectoryName(settingsPath));AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<TikTokSettings>(),settingsPath);
            }
            UiPrefabBuilder.CreateMissing();
            if(!File.Exists(ShortcutPath))SavePopup(ShortcutPath,TikTokMission.Shortcut);
            if(!File.Exists(RevisitPath))SavePopup(RevisitPath,TikTokMission.Revisit);
            var home=PrefabUtility.LoadPrefabContents(UiPrefabBuilder.Folder+"/ScreenHome.prefab");
            try{
                if(!home.transform.Find("TikTokShortcutEntry"))UiPrefabBuilder.CreateGrowthEntry(home.transform,true);
                if(!home.transform.Find("TikTokRevisitEntry"))UiPrefabBuilder.CreateGrowthEntry(home.transform,false);
                PrefabUtility.SaveAsPrefabAsset(home,UiPrefabBuilder.Folder+"/ScreenHome.prefab");
            }finally{PrefabUtility.UnloadPrefabContents(home);}
            var settings=PrefabUtility.LoadPrefabContents(UiPrefabBuilder.Folder+"/PopupSettings.prefab");
            try{
                var content=settings.transform.Find("Content");
                if(!content.Find("PrivacyLink"))UiPrefabBuilder.CreateLegalLink(content,true);
                if(!content.Find("TermsLink"))UiPrefabBuilder.CreateLegalLink(content,false);
                PrefabUtility.SaveAsPrefabAsset(settings,UiPrefabBuilder.Folder+"/PopupSettings.prefab");
            }finally{PrefabUtility.UnloadPrefabContents(settings);}
            var root=PrefabUtility.LoadPrefabContents(UiPrefabBuilder.RootPath);
            try{
                var ui=root.GetComponent<ScrewKingUiController>();
                if(!ui.Shortcut)ui.Shortcut=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ShortcutPath),ui.DesignArea)).GetComponent<TikTokMissionView>();
                if(!ui.Revisit)ui.Revisit=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RevisitPath),ui.DesignArea)).GetComponent<TikTokMissionView>();
                ui.ShortcutEntry=ui.Home.transform.Find("TikTokShortcutEntry").gameObject;ui.RevisitEntry=ui.Home.transform.Find("TikTokRevisitEntry").gameObject;
                ui.Shortcut.gameObject.SetActive(false);ui.Revisit.gameObject.SetActive(false);
                // The loading screen remains the final input-blocking sibling.
                ui.Loading.transform.SetAsLastSibling();PrefabUtility.SaveAsPrefabAsset(root,UiPrefabBuilder.RootPath);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();
        }
        static void SavePopup(string path,TikTokMission mission)
        {
            var root=UiPrefabBuilder.CreateMissionPopup(mission);PrefabUtility.SaveAsPrefabAsset(root,path);Object.DestroyImmediate(root);
        }
        public static void CreateAndVerify(){CreateMissing();UiPrefabBuilder.Validate();TikTokCapabilitiesVerification.Run();}
    }
}
