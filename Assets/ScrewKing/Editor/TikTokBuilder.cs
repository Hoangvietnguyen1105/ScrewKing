using System;
using System.IO;
using System.Linq;
using MoodBoost.Sdk.Unity;
using MoodBoost.Sdk.Ads;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ScrewKing.Editor
{
    public static class TikTokBuilder
    {
        static string Output=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/TikTok"));
        public static SdkSettings EnsureSdkSettings()
        {
            const string path="Assets/SDKSetting/SdkSettings.asset";
            var settings=AssetDatabase.LoadAssetAtPath<SdkSettings>(path);
            if(!settings){Directory.CreateDirectory("Assets/SDKSetting");settings=ScriptableObject.CreateInstance<SdkSettings>();AssetDatabase.CreateAsset(settings,path);AssetDatabase.SaveAssets();}
            return settings;
        }
        public static void ConfigureBaseSdk()
        {
            EnsureSdkSettings();TikTokCapabilitiesBuilder.CreateMissing();
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL,"MB_TIKTOK");
            PortBuilder.MakeScene();AssetDatabase.SaveAssets();
        }
        public static void ValidateTikTokPlayer()
        {
            ConfigureBaseSdk();
            var settings=new UnityEditor.Build.Player.ScriptCompilationSettings{
                group=BuildTargetGroup.WebGL,target=BuildTarget.WebGL,
                extraScriptingDefines=new[]{"MB_TIKTOK"}
            };
            var path=Path.Combine(Output,"TikTokPlayerScripts");Directory.CreateDirectory(path);
            var result=UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings,path);
            if(result.assemblies==null || result.assemblies.Count==0)throw new Exception("TikTok player compilation failed");
            File.WriteAllText(Path.Combine(Output,"sdk-tiktok-player-compile.json"),"{\"passed\":true,\"define\":\"MB_TIKTOK\",\"assemblies\":"+result.assemblies.Count+"}");
            Debug.Log("TIKTOK_PLAYER_SCRIPTS_VERIFIED");
        }
        [MenuItem("Screw King/TikTok/Preflight")]
        public static void Preflight()
        {
            Directory.CreateDirectory(Output);
            bool webgl=BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL);
            var config=Resources.Load<TikTokSettings>("TikTok/Settings");
            var sdk=EnsureSdkSettings();
            var report=new PreflightReport{baseSdkPresent=Directory.Exists("Assets/_Dev/Sdk"),sdkSettingsPresent=sdk!=null,tiktokDefineEnabled=PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL).Split(';').Contains("MB_TIKTOK"),unity=Application.unityVersion,webglModule=webgl,appidConfigured=config&&!string.IsNullOrWhiteSpace(config.AppId),rewardedConfigured=sdk.Mediations.Any(m=>m.adUnits.rewarded!=null && m.adUnits.rewarded.Any(x=>!string.IsNullOrWhiteSpace(x.Current))),sdkPresent=Directory.Exists("Assets/Plugins/com.tiktok.minigame"),bridgePresent=File.Exists("Assets/Plugins/WebGL/ScrewKingSource.jslib"),levelCount=Resources.LoadAll<TextAsset>("Data/Levels").Length,termsConfigured=config&&TikTokSettings.IsHttps(config.TermsUrl),privacyConfigured=config&&TikTokSettings.IsHttps(config.PrivacyUrl),growthFeaturesEnabled=config&&config.EnableGrowthFeatures,growthPrefabsPresent=File.Exists(UiPrefabBuilder.Folder+"/PopupTikTokShortcut.prefab")&&File.Exists(UiPrefabBuilder.Folder+"/PopupTikTokRevisit.prefab"),supportedReleaseUnity=Application.unityVersion.StartsWith("2021.")||Application.unityVersion.StartsWith("2022."),liveDeviceVerified=false};
            File.WriteAllText(Path.Combine(Output,"preflight.json"),JsonUtility.ToJson(report,true));
            Debug.Log("TIKTOK_PREFLIGHT "+JsonUtility.ToJson(report));
        }
        [Serializable] sealed class PreflightReport{public string unity;public bool baseSdkPresent,sdkSettingsPresent,tiktokDefineEnabled,webglModule,appidConfigured,rewardedConfigured,sdkPresent,bridgePresent,termsConfigured,privacyConfigured,growthFeaturesEnabled,growthPrefabsPresent,supportedReleaseUnity,liveDeviceVerified;public int levelCount;}
        public static void ValidateEditor(){Preflight();SdkTrackingVerification.Run();PortBuilder.ValidateReskin();MainEffectsVerification.Run();PortBuilder.Validate();}
        [MenuItem("Screw King/TikTok/Build Browser Preview")]
        public static void BuildBrowserPreview()=>BuildBrowser(false);
        [MenuItem("Screw King/TikTok/Build Optimized Browser Preview")]
        public static void BuildBrowserRelease()=>BuildBrowser(true);
        static void BuildBrowser(bool release)
        {
            Preflight();
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL,BuildTarget.WebGL))throw new InvalidOperationException("Install WebGL Build Support for this Unity version first.");
            PortBuilder.MakeScene();
            var previousSymbols=PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.WebGL);
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL,"");
            try{
                if(release){
                    PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL,ManagedStrippingLevel.Medium);
                    PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL,Il2CppCodeGeneration.OptimizeSize);
                    UnityEditor.WebGL.UserBuildSettings.codeOptimization=UnityEditor.WebGL.WasmCodeOptimization.DiskSize;
                }
                PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.decompressionFallback=false;
                PlayerSettings.runInBackground=false;
                PlayerSettings.WebGL.template="APPLICATION:Default";
                var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/ScrewKing/Scenes/ScrewKing.unity"},locationPathName=Path.Combine(Output,release?"BrowserRelease":"BrowserPreview"),target=BuildTarget.WebGL,options=release?BuildOptions.None:BuildOptions.Development,extraScriptingDefines=Array.Empty<string>()});
                File.WriteAllText(Path.Combine(Output,release?"browser-release-build.json":"browser-build.json"),"{\"result\":\""+result.summary.result+"\",\"errors\":"+result.summary.totalErrors+",\"bytes\":"+result.summary.totalSize+"}");
                if(result.summary.result!=BuildResult.Succeeded)throw new Exception("WebGL build failed");
                Debug.Log("SCREWKING_WEBGL_BUILD_SUCCEEDED "+Output);
            }finally{PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL,previousSymbols);}

        }
        [MenuItem("Screw King/TikTok/Build TikTok Package")]
        public static void BuildTikTokPackage()
        {
            Preflight();
            var config=Resources.Load<TikTokSettings>("TikTok/Settings");
            if(!config||string.IsNullOrWhiteSpace(config.AppId))throw new InvalidOperationException("App ID required: create Resources/TikTok/Settings.asset using Screw King/TikTok Settings. Never include a client secret.");
            if(!TikTokSettings.IsHttps(config.TermsUrl)||!TikTokSettings.IsHttps(config.PrivacyUrl))throw new InvalidOperationException("Configure HTTPS Terms URL and Privacy URL in TikTok/Settings before release.");
            if(!EnsureSdkSettings().Mediations.Any(m=>m.adUnits.rewarded!=null&&m.adUnits.rewarded.Any(x=>!string.IsNullOrWhiteSpace(x.Current))))throw new InvalidOperationException("Configure a rewarded ad unit before release.");
            if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.WebGL)throw new InvalidOperationException("Select WebGL before TikTok packaging.");
            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.WebGL,"MB_TIKTOK");
            AssetDatabase.SaveAssets();
            // The vendor window owns conversion and packaging. Do not impersonate its output with a ZIP of a web build.
            var type=Type.GetType("TTSDK.Tool.StarkSDKToolWindow, ttsdk_tools");
            if(type==null)throw new InvalidOperationException("TTSDK build tools are not loaded.");
            EditorWindow.GetWindow(type).Show();
            Debug.Log("Configure the same public App ID in TikTokGame/Build Minigame and run the vendor Build. Device validation is required before upload.");
        }
    }
}
