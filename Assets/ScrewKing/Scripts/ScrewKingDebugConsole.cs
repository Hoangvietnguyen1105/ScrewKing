#if SK_DEBUG_CONSOLE
#if MB_TIKTOK
#error SK_DEBUG_CONSOLE is for internal web builds only and must never ship in the TikTok package.
#endif
using System.Collections.Generic;
using UnityEngine;

namespace ScrewKing
{
    /// <summary>
    /// Internal test console for the itch.io web build: level jump, no ads, max coins and a log view.
    /// IMGUI keeps it free of prefabs and scenes. Hidden mode draws the DBG button fully transparent for video
    /// capture, but it stays tappable in the same place; the ` key also toggles it.
    /// Labels stay ASCII: the web build has no system fonts for Vietnamese glyphs.
    /// </summary>
    public sealed class ScrewKingDebugConsole : MonoBehaviour
    {
        const string NoAdsKey = "ScrewKingDebug.NoAds", HiddenKey = "ScrewKingDebug.Hidden";
        const float RefHeight = 960;
        const int MaxLogs = 200;
        static ScrewKingDebugConsole instance;
        public static bool NoAds { get; private set; } = true;

        PortGame game;
        bool open, hidden, logs;
        int target = 1;
        string typed = "";
        Vector2 scroll;
        readonly Queue<string> lines = new Queue<string>();
        GUIStyle button, label, box, logLabel;
        float scale = 1;
        Rect toggleRect, panelRect;

        public static void Attach(PortGame game)
        {
            if(instance)return;
            instance=game.gameObject.AddComponent<ScrewKingDebugConsole>();
            instance.game=game;
        }

        // Taps on the console must not also pull screws behind it.
        public static bool BlocksPointer(Vector2 screen)
        {
            if(!instance)return false;
            var gui=new Vector2(screen.x,Screen.height-screen.y)/instance.scale;
            return instance.toggleRect.Contains(gui) || (instance.open && instance.panelRect.Contains(gui));
        }

        void Awake()
        {
            NoAds=PlayerPrefs.GetInt(NoAdsKey,1)!=0;
            hidden=PlayerPrefs.GetInt(HiddenKey,0)!=0;
            Application.logMessageReceived+=OnLog;
        }
        void OnDestroy(){Application.logMessageReceived-=OnLog;if(instance==this)instance=null;}
        void OnLog(string message,string stack,LogType type)
        {
            string prefix=type==LogType.Log?"":"["+type+"] ";
            lines.Enqueue(prefix+message);
            while(lines.Count>MaxLogs)lines.Dequeue();
            if(logs)scroll.y=float.MaxValue;
        }

        void Update()
        {
            if(Input.GetKeyDown(KeyCode.BackQuote))SetHidden(!hidden);
        }
        void SetHidden(bool value){hidden=value;PlayerPrefs.SetInt(HiddenKey,value?1:0);PlayerPrefs.Save();}
        void SetNoAds(bool value){NoAds=value;PlayerPrefs.SetInt(NoAdsKey,value?1:0);PlayerPrefs.Save();}

        void Styles()
        {
            if(button!=null)return;
            button=new GUIStyle(GUI.skin.button){fontSize=20,fontStyle=FontStyle.Bold};
            label=new GUIStyle(GUI.skin.label){fontSize=20,alignment=TextAnchor.MiddleCenter};
            box=new GUIStyle(GUI.skin.box);
            logLabel=new GUIStyle(GUI.skin.label){fontSize=14,wordWrap=true};
        }

        void OnGUI()
        {
            if(!game)return;
            Styles();
            scale=Screen.height/RefHeight;
            GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float width=Screen.width/scale;
            // Mid-left edge stays clear of TikTok's reserved top and bottom bands.
            toggleRect=new Rect(4,RefHeight*.42f,64,44);
            GUI.color=new Color(1,1,1,hidden?0:open?1:.6f);
            if(GUI.Button(toggleRect,"DBG",button))open=!open;
            GUI.color=Color.white;
            if(!open){panelRect=Rect.zero;return;}

            float w=Mathf.Min(width-80,420),h=logs?640:420;
            panelRect=new Rect(74,Mathf.Max(4,RefHeight*.42f-140),w,h);
            GUI.backgroundColor=new Color(0,0,0,.85f);GUI.Box(panelRect,GUIContent.none,box);GUI.backgroundColor=Color.white;
            GUILayout.BeginArea(new Rect(panelRect.x+10,panelRect.y+10,w-20,h-20));

            GUILayout.Label("Current level: "+game.DebugLevel+(game.DebugInMenu?" (home)":""),label);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button("-10",button,GUILayout.Height(48)))target-=10;
            if(GUILayout.Button("-1",button,GUILayout.Height(48)))target--;
            target=Mathf.Clamp(target,1,PortGame.DebugMaxLevel);
            GUILayout.Label(target.ToString(),label,GUILayout.Width(60),GUILayout.Height(48));
            if(GUILayout.Button("+1",button,GUILayout.Height(48)))target++;
            if(GUILayout.Button("+10",button,GUILayout.Height(48)))target+=10;
            target=Mathf.Clamp(target,1,PortGame.DebugMaxLevel);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            typed=GUILayout.TextField(typed,4,new GUIStyle(GUI.skin.textField){fontSize=20},GUILayout.Width(90),GUILayout.Height(48));
            if(int.TryParse(typed,out int value)){target=Mathf.Clamp(value,1,PortGame.DebugMaxLevel);}
            if(GUILayout.Button("Play level "+target,button,GUILayout.Height(48))){typed="";game.DebugJump(target);open=false;}
            GUILayout.EndHorizontal();

            if(GUILayout.Button("Unlock all levels",button,GUILayout.Height(44)))game.DebugUnlockAll();
            if(GUILayout.Button("Max coins ("+PortGame.DebugCurrency+")",button,GUILayout.Height(44)))game.DebugFillCurrency();
            if(GUILayout.Button("Block ads: "+(NoAds?"ON":"OFF"),button,GUILayout.Height(44)))SetNoAds(!NoAds);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(logs?"Hide logs":"Show logs",button,GUILayout.Height(44)))logs=!logs;
            if(GUILayout.Button(hidden?"Show DBG button":"Hide DBG button",button,GUILayout.Height(44))){SetHidden(!hidden);open&=!hidden;}
            GUILayout.EndHorizontal();
            if(logs)
            {
                if(GUILayout.Button("Clear logs",button,GUILayout.Height(36)))lines.Clear();
                scroll=GUILayout.BeginScrollView(scroll);
                foreach(var line in lines)GUILayout.Label(line,logLabel);
                GUILayout.EndScrollView();
            }
            GUILayout.EndArea();
        }
    }
}
#endif
