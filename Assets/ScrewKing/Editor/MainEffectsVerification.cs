using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ScrewKing.Editor
{
    public static class MainEffectsVerification
    {
        static void Check(bool value,string label){if(!value)throw new Exception("Main effects: "+label);}
        public static void Run()
        {
            Check(Mathf.Abs(GameplayLayout.BoardCenterY-(-70))<.001f,"Cocos widget center");
            var probe=new Vector2(123,-321);
            Check(Vector2.Distance(GameplayLayout.ViewToPhysics(GameplayLayout.PhysicsToView(probe)),probe)<.001f,"input/render coordinate round trip");
            var data=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Data/Levels/2").text);
            using(var source=new CocosSourceRuntime())
            {
                var model=new PortModel(data,12345,source);model.Settle();
                var box=model.Boxes.First(b=>!b.Locked);
                var nails=model.Nails.Where(n=>n.Color==box.Color).Take(3).ToArray();
                Check(nails.Length==3,"three matching screws");
                model.NailPosition=n=>new Vector2(-200,-300);
                int lifted=0,landed=0;var sounds=new List<string>();
                model.OnLifted=n=>lifted++;model.OnLanded=b=>landed++;model.OnSound=sounds.Add;
                foreach(var nail in nails)Check(model.Take(nail),"reservation");
                model.Tick(.1f);
                Check(lifted==0 && landed==0 && model.CompletedBoxes==0,"no early arrival");
                Check(model.MotionVisual(nails[0],out var start,out float lift,out float angle,out float scale) && lift>0 && angle>0 && scale==1,"lift visible before release");
                for(int i=0;i<120 && model.CompletedBoxes==0;i++){
                    model.Tick(1f/60);
                    Check(model.AccountedScrews==model.Nails.Count,"conservation during animation");
                    if(model.MotionVisual(nails[0],out var position,out lift,out angle,out scale))Check(!float.IsNaN(position.x) && scale>=.8f && scale<=1,"finite visuals");
                }
                Check(lifted==3 && landed==3 && model.CompletedBoxes==1,"callbacks and completion");
                Check(model.ClosingBoxes.Count==1 && sounds.Contains("full") && !sounds.Contains("exit"),"lid precedes exit");
                model.Tick(.21f);Check(sounds.Contains("exit") && model.ClosingBoxes.Count==1,"exit after lid");
                model.Tick(.4f);Check(model.ClosingBoxes.Count==0,"closing cleanup");
            }
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/TikTok/MainEffectsVerification.json"));Directory.CreateDirectory(Path.GetDirectoryName(output));
            File.WriteAllText(output,"{\"passed\":true,\"checks\":[\"reservation\",\"lift-before-release\",\"finite-visuals\",\"conservation\",\"three-arrival-callbacks\",\"lid-before-exit\",\"closing-cleanup\"]}");
            Debug.Log("MAIN_EFFECTS_VERIFIED "+output);
        }
    }
}
