using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing
{
    public enum ScrewKingUiSetting { Music,Sound,Vibration }
    public sealed class ScrewKingUiToggle : MonoBehaviour
    {
        public ScrewKingUiSetting Setting;
        public GameObject OnHighlight,OffHighlight;
        public Text OnLabel,OffLabel;
        public Color OnColor=new Color(0,.4f,0),OffColor=new Color(.45f,0,0),InactiveColor=new Color(.87f,.77f,.43f);
        public void SetValue(bool value)
        {
            if(OnHighlight)OnHighlight.SetActive(value);
            if(OffHighlight)OffHighlight.SetActive(!value);
            if(OnLabel)OnLabel.color=value?OnColor:InactiveColor;
            if(OffLabel)OffLabel.color=value?InactiveColor:OffColor;
        }
    }
}
