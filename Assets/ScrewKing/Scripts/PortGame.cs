using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ScrewKing
{
    public sealed partial class PortGame : MonoBehaviour
    {
        public PortModel Model { get; private set; }
        bool runtimeReady;
        TikTokPlatform platform;
        public MoodBoost.Sdk.Unity.SdkSettings SdkSettings;
        SourcePrompt observedPrompt;
        bool sdkChangingLevel;
        BoardData guideBoard;
        Camera camera2d;
        CocosSourceRuntime source;
        bool resultPlayed;
        double wonAt, lostAt;
        bool failPlayed;
        float transitionUntil;
        readonly Dictionary<int,float> shakes=new Dictionary<int,float>();
        Dictionary<int, ShapeData> shapes;
        readonly List<BoardActor> boards = new List<BoardActor>();
        readonly Dictionary<int, SpriteRenderer> nails = new Dictionary<int, SpriteRenderer>();
        readonly Dictionary<string, AudioClip> sounds = new Dictionary<string, AudioClip>();
        readonly string[] colors = { "#f19975","#e0b7fb","#fcd69e","#f498b4","#7fd2f2","#66cc99","#edffaa","#88ff8f","#fbff72","#f14367","#efe22c","#e655fd","#9e3eea","#f9b742" };
        AudioSource audioSource, musicSource;
        int level = 1, seed, lastCompleted, bestLevel;
        bool menu = true, hammer, settings, sound = true, busy;
        float lastMessageAt;
        string message = "";
        int generation;
        const float W = 750, H = 1334;

        void Awake()
        {
            runtimeReady=true;platform=gameObject.AddComponent<TikTokPlatform>();
            platform.Configure(SdkSettings,()=>level);
            Application.targetFrameRate = 60;
            #if !UNITY_WEBGL || UNITY_EDITOR
            Screen.SetResolution(540, 960, FullScreenMode.Windowed);
#endif
            camera2d = Camera.main;
            if (!camera2d) { var go = new GameObject("Portrait Camera"); camera2d = go.AddComponent<Camera>();go.tag="MainCamera"; }
            camera2d.orthographic = true; camera2d.orthographicSize = H / 200f;
            camera2d.transform.position = new Vector3(0, 0, -10);
            camera2d.clearFlags = CameraClearFlags.SolidColor; camera2d.backgroundColor = new Color(.73f,.36f,.13f);
            CreateBackground();
            shapes = JsonUtility.FromJson<ShapeCatalog>(Resources.Load<TextAsset>("Data/shapes").text).shapes.ToDictionary(s=>s.id);
            audioSource = gameObject.AddComponent<AudioSource>();
            musicSource = gameObject.AddComponent<AudioSource>();musicSource.loop=true;musicSource.volume=.17f;
            bestLevel = PlayerPrefs.GetInt("ScrewKingPort.Level", 1);
            level = Mathf.Clamp(bestLevel,1,89);
            for(int a=8;a<18;a++)for(int b=8;b<18;b++)Physics2D.IgnoreLayerCollision(a,b,a!=b);
            Physics2D.gravity = new Vector2(0,-9.8f);
            PlayMusic("mainbgm");InitializeArtUi();
            if(Environment.GetCommandLineArgs().Contains("--port-smoke") || Environment.GetCommandLineArgs().Contains("--effects-smoke"))StartCoroutine(SmokeRun());
        }
        public void StartLevel(int id, int fixedSeed = -1, MoodBoost.Sdk.Tracking.PlayType playType = MoodBoost.Sdk.Tracking.PlayType.Home)
        {
            platform.Telemetry.Leave();platform.TutorialReset();observedPrompt=SourcePrompt.None;
            MainGameFragments.Clear();generation++; StopAllCoroutines();shakes.Clear();failPlayed=false;sdkChangingLevel=false;transitionUntil=Time.time+.35f;
            foreach(var b in boards)if(b)Destroy(b.gameObject);
            foreach(var n in nails.Values)if(n)Destroy(n.gameObject);
            boards.Clear();nails.Clear();
            level=Mathf.Clamp(id,1,89);seed=fixedSeed<0?UnityEngine.Random.Range(1,int.MaxValue):fixedSeed;
            var data=JsonUtility.FromJson<LevelData>(Resources.Load<TextAsset>("Data/Levels/"+level).text);
            source?.Dispose();source=new CocosSourceRuntime();
            Model=new PortModel(data,seed,source);source.ResetPhysics();
            data=Model.Level;lastCompleted=0;resultPlayed=false;roundReward.Reset();wonAt=double.MaxValue;
            menu=false;hammer=false;settings=false;busy=false;message="";
            for(int layer=0;layer<data.layers.Length;layer++)
                for(int boardIndex=0;boardIndex<data.layers[layer].boards.Length;boardIndex++)
                
                {
                    var dataBoard=data.layers[layer].boards[boardIndex];
                    var board=new GameObject("Layer "+layer+" / Board "+dataBoard.id).AddComponent<BoardActor>();
                    ColorUtility.TryParseHtmlString(colors[(layer*5+2)%colors.Length],out var color);
                    board.Setup(dataBoard,shapes[dataBoard.id],layer,color,Model.Nails.Where(n=>ReferenceEquals(n.Board,dataBoard)),source,boards.Count,boardIndex);
                    boards.Add(board);
                    foreach(var nail in board.Nails)
                    {
                        var go=new GameObject("Screw "+nail.Id+" color "+nail.Color);
                        var sprite=go.AddComponent<SpriteRenderer>();sprite.sprite=ReskinArt.Sprite(ReskinArt.Screw(nail.Color));
                        sprite.sortingOrder=layer*100+20;
                        go.transform.position=board.NailPosition(nail);
                        // Cocos screw head uses the image's native pixel scale.
                        ReskinArt.FitScrew(sprite);
                        nails[nail.Id]=sprite;
                    }
                }
            guideBoard=Model.Level.layers.Last().boards.FirstOrDefault();
            Model.NailPosition=n=>new Vector2(nails[n.Id].transform.position.x*100,nails[n.Id].transform.position.y*100);
            Model.Pick=source.Pick;
            Model.OnLanded=inBox=>Play(inBox?"put":"take");
            Model.OnSound=Play;
            Model.OnLifted=n=>{var owner=boards.FirstOrDefault(b=>b && ReferenceEquals(b.Data,n.Board));if(owner)owner.Release(n);};
            UpdateVisibility();PlayMusic("gameBgm");
            platform.Telemetry.Begin(level,Model.Nails.Count,playType);
            Debug.Log("PORT_LEVEL_READY "+level+" screws="+Model.Nails.Count+" boxes="+(Model.PendingBoxes.Count+Model.Boxes.Count(b=>!b.Locked)));
        }

        void UpdateVisibility()
        {
            var activeLayers=boards.Where(b=>b && !b.Cleared).Select(b=>b.Layer).Distinct().OrderByDescending(x=>x).ToList();
            foreach(var b in boards.Where(b=>b).OrderByDescending(b=>b.Layer))
            {
                if(!b || b.Cleared)continue;
                int rank=activeLayers.IndexOf(b.Layer);
                b.SetVisibility(rank<Model.Level.show,rank<Model.Level.show-Model.Level.gray);
                foreach(var nail in b.Nails)
                    if(!nail.Removed && nails.TryGetValue(nail.Id,out var renderer) && renderer) renderer.enabled=b.Shown && b.Interactive;
            }
        }
        void Update()
        {
            if(!camera2d || !runtimeReady)return;
            float ratio=(float)Screen.width/Screen.height;
            camera2d.orthographicSize=Mathf.Max(H/200f,W/200f/ratio);
            if(Input.GetKeyDown(KeyCode.Escape)) { if(settings)settings=false;else if(menu)Application.Quit();else GoHome(); }
            if(!menu && Model!=null)
            {
                float gameplayDelta=(settings || platform.Showing || platform.Backgrounded || Model.Prompt!=SourcePrompt.None)?0:Time.deltaTime;
                bool telemetryPaused=settings || platform.Showing || platform.Backgrounded || Model.Prompt!=SourcePrompt.None || Time.timeScale==0;
                platform.Telemetry.Tick(telemetryPaused?0:Time.unscaledDeltaTime,Model.Remaining);
                if(Model.Prompt!=observedPrompt){observedPrompt=Model.Prompt;if(observedPrompt!=SourcePrompt.None){platform.Offer(observedPrompt);if(observedPrompt==SourcePrompt.Revive)platform.Telemetry.Lost();}}
                Model.Tick(gameplayDelta);
                if(Model.Won && !resultPlayed){resultPlayed=true;GrantRoundCurrency();OfferDoubleReward();platform.Telemetry.Won();wonAt=Model.Clock;Play("win");bestLevel=Math.Max(bestLevel,Math.Min(89,level+1));PlayerPrefs.SetInt("ScrewKingPort.Level",bestLevel);PlayerPrefs.Save();PlatformFeatures?.PersistProgress();}
                if(Model.Lost && !failPlayed){failPlayed=true;platform.Telemetry.Lost();lostAt=Model.Clock;Play("fail");}
                lastCompleted=Model.CompletedBoxes;
                foreach(var nail in Model.Nails.Where(n=>n.Removed))
                {
                    // Moving screws are drawn once above the boxes from the model motion stage.
                    if(nails[nail.Id])nails[nail.Id].enabled=false;
                }
                foreach(var board in boards.Where(b=>b && b.Shown))
                    foreach(var nail in board.Nails.Where(n=>!n.Removed))
                    {
                        var renderer=nails[nail.Id];var c=renderer.color;c.a=board.ScrewAlpha;renderer.color=c;
                        var pos=board.NailPosition(nail);
                        if(shakes.TryGetValue(nail.Id,out float began)){
                            float elapsed=Time.time-began;
                            if(elapsed<.32f)pos.x+=.05f*Mathf.Sin(elapsed/.08f*Mathf.PI*2);else shakes.Remove(nail.Id);
                        }
                        renderer.transform.position=pos;
                    }
                var poses=source.Step(gameplayDelta);
                var sourceKeys=new HashSet<int>(poses.boards.Select(p=>p.key));
                foreach(var actor in boards)if(actor && actor.HasSource && !sourceKeys.Contains(actor.Key))actor.RemoveSource();
                foreach(var pose in poses.boards){var actor=boards.FirstOrDefault(b=>b&&b.Key==pose.key);if(actor){actor.transform.position=GameplayLayout.PhysicsToView(new Vector3((float)pose.x/100,(float)pose.y/100));actor.transform.rotation=Quaternion.Euler(0,0,(float)pose.angle);}}
            }
            hammer=Model!=null && Model.HammerArmed;
            if(menu || Model==null || settings || busy || platform.Showing || !Model.Ready)return;
            if(Input.GetMouseButtonDown(0))
            {
                if(UiRoot && UiRoot.BlocksPointer(Input.mousePosition))return;
                var world=(Vector2)camera2d.ScreenToWorldPoint(Input.mousePosition);
                if(hammer)
                {
                    var board=boards.Where(b=>b && !b.Cleared && b.Covers(world)).OrderByDescending(b=>b.Layer).FirstOrDefault();
                    if(board){hammer=false;StartCoroutine(HammerBoard(board));}
                    return;
                }
                var nearest=Model.Nails.Where(n=>!n.Removed && nails[n.Id] && nails[n.Id].enabled && boards.Any(b=>b&&ReferenceEquals(b.Data,n.Board)&&b.Clickable))
                    .Where(n=>Mathf.Abs(nails[n.Id].transform.position.x-world.x)<=.375f&&Mathf.Abs(nails[n.Id].transform.position.y-world.y)<=.375f)
                    .OrderByDescending(n=>n.Layer).ThenByDescending(n=>n.Id).FirstOrDefault();
                if(nearest!=null)TryTake(nearest);
            }
        }
        bool IsBlocked(NailState nail)
        {
            var point=(Vector2)nails[nail.Id].transform.position;
            return boards.Any(b=>b && b.Layer>nail.Layer && b.Covers(point,.25f));
        }
        public bool TryTake(NailState nail)
        {
            if(busy || !Model.Ready || nail.Removed)return false;
            if(IsBlocked(nail)) { Say("This screw is covered by a board above.");Play("takeFail");shakes[nail.Id]=Time.time;return false; }
            Vector3 start=nails[nail.Id].transform.position;
            int color=nail.Color;
            if(!Model.Take(nail))return false;
            var board=boards.First(b=>b && ReferenceEquals(b.Data,nail.Board));board.BeginUnscrew();
            nails[nail.Id].enabled=false;
            Play("take");if(vibrationOn)platform.Vibrate(false);
            if(ReferenceEquals(nail.Board,guideBoard)){bool complete=board.Nails.All(n=>n.Removed);platform.TutorialStep(complete);if(complete&&level==1)PlatformFeatures?.TutorialCompleted();}
            StartCoroutine(RefreshAfterRelease());
            return true;
        }
        IEnumerator RefreshAfterRelease()
        {
            yield return new WaitForSeconds(.5f);UpdateVisibility();
        }
        IEnumerator HammerBoard(BoardActor board)
        {
            busy=true;Play("hammer");
            // Source hammer removes the board and caches its screws rather than losing them.
            Model.Hammer(board.Nails);
            foreach(var nail in board.Nails)nails[nail.Id].enabled=false;
            MainGameFragments.Burst(board);if(vibrationOn)platform.Vibrate(true);board.RemoveSource();UpdateVisibility();
            yield return null;busy=false;
        }
        void GoHome()
        {
            platform.Telemetry.Leave();platform.TutorialReset();
            MainGameFragments.Clear();generation++;StopAllCoroutines();foreach(var b in boards)if(b)b.gameObject.SetActive(false);foreach(var n in nails.Values)if(n)n.gameObject.SetActive(false);
            menu=true;settings=false;PlayMusic("mainbgm");
        }
        void Say(string value){message=value;lastMessageAt=Time.time;}
        void Play(string name)
        {
            if(!sound)return;
            if(!sounds.ContainsKey(name))sounds[name]=Resources.Load<AudioClip>("Art/Audio/"+name);
            if(sounds[name])audioSource.PlayOneShot(sounds[name],.4f);
        }
        void PlayMusic(string name){musicSource.clip=Resources.Load<AudioClip>("Art/Audio/"+name);musicSource.mute=!musicOn;musicSource.Play();}
        void CreateBackground()
        {
            var background = new GameObject("Reskin / Wood background").AddComponent<SpriteRenderer>();
            background.transform.SetParent(transform, false);
            background.sprite = ReskinArt.Sprite(ReskinArt.UI + "bg");
            background.sortingOrder = -1000;
            if (background.sprite)
                background.transform.localScale = new Vector3(W / 100 / background.sprite.bounds.size.x, H / 100 / background.sprite.bounds.size.y, 1);
        }
        void LateUpdate()
        {
            if(!runtimeReady || !UiRoot || !UiRoot.Gameplay)return;
            if(menu){platform.ScreenRevealed();return;}
            if(Model!=null)UiRoot.Gameplay.Refresh(this);
        }
        public void WatchRewarded()
        {
            if(Model==null || Model.Prompt==SourcePrompt.None || Model.DoScrewCount!=0 || platform.Showing)return;
            var current=Model;int version=generation;var action=Model.Prompt;
            platform.ShowRewarded(action,success=>{
                if(generation!=version || !ReferenceEquals(Model,current) || menu || Model.Prompt!=action)return;
                Model.RewardResult(success);
                if(success){platform.Telemetry.BoosterUsed(action);if(action==SourcePrompt.Revive)platform.Telemetry.Resumed();PlayRewardSound(action);}
            });
        }
        void PlayRewardSound(SourcePrompt action){if(action==SourcePrompt.Slot)Play("add");else if(action==SourcePrompt.Clear || action==SourcePrompt.ClearRescue)Play("clear");else if(action!=SourcePrompt.Hammer)Play("exchange");}
        void OnDestroy(){platform?.Telemetry?.Leave();source?.Dispose();}

        IEnumerator SmokeRun()
        {
            // Begin after StartLevel because it cancels previous gameplay coroutines.
            yield return null;
            var menuOutput=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--smoke-output="));
            if(menuOutput!=null)
            {
                string directory=menuOutput.Substring("--smoke-output=".Length);Directory.CreateDirectory(directory);
                ScreenCapture.CaptureScreenshot(Path.Combine(directory,"unity-menu.png"));
                yield return new WaitForSeconds(.3f);
            }
            var levelArg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--smoke-level="));
            int smokeLevel=levelArg!=null && int.TryParse(levelArg.Substring(14),out int requested)?requested:1;
            StartLevel(smokeLevel,12345);
            StartCoroutine(Environment.GetCommandLineArgs().Contains("--effects-smoke")?EffectsSmokeSteps():SmokeSteps());
        }
        IEnumerator EffectsSmokeSteps()
        {
            var arg=Environment.GetCommandLineArgs().First(a=>a.StartsWith("--smoke-output="));
            string output=arg.Substring("--smoke-output=".Length);Directory.CreateDirectory(output);
            yield return new WaitForSeconds(2);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"initial.png"));
            int taken=0;bool conserved=true;
            for(int step=0;step<15 && !Model.Won;step++)
            {
                var nail=Model.Nails.Where(n=>!n.Removed && nails[n.Id].enabled && !IsBlocked(n)).OrderByDescending(n=>n.Layer).FirstOrDefault();
                if(nail==null){yield return new WaitForSeconds(.9f);continue;}
                if(TryTake(nail))taken++;
                for(int frame=0;frame<12;frame++){
                    yield return new WaitForSeconds(.075f);
                    ScreenCapture.CaptureScreenshot(Path.Combine(output,"take-"+step+"-"+frame+".png"));
                    conserved &= Model.AccountedScrews==Model.Nails.Count;
                }
            }
            yield return new WaitForSeconds(1);
            bool won=Model.Won;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"result.png"));
            yield return new WaitForSeconds(.15f);
            // StartLevel cancels coroutines, so continue the booster check in a new coroutine.
            StartLevel(2,12345);StartCoroutine(EffectsBoosterSmoke(output,taken,conserved,won));
        }
        IEnumerator EffectsBoosterSmoke(string output,int taken,bool conserved,bool won)
        {
            yield return new WaitForSeconds(2);
            var board=boards.Where(b=>b && b.Interactive && !b.Cleared).OrderByDescending(b=>b.Layer).First();
            yield return StartCoroutine(HammerBoard(board));
            yield return new WaitForSeconds(.12f);
            bool debris=FindObjectsByType<MainGameFragments>(FindObjectsSortMode.None).Length>0;
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"hammer.png"));
            conserved &= Model.AccountedScrews==Model.Nails.Count;
            yield return new WaitForSeconds(.9f);
            bool cleaned=FindObjectsByType<MainGameFragments>(FindObjectsSortMode.None).Length==0;
            int capacity=Model.HoleCapacity;Model.AddHole();Play("add");
            bool slot=Model.HoleCapacity==capacity+1;
            Model.ClearHoles();Play("clear");Model.ChangeBox();Play("exchange");
            conserved &= Model.AccountedScrews==Model.Nails.Count;
            string report="{\"taken\":"+taken+",\"won\":"+won.ToString().ToLowerInvariant()+",\"conserved\":"+conserved.ToString().ToLowerInvariant()+",\"debris\":"+debris.ToString().ToLowerInvariant()+",\"cleanup\":"+cleaned.ToString().ToLowerInvariant()+",\"slot\":"+slot.ToString().ToLowerInvariant()+"}";
            File.WriteAllText(Path.Combine(output,"effects-smoke.json"),report);Debug.Log("MAIN_EFFECTS_SMOKE "+report);
            yield return new WaitForSeconds(.5f);Application.Quit();
        }
        IEnumerator SmokeSteps()
        {
            var output=Path.Combine(Application.dataPath,"../../../../Builds/Smoke");
            var arg=Environment.GetCommandLineArgs().FirstOrDefault(a=>a.StartsWith("--smoke-output="));
            if(arg!=null)output=arg.Substring("--smoke-output=".Length);
            Directory.CreateDirectory(output);
            yield return new WaitForSeconds(2.2f);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"unity-initial.png"));
            yield return null;
            bool preview=Environment.GetCommandLineArgs().Contains("--flight-preview");
            string frames=Path.Combine(output,"frames");
            int frame=0;
            if(preview){Directory.CreateDirectory(frames);Time.captureFramerate=30;}
            int taken=0;
            for(int step=0;step<6;step++)
            {
                var nail=Model.Nails.Where(n=>!n.Removed && nails[n.Id].enabled && !IsBlocked(n)).OrderByDescending(n=>n.Layer).FirstOrDefault();
                if(nail==null)break;
                if(TryTake(nail))taken++;
                if(preview)
                {
                    for(int i=0;i<24;i++)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(frames,"frame-"+(frame++).ToString("D4")+".png"));
                        yield return null;
                    }
                }
                else yield return new WaitForSeconds(.7f);
            }
            if(preview)Time.captureFramerate=0;
            Model.Settle();
            bool invariant=Model.AccountedScrews==Model.Nails.Count;
            var report="{\"level\":"+level+",\"taken\":"+taken+",\"total\":"+Model.Nails.Count+",\"accounted\":"+Model.AccountedScrews+",\"invariant\":"+invariant.ToString().ToLowerInvariant()+"}";
            File.WriteAllText(Path.Combine(output,"runtime-smoke.json"),report);
            ScreenCapture.CaptureScreenshot(Path.Combine(output,"unity-after-moves.png"));
            Debug.Log("PORT_SMOKE "+report);
            yield return new WaitForSeconds(.5f);
            if(Environment.GetCommandLineArgs().Contains("--smoke-quit"))Application.Quit();
        }
    }
}
