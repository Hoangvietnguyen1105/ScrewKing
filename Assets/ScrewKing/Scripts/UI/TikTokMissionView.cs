using UnityEngine;
using UnityEngine.UI;
namespace ScrewKing
{
    public sealed class TikTokMissionView:MonoBehaviour
    {
        public TikTokMission Mission;
        public Text AmountLabel,StatusLabel,PrimaryLabel;
        public Button PrimaryButton,CheckButton;
        public RectTransform Content;
        float opened;
        Vector3 contentScale;
        bool captured;
        public void Show(bool visible)
        {
            if(!captured){captured=true;contentScale=Content.localScale;}
            if(gameObject.activeSelf==visible)return;
            if(visible)opened=Time.unscaledTime;gameObject.SetActive(visible);
            if(!visible)Content.localScale=contentScale;
        }
        public void Refresh(TikTokCapabilities features)
        {
            AmountLabel.text=features.RewardAmount(Mission).ToString();
            StatusLabel.text=features.StatusFor(Mission);
            bool claimed=features.Claimed(Mission),eligible=features.Eligible(Mission);
            PrimaryLabel.text=claimed?"Received":features.Busy?"Please wait…":!features.IsLive?"Open in TikTok":eligible?"Get reward":Mission==TikTokMission.Shortcut?"Add Shortcut":"Go to Sidebar";
            PrimaryButton.interactable=features.CanAct(Mission);
            if(CheckButton)CheckButton.interactable=!features.Busy&&features.Ready&&features.Supported(Mission)&&!claimed;
            float t=Mathf.Clamp01((Time.unscaledTime-opened)/.23f);float v=t-1;
            Content.localScale=contentScale*Mathf.LerpUnclamped(.9f,1,1+2.70158f*v*v*v+1.70158f*v*v);
        }
    }
}
