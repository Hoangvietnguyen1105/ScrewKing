using System;
using UnityEngine;
namespace ScrewKing
{
    [CreateAssetMenu(menuName="Screw King/TikTok Settings")]
    public sealed class TikTokSettings:ScriptableObject
    {
        [Tooltip("Public application identifier. Client secret belongs only on the server.")] public string AppId;
        public string TermsUrl,PrivacyUrl;
        public bool EnableGrowthFeatures=true,ShowPlatformUiInPreview=true,AutoGuideAfterTutorial=true;
        [Min(1)] public int ShortcutReward=100,RevisitReward=50;
        public static bool IsHttps(string value)=>Uri.TryCreate(value,UriKind.Absolute,out var u)&&u.Scheme==Uri.UriSchemeHttps&&!string.IsNullOrWhiteSpace(u.Host)&&string.IsNullOrEmpty(u.UserInfo);
    }
}
