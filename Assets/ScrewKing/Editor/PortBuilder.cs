using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ScrewKing.Editor
{
    public sealed class ArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/ScrewKing/Resources/Art/") &&
                !assetPath.StartsWith("Assets/ScrewKing/Resources/NewArt/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }
    }

    public static class PortBuilder
    {
        static string Root => Directory.GetParent(Application.dataPath).FullName;
        static string Deliverables => Path.Combine(Directory.GetParent(Root).FullName, "Builds", "TikTok");
        static void Require(bool condition, string description)
        {
            if (!condition) throw new Exception("PORT CHECK FAILED: " + description);
        }

        [Serializable] sealed class OracleBox { public int color,count; public bool center; }
        [Serializable] sealed class OracleInput { public bool[] holes;public OracleBox[] boxes;public int color; }
        [Serializable] sealed class OracleExpected { public bool accepted;public string route; }
        [Serializable] sealed class OracleCase { public OracleInput input;public OracleExpected expected; }
        [Serializable] sealed class OraclePose { public int steps,key;public double x,y,angle; }
        [Serializable] sealed class Oracle { public string v8;public int removalStep;public OracleCase[] cases;public OraclePose[] poses; }
        static LevelData Level(int id)=>JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Data/Levels/"+id).text);

        [MenuItem("Screw King/Validate all 89 levels")]
        public static void Validate()
        {
            Directory.CreateDirectory(Deliverables);
            var mobileTwo=Level(2);
            Require(mobileTwo.layers.Length==12 && mobileTwo.layers[11].boards.Length==1 && mobileTwo.layers[11].boards[0].id==86,
                "mobile level 2 uses the bundled smiling face");
            Require(mobileTwo.layers[10].boards.Length==4 && mobileTwo.layers[10].boards.All(b=>b.id==28),
                "mobile level 2 has four clovers immediately below the face");
            var shapeIds=new HashSet<int>(JsonUtility.FromJson<ShapeCatalog>(Resources.Load<TextAsset>("Data/shapes").text).shapes.Select(s=>s.id));
            Require(shapeIds.Count==108,"108 original polygons");
            int passed=0,totalScrews=0,prefixMoves=0,physicsBoards=0;
            using(var source=new CocosSourceRuntime())
            {
                for(int id=1;id<=89;id++)foreach(int seed in new[]{12345,42})
                {
                    var model=new PortModel(Level(id),seed,source);
                    foreach(var layer in model.Level.layers)foreach(var board in layer.boards)
                    {
                        Require(shapeIds.Contains(board.id),"source polygon "+board.id);
                        Require(Resources.Load<Sprite>("Art/Boards/icon/"+board.id)!=null,"source texture "+board.id);
                    }
                    Require(model.Nails.All(n=>n.Color>=1&&n.Color<=11),"original generator assigned all colors, level "+id);
                    Require(model.Nails.GroupBy(n=>n.Color).All(g=>g.Count()%3==0),"source triplets, level "+id);
                    Require((model.PendingBoxes.Count+model.Boxes.Count(x=>!x.Locked))*3==model.Nails.Count,"source box budget, level "+id);
                    Require(model.Boxes.Count(x=>x.Locked)==2 && model.HoleCapacity==5,"source startup locks/buffer");
                    Require(model.Boxes.Where(x=>!x.Locked).All(x=>!x.IsCenter(model.Clock)),"entering boxes are not instantly eligible");
                    model.Settle();
                    foreach(var nail in model.Nails.OrderByDescending(n=>n.Layer).ToArray())
                    {
                        if(!model.Ready)break;
                        Require(model.Take(nail),"prefix input accepted");prefixMoves++;
                        Require(model.AccountedScrews==model.Nails.Count,"reservation conservation");
                        model.Settle();Require(model.AccountedScrews==model.Nails.Count,"arrival/closing conservation");
                        if(model.Prompt!=SourcePrompt.None)model.RewardResult(false);
                    }
                    if(seed==12345)
                    {
                        source.ResetPhysics();int key=0;
                        for(int layer=model.Level.layers.Length-1;layer>=Math.Max(0,model.Level.layers.Length-model.Level.show);layer--)
                            for(int index=0;index<model.Level.layers[layer].boards.Length;index++)
                            {source.AddBoard(key,layer,index);source.ShowBoard(key,true);key++;}
                        for(int frame=0;frame<20;frame++)
                            foreach(var pose in source.Step(1f/60).boards)
                                Require(!double.IsNaN(pose.x)&&!double.IsInfinity(pose.x)&&!double.IsNaN(pose.y)&&!double.IsNaN(pose.angle),"finite source physics, level "+id);
                        physicsBoards+=key;totalScrews+=model.Nails.Count;
                    }
                    passed++;
                }
                var oracle=JsonUtility.FromJson<Oracle>(Resources.Load<TextAsset>("Data/SourceOracle").text);
                int routeCases=0;
                foreach(var test in oracle.cases)
                {
                    var model=new PortModel(Level(1),12345,source);model.Settle();model.Boxes.Clear();
                    foreach(var b in test.input.boxes)
                    {
                        var box=new BoxState{Color=b.color,FromX=b.center?0:-470,TargetX=b.center?0:-470};
                        for(int n=0;n<b.count;n++)box.Screws.Add(b.color);model.Boxes.Add(box);
                    }
                    var nail=model.Nails[0];nail.Color=test.input.color;
                    for(int i=0;i<5;i++)if(!test.input.holes[i])model.Seats[i].Screw=model.Nails[i+1];
                    int[] before=model.Boxes.Select(x=>x.Screws.Count).ToArray();
                    bool accepted=model.Take(nail);string route="reject";
                    if(accepted)
                    {
                        for(int i=0;i<model.Boxes.Count;i++)if(model.Boxes[i].Screws.Count>before[i])route="box:"+i;
                        for(int i=0;i<model.Seats.Count;i++)if(ReferenceEquals(model.Seats[i].Screw,nail))route="hole:"+i;
                    }
                    Require(accepted==test.expected.accepted && route==test.expected.route,"original unscrew route "+routeCases);
                    string original=source.Eval("SourceRoute("+Quote(JsonUtility.ToJson(test.input))+")");
                    var direct=JsonUtility.FromJson<OracleExpected>(original);
                    Require(direct.accepted==accepted&&direct.route==route,"unmodified source call under JavaScriptCore");routeCases++;
                }
                // Execute the same shipped Box2D on V8 (fixture) and native JavaScriptCore (Unity backend).
                source.GenerateRaw(Resources.Load<TextAsset>("Data/PhysicsOracleLevel").text,12345);source.ResetPhysics();source.AddBoard(0,0,0);source.ShowBoard(0,true);
                for(int i=0;i<36;i++)source.Step(1f/60);source.Dynamic(0);source.Release(0,0);source.Release(0,1);source.Release(0,2);
                double maxPixelError=0,maxAngleError=0;int comparisons=0,removalStep=0;
                for(int step=1;step<=180;step++)
                {
                    if(step==121)source.Release(0,3);
                    var snapshot=source.Step(1f/60);if(snapshot.boards.Length==0){if(removalStep==0)removalStep=step;continue;}var pose=snapshot.boards[0];
                    var expected=oracle.poses.FirstOrDefault(x=>x.steps==step);if(expected==null)continue;
                    maxPixelError=Math.Max(maxPixelError,Math.Max(Math.Abs(pose.x-expected.x),Math.Abs(pose.y-expected.y)));
                    maxAngleError=Math.Max(maxAngleError,Math.Abs(pose.angle-expected.angle));comparisons++;
                }
                Require(removalStep==oracle.removalStep,"original floor-trigger removal step");
                Require(maxPixelError<.0001 && maxAngleError<.00001,"source Box2D trajectory across JS engines");
                var timed=new PortModel(Level(1),12345,source);timed.Settle();
                var firstColor=timed.Boxes.First(x=>!x.Locked).Color;
                var triplet=timed.Nails.Where(x=>x.Color==firstColor).Take(3).ToArray();
                foreach(var n in triplet)Require(timed.Take(n),"reserve triplet");
                Require(timed.Cleared==0&&timed.DoScrewCount==3,"no immediate full-box resolution");
                timed.Tick(.15f);Require(timed.Cleared==0,"lift phase does not complete box");
                timed.Settle();Require(timed.CompletedBoxes==1&&timed.DoScrewCount==0,"full box only after screw arrival");
                Require(timed.AccountedScrews==timed.Nails.Count,"timed box replacement conservation");
                var cache=new PortModel(Level(1),12345,source);cache.Settle();
                var unmatched=cache.Nails.First(n=>cache.Boxes.All(b=>b.Color!=n.Color));
                Require(cache.Take(unmatched),"unmatched screw enters buffer");cache.Settle();
                int oldCleared=cache.Cleared;cache.ClearHoles();
                Require(cache.Holes.Count==0&&cache.Cache.Values.Sum()==1&&cache.Cleared==oldCleared,"clear stores rather than inventing immediate cache fill");
                Require(cache.AccountedScrews==cache.Nails.Count,"cache conservation");
                string report="{\"backend\":\"unmodified APK generation + shipped Box2D, native JavaScriptCore\",\"generation_runs\":"+passed+",\"levels\":89,\"original_screws\":"+totalScrews+",\"conservation_prefix_moves\":"+prefixMoves+",\"physics_corpus_levels\":89,\"physics_boards_checked\":"+physicsBoards+",\"floor_removal_step\":"+removalStep+",\"source_route_cases\":"+routeCases+",\"physics_pose_comparisons\":"+comparisons+",\"max_physics_pixel_error\":"+maxPixelError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"max_physics_angle_error\":"+maxAngleError.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+",\"timing_checks\":true,\"cache_checks\":true}";
                File.WriteAllText(Path.Combine(Deliverables,"SourceParityVerification.json"),report);Debug.Log("SOURCE_PARITY "+report);
            }
        }
        static string Quote(string x)=>"\""+x.Replace("\\","\\\\").Replace("\"","\\\"")+"\"";

        [MenuItem("Screw King/Create playable scene")]
        public static void MakeScene()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera");camera.tag="MainCamera";camera.AddComponent<Camera>();camera.AddComponent<AudioListener>();
            var game=new GameObject("Screw King • Core Port").AddComponent<PortGame>();
            game.SdkSettings=TikTokBuilder.EnsureSdkSettings();
            UiPrefabBuilder.Attach(game);
            Directory.CreateDirectory(Path.Combine(Application.dataPath,"ScrewKing/Scenes"));
            const string path="Assets/ScrewKing/Scenes/ScrewKing.unity";
            EditorSceneManager.SaveScene(scene,path);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(path,true)};
            PlayerSettings.companyName="Local Study";
            PlayerSettings.productName="Screw King Source";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone,"com.localstudy.screwking.source");
            PlayerSettings.bundleVersion="0.2.0";
            PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
            PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;
            PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Gamma;
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Screw King/Validate new art")]
        public static void ValidateReskin()
        {
            for(int color=1;color<=11;color++)
            {
                foreach(var path in new[]{ReskinArt.Screw(color),ReskinArt.Box(color),ReskinArt.Lid(color)})
                {
                    Require(Resources.Load<Sprite>(path)!=null,"reskin sprite "+path);
                    Require(Resources.Load<Texture2D>(path)!=null,"reskin UI texture "+path);
                    var importer=(TextureImporter)AssetImporter.GetAtPath("Assets/ScrewKing/Resources/"+path+".png");
                    Require(!importer.mipmapEnabled && importer.spritePixelsPerUnit==100,"reskin import settings "+path);
                }
            }
            foreach(var name in new[]{"bg","Layer 5 copy 4","q copy 5","Layer 9","Layer 12","Layer 16 copy 2","Layer 17 copy 2","Layer 18","Layer 33","Layer 34","Layer 35"})
                Require(Resources.Load<Sprite>(ReskinArt.UI+name)!=null,"reskin UI "+name);
            Debug.Log("RESKIN_ART_VALIDATED 11 color families and 11 UI assets");
        }

        [MenuItem("Screw King/Validate simple LitMotion flight")]
        public static void ValidateSimpleFlight()
        {
            using(var flight=new SimpleScrewFlight())
            {
                var start=new Vector2(10,-300);var target=new Vector2(80,500);
                flight.Tick(5); // Simulate waiting on the level before the very first click.
                flight.Begin(1,start);flight.Tick(0);
                Require(Vector2.Distance(flight.Position(1,target),start)<.01,"flight begins at screw position");
                Vector2 previous=start;
                for(int i=0;i<30;i++)
                {
                    flight.Tick(1f/60);
                    var p=flight.Position(1,target);
                    Require(p.x>=previous.x-.01 && p.y>=previous.y-.01 && p.x<=target.x+.01 && p.y<=target.y+.01,"flight has no overshoot or reversal");
                    previous=p;
                }
                Require(Vector2.Distance(previous,target)<.01,"flight reaches seat");
                flight.Begin(1,new Vector2(-20,20));flight.Begin(2,new Vector2(50,50));flight.Tick(0);
                Require(Vector2.Distance(flight.Position(1,target),new Vector2(-20,20))<.01,"reused screw cancels old tween");
                flight.Reset();flight.Tick(.5f);
                Require(flight.Position(1,target)==target,"restart cancels active flights");
                flight.Begin(3,start);flight.Tick(1f/60);
                Require(Vector2.Distance(flight.Position(3,target),start)<20,"first click after reset animates rather than teleporting");
            }
            Debug.Log("SIMPLE_LITMOTION_VALIDATED monotonic flight, endpoint, reuse, reset");
        }

        public static void BuildMac()
        {
            try
            {
                ValidateReskin();MainEffectsVerification.Run();Validate();MakeScene();
                var options=new BuildPlayerOptions
                {
                    scenes=new[]{"Assets/ScrewKing/Scenes/ScrewKing.unity"},
                    locationPathName=Path.Combine(Deliverables,"Screw King Main Effects.app"),
                    target=BuildTarget.StandaloneOSX,
                    options=BuildOptions.None
                };
                var report=BuildPipeline.BuildPlayer(options);
                File.WriteAllText(Path.Combine(Deliverables,"MainEffectsBuildResult.json"),"{\"result\":\""+report.summary.result+"\",\"bytes\":"+report.summary.totalSize+",\"errors\":"+report.summary.totalErrors+"}");
                if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
                Debug.Log("PORT_BUILD_SUCCEEDED "+options.locationPathName);
            }
            catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}
        }
    }
}
