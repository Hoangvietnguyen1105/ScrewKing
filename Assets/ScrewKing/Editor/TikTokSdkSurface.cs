using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
namespace ScrewKing.Editor
{
    public static class TikTokSdkSurface
    {
        public static void Run()
        {
            var text=new StringBuilder();var type=typeof(TTSDK.TT);var all=type.Assembly.GetTypes();
            foreach(var m in type.GetMethods().Where(m=>new[]{"InitSDK","Login","AddShortcut","GetShortcutMissionReward","StartEntranceMission","GetEntranceMissionReward","CanIUse","GetAppLifeCycle","Save","LoadSaving"}.Contains(m.Name)))text.AppendLine(m.ToString());
            foreach(var t in all.Where(t=>t.Name.Contains("EntranceMission")||t.Name.Contains("ShortcutMission")||t.Name.Contains("AppLifeCycle")||t.Name.Contains("Login")||t.FullName.StartsWith("TTSDK.CanIUse")||t.Name=="OnAppShowEventWithDict")){text.AppendLine("TYPE "+t.FullName);foreach(var f in t.GetFields())text.AppendLine(" FIELD "+f.FieldType+" "+f.Name);foreach(var p in t.GetProperties())text.AppendLine(" PROPERTY "+p.PropertyType+" "+p.Name);foreach(var m in t.GetMethods().Where(m=>m.Name=="Invoke"))text.AppendLine(" INVOKE "+m);foreach(var e in t.GetEvents())text.AppendLine(" EVENT "+e.EventHandlerType+" "+e.Name);}
            Directory.CreateDirectory("../Builds/TikTok");File.WriteAllText("../Builds/TikTok/sdk-capability-surface.txt",text.ToString());
        }
    }
}
