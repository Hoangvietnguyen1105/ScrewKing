using UnityEngine;
namespace ScrewKing
{
    [CreateAssetMenu(menuName="Screw King/TikTok Settings")]
    public sealed class TikTokSettings:ScriptableObject
    {
        [Tooltip("Public application identifier. Client secret belongs only on the server.")] public string AppId;
        [Tooltip("HTTPS backend endpoint that exchanges the temporary login code for a game session.")] public string LoginEndpoint;
        public string TermsUrl,PrivacyUrl;
        public bool EnableGrowthFeatures=true,ShowPlatformUiInPreview=true,AutoGuideAfterTutorial=true;
        [Min(1)] public int ShortcutReward=100,RevisitReward=50;
    }
}
