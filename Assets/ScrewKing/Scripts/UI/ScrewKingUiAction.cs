using UnityEngine;

namespace ScrewKing
{
    public enum ScrewKingUiCommand { Play, Previous, NextSelection, Settings, Resume, Quit, NextLevel, Retry, CloseResult, WatchReward, CancelReward, MusicOn, MusicOff, SoundOn, SoundOff, VibrationOn, VibrationOff, AddSlot, ClearSlots, GlassHammer, DoubleReward, TikTokShortcut, TikTokRevisit, TikTokMissionPrimary, TikTokMissionCheck, TikTokMissionClose, Privacy, Terms }
    // Each Button's persistent OnClick points here; the command is editable in the Inspector.
    public sealed class ScrewKingUiAction : MonoBehaviour
    {
        public ScrewKingUiCommand Command;
        public void Invoke()
        {
            var ui=GetComponentInParent<ScrewKingUiController>();
            if(ui)ui.Invoke(Command);
        }
    }
}
