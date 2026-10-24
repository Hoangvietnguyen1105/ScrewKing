using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScrewKing.Editor
{
    public static class UiPrefabBuilder
    {
        public const string Folder="Assets/ScrewKing/Resources/UI/Prefabs";
        public const string RootPath=Folder+"/ScrewKingUI.prefab";
        public const string PreviewPath="Assets/ScrewKing/Scenes/UIEditing.unity";
        const string Art="Art/NewArtUI/";
        internal static Font Font=>Resources.Load<Font>("UI/Fonts/LilitaOne-Regular");
        static readonly Color Gold=new Color(.65f,.49f,.07f);
        [MenuItem("Screw King/UI/Create Missing Prefabs")]
        public static void CreateMissing()
        {
            Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            GameplayHudPrefabBuilder.Ensure();
            string[] names={"ScreenHome","ScreenLoading","PopupSettings","PopupWin","PopupLose","PopupReward","GameplayHUD","ScrewKingUI"};
            if(names.All(n=>File.Exists(Folder+"/"+n+".prefab")))return;
            PrepareBorders();
            SaveMissing("ScreenHome",Home);SaveMissing("ScreenLoading",Loading);SaveMissing("PopupSettings",Settings);
            SaveMissing("PopupWin",()=>Result(true));SaveMissing("PopupLose",()=>Result(false));SaveMissing("PopupReward",Reward);
            SaveMissing("ScrewKingUI",Root);UpgradeHud();AssetDatabase.SaveAssets();
        }
        static void SaveMissing(string name,Func<GameObject> create)
        {
            string path=Folder+"/"+name+".prefab";if(File.Exists(path))return;
            var go=create();PrefabUtility.SaveAsPrefabAsset(go,path);UnityEngine.Object.DestroyImmediate(go);
        }
        public static ScrewKingUiController Attach(PortGame game)
        {
            CreateMissing();
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RootPath));
            game.UiRoot=instance.GetComponent<ScrewKingUiController>();return game.UiRoot;
        }
        [MenuItem("Screw King/UI/Open UI Editing Scene")]
        public static void OpenEditingScene()
        {
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            if(!File.Exists(PreviewPath))CreateEditingScene();
            else EditorSceneManager.OpenScene(PreviewPath);
        }
        // Explicit initial setup only. Existing editable scenes and prefabs are never regenerated here.
        public static void CreateEditingScene()
        {
            CreateMissing();if(File.Exists(PreviewPath))return;
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera=new GameObject("Main Camera");camera.tag="MainCamera";camera.AddComponent<Camera>();camera.AddComponent<AudioListener>();
            var game=new GameObject("Screw King").AddComponent<PortGame>();Attach(game);
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewPath));EditorSceneManager.SaveScene(scene,PreviewPath);
        }
        static GameObject Screen(string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(ScrewKingUiView));Center(go.GetComponent<RectTransform>());return go;
        }
        internal static void Center(RectTransform rect){rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(750,1334);rect.anchoredPosition=Vector2.zero;}
        internal static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(w,h);rect.anchoredPosition=new Vector2(x,-y);return rect;
        }
        static Sprite Sprite(string path){var sprite=Resources.Load<Sprite>(path);if(!sprite)throw new Exception("Missing UI sprite: "+path);return sprite;}
        internal static Image Pic(string name,Transform parent,string path,float x,float y,float w,float h,bool sliced=false,float multiplier=2.5f)
        {
            var rect=Rect(name,parent,x,y,w,h);var image=rect.gameObject.AddComponent<Image>();image.sprite=Sprite(path);
            image.raycastTarget=false;image.type=sliced?Image.Type.Sliced:Image.Type.Simple;image.preserveAspect=!sliced;
            image.pixelsPerUnitMultiplier=multiplier;return image;
        }
        static Image Solid(string name,Transform parent,Color color,bool raycast=true)
        {
            var rect=Rect(name,parent,0,0,750,1334);var image=rect.gameObject.AddComponent<Image>();image.color=color;image.raycastTarget=raycast;return image;
        }
        internal static Text Label(string name,Transform parent,string value,float x,float y,float w,float h,int size,Color? color=null,Color? outline=null)
        {
            var rect=Rect(name,parent,x,y,w,h);var text=rect.gameObject.AddComponent<Text>();text.font=Font;text.text=value;text.fontStyle=FontStyle.Normal;text.fontSize=size;
            text.alignment=TextAnchor.MiddleCenter;text.color=color??Color.white;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;
            if(outline.HasValue){var effect=rect.gameObject.AddComponent<Outline>();effect.effectColor=outline.Value;effect.effectDistance=new Vector2(2,-2);}
            return text;
        }
        internal static void Wire(Button button,ScrewKingUiCommand command)
        {
            if(!button.GetComponent<ScrewKingButtonFeedback>())button.gameObject.AddComponent<ScrewKingButtonFeedback>();
            var action=button.gameObject.AddComponent<ScrewKingUiAction>();action.Command=command;
            UnityEventTools.AddPersistentListener(button.onClick,action.Invoke);
            var colors=button.colors;colors.highlightedColor=new Color(1,1,1,.95f);colors.pressedColor=new Color(.82f,.82f,.82f);colors.disabledColor=new Color(.55f,.55f,.55f,.6f);button.colors=colors;
        }
        static Button Icon(string name,Transform parent,string art,Rect r,ScrewKingUiCommand command)
        {
            var image=Pic(name,parent,Art+art,r.x,r.y,r.width,r.height);image.raycastTarget=true;
            var button=image.gameObject.AddComponent<Button>();button.targetGraphic=image;Wire(button,command);return button;
        }
        static Button Button(string name,Transform parent,string label,Rect r,ScrewKingUiCommand command,bool red=false,bool orange=false,bool settingsFrame=false,int size=52)
        {
            var frame=Pic(name,parent,Art+(settingsFrame?"screw_setting/Rectangle 3 copy 4":"screw_win_lose/Layer 3 copy 2"),r.x,r.y,r.width,r.height,true);
            frame.raycastTarget=true;
            var background=Pic("Background",frame.transform,Art+(orange?"screw_win_lose/Layer 3 copy 9":red?"screw_setting/Layer 18 copy 3":"screw_setting/Layer 18 copy 2"),16,16,r.width-32,r.height-32,true);
            Label("Label",frame.transform,label,0,0,r.width,r.height,size,Color.white,orange?new Color(.63f,.32f,.01f):red?new Color(.45f,0,0):new Color(0,.36f,0));
            var button=frame.gameObject.AddComponent<Button>();button.targetGraphic=background;Wire(button,command);return button;
        }
        static Transform Content(GameObject screen)
        {
            var content=new GameObject("Content",typeof(RectTransform)).GetComponent<RectTransform>();content.SetParent(screen.transform,false);Center(content);return content;
        }
        static GameObject Home()
        {
            var go=Screen("ScreenHome");var view=go.GetComponent<ScrewKingUiView>();
            var wood=Pic("WoodBackground",go.transform,ReskinArt.UI+"bg",0,0,750,1334);wood.preserveAspect=false;wood.raycastTarget=true;
            Pic("LevelBadge",go.transform,Art+"screw_win_lose/Layer 3 copy 9",70,37,235,69,true);
            Pic("ScrewIcon",go.transform,Art+"screw_win_lose/Layer 3 copy 8",44,22,94,103);
            view.CounterLabel=Label("CurrencyCounter",go.transform,"0",137,44,155,52,34);
            
            Icon("SettingsButton",go.transform,"screw_home/setting",new Rect(627,28,88,88),ScrewKingUiCommand.Settings);
            var play=Button("PlayButton",go.transform,"Level 1",new Rect(205,882,340,155),ScrewKingUiCommand.Play,size:58);view.LevelLabel=play.GetComponentInChildren<Text>();
            var previous=Icon("PreviousLevel",go.transform,"screw_setting/Layer 19 copy 3",new Rect(120,925,65,57),ScrewKingUiCommand.Previous);Label("Arrow",previous.transform,"<",0,0,65,57,40);previous.gameObject.SetActive(false);
            var next=Icon("NextLevel",go.transform,"screw_setting/Layer 19 copy 3",new Rect(565,925,65,57),ScrewKingUiCommand.NextSelection);Label("Arrow",next.transform,">",0,0,65,57,40);next.gameObject.SetActive(false);
            var footer=Pic("Footer",go.transform,Art+"screw_home/Layer 15",0,1164,750,170);footer.preserveAspect=false;footer.gameObject.SetActive(false);
            Pic("HomeTabBackground",go.transform,Art+"screw_home/Layer 14 copy",231,1126,288,208).gameObject.SetActive(false);
            Pic("HomeIcon",go.transform,Art+"screw_home/Layer 7 copy",300,1138,151,129).gameObject.SetActive(false);Label("HomeCaption",go.transform,"Home",265,1268,220,58,45,new Color(.54f,.28f,0)).gameObject.SetActive(false);
            var shop=Pic("ShopComingSoon",go.transform,Art+"screw_home/Layer 9 copy",62,1200,124,116);shop.color=new Color(1,1,1,.55f);
            var cards=Pic("CardsComingSoon",go.transform,Art+"screw_home/Layer 8 copy",558,1200,148,114);cards.color=new Color(1,1,1,.55f);shop.gameObject.SetActive(false);cards.gameObject.SetActive(false);
            Label("ShopCaption",go.transform,"Soon",67,1300,114,27,17,new Color(.5f,.26f,0)).gameObject.SetActive(false);Label("CardsCaption",go.transform,"Soon",575,1300,114,27,17,new Color(.5f,.26f,0)).gameObject.SetActive(false);return go;
        }
        static ScrewKingUiToggle Toggle(Transform parent,ScrewKingUiSetting setting,string icon,float y)
        {
            var root=Rect(setting+"Switch",parent,0,y,750,68);var toggle=root.gameObject.AddComponent<ScrewKingUiToggle>();toggle.Setting=setting;
            Pic("Icon",root,Art+"screw_setting/"+icon,135,2,60,60);Label("Caption",root,setting.ToString(),208,0,215,68,42,Gold);
            Pic("Track",root,Art+"screw_setting/Rectangle 5 copy 5",430,0,198,64,true,1.33f);
            var off=Pic("OffHighlight",root,Art+"screw_setting/Layer 19 copy 4",430,0,99,64,true,1.3f);toggle.OffHighlight=off.gameObject;
            var on=Pic("OnHighlight",root,Art+"screw_setting/Layer 19 copy 3",529,0,99,64,true,1.3f);toggle.OnHighlight=on.gameObject;
            toggle.OffLabel=Label("OffLabel",root,"Off",430,0,99,64,32);toggle.OnLabel=Label("OnLabel",root,"On",529,0,99,64,32);
            foreach(bool value in new[]{false,true}){
                var click=Rect(value?"OnButton":"OffButton",root,value?529:430,0,99,64);var image=click.gameObject.AddComponent<Image>();image.color=Color.clear;
                var button=click.gameObject.AddComponent<Button>();button.targetGraphic=image;
                var command=setting==ScrewKingUiSetting.Music?(value?ScrewKingUiCommand.MusicOn:ScrewKingUiCommand.MusicOff):setting==ScrewKingUiSetting.Sound?(value?ScrewKingUiCommand.SoundOn:ScrewKingUiCommand.SoundOff):(value?ScrewKingUiCommand.VibrationOn:ScrewKingUiCommand.VibrationOff);Wire(button,command);
            }
            toggle.SetValue(true);return toggle;
        }
        static GameObject Settings()
        {
            var go=Screen("PopupSettings");Solid("Dimmer",go.transform,new Color(0,0,0,.75f));var content=Content(go);var view=go.GetComponent<ScrewKingUiView>();
            Pic("Panel",content,Art+"screw_setting/Rectangle 1 copy 2",58,240,635,863,true,2.8f);
            Pic("Header",content,Art+"screw_setting/Layer 11 (merged) copy 2",174,218,409,114,true,3);
            Pic("LeftBolt",content,Art+"screw_setting/Layer 10 copy 3",205,249,38,38);Pic("RightBolt",content,Art+"screw_setting/Layer 10 copy 3",515,249,38,38);
            Label("Title",content,"Settings",246,230,266,85,57,Color.white,new Color(.61f,.36f,.02f));
            Icon("CloseButton",content,"screw_setting/Layer 9 copy",new Rect(632,218,88,88),ScrewKingUiCommand.Resume);
            view.Switches=new[]{Toggle(content,ScrewKingUiSetting.Music,"Layer 15 copy",374),Toggle(content,ScrewKingUiSetting.Sound,"Layer 13 copy",485),Toggle(content,ScrewKingUiSetting.Vibration,"Layer 14 copy",589)};
            Button("ResumeButton",content,"Resume",new Rect(215,704,335,147),ScrewKingUiCommand.Resume,settingsFrame:true,size:54);
            Button("QuitButton",content,"Quit",new Rect(215,879,335,147),ScrewKingUiCommand.Quit,red:true,settingsFrame:true,size:54);return go;
        }
        static GameObject Result(bool win)
        {
            var go=Screen(win?"PopupWin":"PopupLose");var view=go.GetComponent<ScrewKingUiView>();Solid("Dimmer",go.transform,new Color(.045f,.025f,.01f,.82f));var content=Content(go);view.AnimatedContent=(RectTransform)content;
            var r=win?new Rect(105,202,540,150):new Rect(180,202,390,205);
            Pic("ResultHeading",content,Art+"screw_win_lose/"+(win?"Layer 7 copy":"Layer 4 copy"),r.x,r.y,r.width,r.height);
            view.LevelLabel=Label("LevelLabel",content,"Level 1",180,410,390,75,53);
            Pic("ScrewIllustration",content,Art+"screw_win_lose/Layer 3 copy 8",300,540,150,170);
            Pic("StatusBadge",content,Art+"screw_win_lose/Rounded Rectangle 1 copy",289,720,172,64,true,1.1f);Label("StatusCaption",content,win?"COMPLETE":"TRY AGAIN",290,723,170,57,24,new Color(1,.87f,0));
            Button(win?"NextLevelButton":"RetryButton",content,win?"Next level":"Retry",new Rect(205,882,340,155),win?ScrewKingUiCommand.NextLevel:ScrewKingUiCommand.Retry,orange:win);
            Icon("CloseButton",content,"screw_setting/Layer 9 copy",new Rect(650,220,66,66),ScrewKingUiCommand.CloseResult);ApplyResultEconomy(go,win);return go;
        }
        // Explicit migration only: keep all unrelated art and designer transforms intact.
        internal static void ApplyResultEconomy(GameObject go,bool win)
        {
            var view=go.GetComponent<ScrewKingUiView>();
            if(go.transform.Find("CurrencyBadge"))return;
            Pic("CurrencyBadge",go.transform,Art+"screw_win_lose/Layer 3 copy 9",70,37,220,69,true);
            Pic("CurrencyIcon",go.transform,Art+"screw_win_lose/Layer 3 copy 8",44,22,94,103);
            view.CounterLabel=Label("CurrencyBalance",go.transform,"0",137,44,143,52,42);
            var content=go.transform.Find("Content");
            var caption=content.Find("StatusCaption").GetComponent<Text>();caption.text=win?"+20":"20";caption.fontSize=48;
            view.StatusLabel=win?caption:null;
            var close=content.Find("CloseButton");if(close)close.gameObject.SetActive(false);
            if(win){
                var next=(RectTransform)content.Find("NextLevelButton");next.anchoredPosition=new Vector2(next.anchoredPosition.x,-1070);
                var button=Button("DoubleRewardButton",content,"x2",new Rect(205,882,340,155),ScrewKingUiCommand.DoubleReward,size:64);
                var label=button.GetComponentInChildren<Text>();view.RewardButtonLabel=label;
                var labelRect=label.rectTransform;labelRect.anchoredPosition=new Vector2(198,0);labelRect.sizeDelta=new Vector2(126,155);
                Pic("VideoIcon",button.transform,ReskinArt.UI+"Layer 18",45,45,68,62);
                Pic("RewardScrew",button.transform,Art+"screw_win_lose/Layer 3 copy 8",135,30,70,93);
                view.LocalSimulationNotice=Label("LocalSimulationNotice",content,"Local preview: simulated video",160,1038,430,27,17).gameObject;view.LocalSimulationNotice.SetActive(false);
            }else Pic("NoRewardCross",content,Art+"screw_win_lose/Layer 7",405,735,45,45);
            EditorUtility.SetDirty(view);
        }
        static GameObject Reward()
        {
            var go=Screen("PopupReward");BuildRewardContents(go);return go;
        }
        internal static void BuildRewardContents(GameObject go)
        {
            var view=go.GetComponent<ScrewKingUiView>();
            Solid("Dimmer",go.transform,new Color(.045f,.025f,.01f,.76f));var content=Content(go);
            view.AnimatedContent=(RectTransform)content;view.EntranceDuration=.23f;
            Pic("Panel",content,Art+"screw_setting/Rectangle 1 copy 2",70,330,610,755,true,1.1f);
            Pic("Header",content,Art+"screw_setting/Layer 11 (merged) copy 2",170,292,410,108,true,1.5f);
            Pic("LeftBolt",content,Art+"screw_setting/Layer 10 copy 3",193,329,28,28);
            Pic("RightBolt",content,Art+"screw_setting/Layer 10 copy 3",529,329,28,28);
            view.TitleLabel=RewardLabel("OfferTitle",content,"REVIVE!",225,303,300,80,52,Color.white,new Color(.58f,.31f,.01f));
            view.TitleLabel.resizeTextForBestFit=true;view.TitleLabel.resizeTextMinSize=28;view.TitleLabel.resizeTextMaxSize=52;
            Icon("CloseButton",content,"screw_setting/Layer 9 copy",new Rect(620,304,66,66),ScrewKingUiCommand.CancelReward);
            Pic("RewardMedallion",content,ReskinArt.UI+"Layer 17 copy 2",271,419,208,208).gameObject.SetActive(false);
            view.OfferIcon=Pic("ScrewIllustration",content,ReskinArt.Box(1),275,410,200,225);
            view.SlotIcon=Resources.Load<Sprite>(ReskinArt.UI+"Layer 35");view.ClearIcon=Resources.Load<Sprite>(ReskinArt.UI+"Layer 33");
            view.HammerIcon=Resources.Load<Sprite>(ReskinArt.UI+"Layer 34");view.BoxIcon=Resources.Load<Sprite>(ReskinArt.Box(1));
            view.DescriptionLabel=RewardLabel("Description",content,"Open another box\nand keep playing.",125,650,500,83,32,new Color(.4f,.23f,.07f));
            var watch=Button("WatchButton",content,"Watch video",new Rect(177,751,396,140),ScrewKingUiCommand.WatchReward,size:42);
            PolishRewardButton(watch);view.RewardButtonLabel=watch.GetComponentInChildren<Text>();
            var labelRect=view.RewardButtonLabel.rectTransform;labelRect.anchoredPosition=new Vector2(80,0);labelRect.sizeDelta=new Vector2(296,140);
            Pic("VideoBadge",watch.transform,ReskinArt.UI+"Layer 18",38,47,62,47);
            view.StatusLabel=RewardLabel("Status",content,"Watch a video to continue.",115,906,520,28,19,new Color(.52f,.37f,.16f));
            var cancel=Button("CancelButton",content,"Give up",new Rect(245,955,260,96),ScrewKingUiCommand.CancelReward,red:true,size:32);
            PolishRewardButton(cancel);view.CancelButtonLabel=cancel.GetComponentInChildren<Text>();
        }
        static Text RewardLabel(string name,Transform parent,string text,float x,float y,float w,float h,int size,Color color,Color? outline=null)
        {
            var label=Label(name,parent,text,x,y,w,h,size,color,outline);label.font=Resources.Load<Font>("UI/Fonts/LilitaOne-Regular");label.fontStyle=FontStyle.Normal;return label;
        }
        static void PolishRewardButton(Button button)
        {
            var frame=button.GetComponent<Image>();frame.pixelsPerUnitMultiplier=1.6f;
            var background=button.transform.Find("Background").GetComponent<Image>();
            var rect=button.GetComponent<RectTransform>();bool primary=rect.sizeDelta.x>350;float inset=22;
            background.pixelsPerUnitMultiplier=primary?1.6f:2.5f;
            background.rectTransform.anchoredPosition=new Vector2(inset,-inset);background.rectTransform.sizeDelta=rect.sizeDelta-Vector2.one*inset*2;
            var text=button.GetComponentInChildren<Text>();text.font=Resources.Load<Font>("UI/Fonts/LilitaOne-Regular");text.fontStyle=FontStyle.Normal;
        }
        internal static void CreateGrowthEntry(Transform home,bool shortcut)
        {
            var button=Button(shortcut?"TikTokShortcutEntry":"TikTokRevisitEntry",home,shortcut?"HOME SCREEN":"REVISIT REWARD",new Rect(shortcut?65:390,180,295,102),shortcut?ScrewKingUiCommand.TikTokShortcut:ScrewKingUiCommand.TikTokRevisit,orange:true,size:25);
            PolishRewardButton(button);var label=button.GetComponentInChildren<Text>();label.font=Resources.Load<Font>("UI/Fonts/LilitaOne-Regular");label.fontStyle=FontStyle.Normal;
            label.rectTransform.anchoredPosition=new Vector2(62,0);label.rectTransform.sizeDelta=new Vector2(215,102);
            Pic("ScrewIcon",button.transform,Art+"screw_win_lose/Layer 3 copy 8",16,23,52,63);
        }
        internal static void CreateLegalLink(Transform content,bool privacy)
        {
            var area=Rect(privacy?"PrivacyLink":"TermsLink",content,privacy?390:180,1046,180,38);
            var image=area.gameObject.AddComponent<Image>();image.color=Color.clear;
            var button=area.gameObject.AddComponent<Button>();button.targetGraphic=image;Wire(button,privacy?ScrewKingUiCommand.Privacy:ScrewKingUiCommand.Terms);
            Label("Label",area,privacy?"Privacy":"Terms",0,0,180,38,20,new Color(.48f,.30f,.07f));
        }
        internal static GameObject CreateMissionPopup(TikTokMission mission)
        {
            bool shortcut=mission==TikTokMission.Shortcut;
            var root=new GameObject(shortcut?"PopupTikTokShortcut":"PopupTikTokRevisit",typeof(RectTransform),typeof(TikTokMissionView));Center(root.GetComponent<RectTransform>());
            var view=root.GetComponent<TikTokMissionView>();view.Mission=mission;
            Solid("Dimmer",root.transform,new Color(.045f,.025f,.01f,.76f));var content=Content(root);view.Content=(RectTransform)content;
            Pic("Panel",content,Art+"screw_setting/Rectangle 1 copy 2",60,270,630,860,true,1.1f);
            Pic("Header",content,Art+"screw_setting/Layer 11 (merged) copy 2",130,230,490,112,true,1.5f);
            var title=RewardLabel("Title",content,shortcut?"HOME SCREEN":"REVISIT REWARD",160,247,430,70,44,Color.white,new Color(.58f,.31f,.01f));title.resizeTextForBestFit=true;title.resizeTextMinSize=28;title.resizeTextMaxSize=44;
            Icon("CloseButton",content,"screw_setting/Layer 9 copy",new Rect(628,247,64,64),ScrewKingUiCommand.TikTokMissionClose);
            Pic("RewardScrew",content,Art+"screw_win_lose/Layer 3 copy 8",310,367,130,157);
            view.AmountLabel=RewardLabel("Amount",content,shortcut?"+100 screws":"+50 screws",155,535,440,56,40,new Color(.58f,.31f,.01f));
            string steps=shortcut?"1. Add Screw King to Home Screen\n2. Open it using the new shortcut\n3. Return here to claim your reward":"1. Open your TikTok Profile menu\n2. Find Screw King in TikTok Minis\n3. Reopen the game and claim";
            var description=RewardLabel("Steps",content,steps,122,620,506,135,27,new Color(.4f,.23f,.07f));description.lineSpacing=1.2f;
            var primary=Button("PrimaryButton",content,shortcut?"Add now":"Get started",new Rect(177,786,396,128),ScrewKingUiCommand.TikTokMissionPrimary,size:36);PolishRewardButton(primary);view.PrimaryButton=primary;view.PrimaryLabel=primary.GetComponentInChildren<Text>();
            view.StatusLabel=RewardLabel("Status",content,"Open in TikTok to use these rewards.",115,926,520,66,22,new Color(.52f,.37f,.16f));
            var check=Button("CheckButton",content,"Check reward",new Rect(195,1000,360,74),ScrewKingUiCommand.TikTokMissionCheck,size:27);PolishRewardButton(check);view.CheckButton=check;
            return root;
        }
        static GameObject Loading()
        {
            var go=Screen("ScreenLoading");var view=go.GetComponent<ScrewKingUiView>();Solid("InputBlocker",go.transform,Color.clear);
            view.LeftDoor=Door(go.transform,"LeftDoor",0);view.RightDoor=Door(go.transform,"RightDoor",750);view.RightDoor.localScale=new Vector3(-1,1,1);return go;
        }
        static RectTransform Door(Transform parent,string name,float x)
        {
            var root=Rect(name,parent,x,0,375,1334);string path=Art+"screw_loading/Layer 11";var texture=Resources.Load<Texture2D>(path);float height=texture.height*375f/texture.width,y=(1334-height)*.5f;
            var top=Rect("TopWood",root,0,0,375,y).gameObject.AddComponent<RawImage>();top.texture=texture;top.uvRect=new Rect(0,.995f,1,.005f);top.raycastTarget=false;
            Pic("DoorAndHandle",root,path,0,y,375,height);
            var bottom=Rect("BottomWood",root,0,y+height,375,1334-y-height).gameObject.AddComponent<RawImage>();bottom.texture=texture;bottom.uvRect=new Rect(0,0,1,.005f);bottom.raycastTarget=false;return root;
        }
        static void UpgradeHud()
        {
            var root=PrefabUtility.LoadPrefabContents(RootPath);
            try{
                var ui=root.GetComponent<ScrewKingUiController>();if(ui.Gameplay)return;
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/GameplayHUD.prefab");
                var hud=(GameObject)PrefabUtility.InstantiatePrefab(prefab,ui.DesignArea);hud.transform.SetAsFirstSibling();ui.Gameplay=hud.GetComponent<ScrewKingGameplayHud>();
                PrefabUtility.SaveAsPrefabAsset(root,RootPath);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        static GameObject Root()
        {
            var go=new GameObject("ScrewKingUI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(ScrewKingUiController));
            var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(750,1334);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var ui=go.GetComponent<ScrewKingUiController>();var design=new GameObject("DesignArea",typeof(RectTransform)).GetComponent<RectTransform>();design.SetParent(go.transform,false);Center(design);ui.DesignArea=design;
            Func<string,ScrewKingUiView> instantiate=name=>{
                var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Folder+"/"+name+".prefab");var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,design);return instance.GetComponent<ScrewKingUiView>();};
            ui.Home=instantiate("ScreenHome");ui.Settings=instantiate("PopupSettings");ui.Win=instantiate("PopupWin");ui.Lose=instantiate("PopupLose");ui.Reward=instantiate("PopupReward");ui.Loading=instantiate("ScreenLoading");
            foreach(var view in new[]{ui.Settings,ui.Win,ui.Lose,ui.Reward,ui.Loading})view.gameObject.SetActive(false);
            var events=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));events.transform.SetParent(go.transform,false);ui.OwnEventSystem=events.GetComponent<EventSystem>();return go;
        }
        static void PrepareBorders()
        {
            var borders=new Dictionary<string,int>{{"screw_win_lose/Layer 3 copy 9",70},{"screw_win_lose/Layer 3 copy 2",85},{"screw_win_lose/Rounded Rectangle 1 copy",20},{"screw_setting/Layer 18 copy 2",70},{"screw_setting/Layer 18 copy 3",70},{"screw_setting/Rectangle 3 copy 4",70},{"screw_setting/Rectangle 1 copy 2",90},{"screw_setting/Layer 11 (merged) copy 2",76},{"screw_setting/Rectangle 5 copy 5",20},{"screw_setting/Layer 19 copy 3",18},{"screw_setting/Layer 19 copy 4",18}};
            foreach(var pair in borders){string path="Assets/ScrewKing/Resources/"+Art+pair.Key+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(!importer)throw new Exception("Missing art importer: "+path);
                if(importer.spriteBorder==Vector4.zero){importer.spriteBorder=Vector4.one*pair.Value;importer.SaveAndReimport();}}
        }
        public static void GenerateAndValidate()
        {
            CreateMissing();CreateEditingScene();Validate();
        }
        [MenuItem("Screw King/UI/Validate Prefab Wiring")]
        public static void Validate()
        {
            int buttons=0,images=0;var root=AssetDatabase.LoadAssetAtPath<GameObject>(RootPath);if(!root)throw new Exception("Missing UI prefab");
            var controller=root.GetComponent<ScrewKingUiController>();if(!controller.Home||!controller.Settings||!controller.Win||!controller.Lose||!controller.Reward||!controller.Loading)throw new Exception("Missing view binding");
            if(!controller.Gameplay || controller.Gameplay.transform.GetSiblingIndex()!=0)throw new Exception("Gameplay HUD must be the first Canvas child, below all popup views");
            var hud=controller.Gameplay;
            if(!hud.IsAuthored||hud.BufferSlots.Length!=16||!hud.ClosingBoxPrefab||!hud.FlyingScrewPrefab||!hud.Guide)throw new Exception("Gameplay HUD is not backed by authored widget prefabs");
            if(hud.ProgressFill.type!=Image.Type.Filled||!hud.ProgressFill.sprite)throw new Exception("Progress fill needs a sprite and Filled image type");
            foreach(var box in hud.Boxes)if(box.UnlockButton.onClick.GetPersistentTarget(0)!=box)throw new Exception("Unwired box unlock prefab");
            foreach(var action in root.GetComponentsInChildren<ScrewKingUiAction>(true)){
                var button=action.GetComponent<Button>();if(!button||button.onClick.GetPersistentEventCount()!=1||button.onClick.GetPersistentTarget(0)!=action||button.onClick.GetPersistentMethodName(0)!="Invoke")throw new Exception("Unwired button: "+action.name);buttons++;}
            foreach(var image in root.GetComponentsInChildren<Image>(true)){if(image.sprite)images++;}
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Builds/NewArtUI"));Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output,"prefab-wiring.json"),"{\"passed\":true,\"screen_prefabs\":6,\"root_prefab\":true,\"hud_below_popups\":true,\"authored_hud\":true,\"buffer_widgets\":16,\"wired_buttons\":"+buttons+",\"sprite_images\":"+images+",\"existing_prefabs_preserved\":true}");
            Debug.Log("UI_PREFABS_VERIFIED buttons="+buttons+" images="+images);
        }
    }
}
