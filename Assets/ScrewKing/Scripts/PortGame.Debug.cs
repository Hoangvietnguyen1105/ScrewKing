#if SK_DEBUG_CONSOLE
#if MB_TIKTOK
#error SK_DEBUG_CONSOLE is for internal web builds only and must never ship in the TikTok package.
#endif
using UnityEngine;

namespace ScrewKing
{
    // Internal test hooks used by ScrewKingDebugConsole. Compiled only by Screw King/Debug/Build Itch.io Debug Web.
    public sealed partial class PortGame
    {
        public const int DebugMaxLevel = 89, DebugCurrency = 999999;
        public int DebugLevel => level;
        public bool DebugInMenu => menu;
        public void DebugJump(int id)
        {
            id=Mathf.Clamp(id,1,DebugMaxLevel);
            bestLevel=Mathf.Max(bestLevel,id);PlayerPrefs.SetInt("ScrewKingPort.Level",bestLevel);PlayerPrefs.Save();
            StartLevel(id);
        }
        public void DebugUnlockAll(){bestLevel=DebugMaxLevel;PlayerPrefs.SetInt("ScrewKingPort.Level",bestLevel);PlayerPrefs.Save();if(menu)level=bestLevel;}
        public void DebugFillCurrency(){PlayerPrefs.SetInt(CurrencyKey,DebugCurrency);PlayerPrefs.Save();Play("coin");}
        void Start()=>ScrewKingDebugConsole.Attach(this);
    }
}
#endif
