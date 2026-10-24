using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ScrewKing.Editor
{
    [InitializeOnLoad]
    public static class EditorLifecycleSmoke
    {
        const string Key="ScrewKing.EditorLifecycleSmoke.";
        static double deadline;
        static EditorLifecycleSmoke(){if(SessionState.GetBool(Key+"Active",false))Install();}
        static void Install()
        {
            Application.logMessageReceived-=OnLog;Application.logMessageReceived+=OnLog;
            EditorApplication.playModeStateChanged-=OnState;EditorApplication.playModeStateChanged+=OnState;
            EditorApplication.update-=Tick;EditorApplication.update+=Tick;
        }
        static void OnLog(string text,string stack,LogType type)
        {
            if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert)
            {
                SessionState.SetInt(Key+"Errors",SessionState.GetInt(Key+"Errors",0)+1);
                SessionState.SetString(Key+"LastError",text);
            }
        }
        public static void Run()
        {
            SessionState.SetBool(Key+"Active",true);SessionState.SetInt(Key+"Errors",0);
            SessionState.SetInt(Key+"Cycles",0);SessionState.SetInt(Key+"Phase",-1);Install();
            EditorSceneManager.OpenScene("Assets/ScrewKing/Scenes/ScrewKing.unity");
            // Reproduce the original serialization/constructor path while not in Play mode.
            var dormant=new GameObject("Never-awakened lifecycle check");dormant.AddComponent<PortGame>();
            UnityEngine.Object.DestroyImmediate(dormant);
            EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
        }
        static void OnState(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode)
            {SessionState.SetInt(Key+"Phase",0);deadline=EditorApplication.timeSinceStartup+1;}
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                int cycles=SessionState.GetInt(Key+"Cycles",0)+1;SessionState.SetInt(Key+"Cycles",cycles);
                if(cycles<2)EditorApplication.delayCall+=()=>EditorApplication.isPlaying=true;
                else Finish();
            }
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying || EditorApplication.timeSinceStartup<deadline)return;
            int phase=SessionState.GetInt(Key+"Phase",-1);
            if(phase<0 || phase>2)return;
            try
            {
                var game=UnityEngine.Object.FindFirstObjectByType<PortGame>();
                if(!game)throw new Exception("Play mode did not create PortGame");
                if(phase==0){game.StartLevel(2,12345);SessionState.SetInt(Key+"Phase",1);deadline=EditorApplication.timeSinceStartup+2;}
                else if(phase==1)
                {
                    if(game.Model==null || game.Model.Nails.Count!=120)throw new Exception("Level 2 failed to initialize");
                    var nail=game.Model.Nails.OrderByDescending(n=>n.Layer).First();
                    if(!game.TryTake(nail))throw new Exception("Level 2 does not accept a visible screw");
                    game.StartLevel(1,12345);SessionState.SetInt(Key+"Phase",2);deadline=EditorApplication.timeSinceStartup+2;
                }
                else
                {
                    if(game.Model==null || game.Model.Nails.Count!=9)throw new Exception("Level 1 failed after changing levels");
                    SessionState.SetInt(Key+"Phase",3);EditorApplication.isPlaying=false;
                }
            }
            catch(Exception ex)
            {SessionState.SetInt(Key+"Errors",SessionState.GetInt(Key+"Errors",0)+1);SessionState.SetString(Key+"LastError",ex.ToString());Finish();}
        }
        static void Finish()
        {
            int errors=SessionState.GetInt(Key+"Errors",0);SessionState.SetBool(Key+"Active",false);
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/EditorLifecycleVerification.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,"{\"play_stop_cycles\":"+SessionState.GetInt(Key+"Cycles",0)+",\"level_2_screws\":120,\"level_1_screws\":9,\"errors\":"+errors+",\"passed\":"+(errors==0?"true":"false")+"}");
            if(errors!=0)Debug.LogError("EDITOR_LIFECYCLE_FAILED "+SessionState.GetString(Key+"LastError",""));
            else Debug.Log("EDITOR_LIFECYCLE_PASSED "+output);
            if(Application.isBatchMode)EditorApplication.Exit(errors==0?0:1);
        }
    }
}
