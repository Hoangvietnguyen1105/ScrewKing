using UnityEngine;
namespace ScrewKing
{
    public sealed partial class PortGame
    {
        public TikTokCapabilities PlatformFeatures {get;private set;}
        public MoodBoost.Sdk.Tracking.ITrackingService PlatformTracking=>platform?.Context?.Tracking;
        void InitializePlatformFeatures()
        {
            PlatformFeatures=gameObject.AddComponent<TikTokCapabilities>();
            PlatformFeatures.Configure(this,Resources.Load<TikTokSettings>("TikTok/Settings"));
        }
        public void RestorePlatformProgress(TikTokPlatformSave save)
        {
            PlayerPrefs.SetInt(CurrencyKey,save.coins);PlayerPrefs.SetInt("ScrewKingPort.Level",save.bestLevel);
            PlayerPrefs.SetInt("ScrewKing.Music",save.music);PlayerPrefs.SetInt("ScrewKing.Sound",save.sound);PlayerPrefs.SetInt("ScrewKing.Vibration",save.vibration);PlayerPrefs.Save();
            bestLevel=save.bestLevel;if(menu)level=bestLevel;sound=save.sound!=0;musicOn=save.music!=0;vibrationOn=save.vibration!=0;musicSource.mute=!musicOn;
        }
        public void PlatformRewardSound()=>Play("add");
    }
}
