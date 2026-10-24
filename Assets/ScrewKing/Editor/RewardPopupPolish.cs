using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ScrewKing.Editor
{
    public static class RewardPopupPolish
    {
        public static void ApplyAndTest(){ApplyOnly();UiPrefabPlaySmoke.Run();}
        public static void ApplyOnly()
        {
            string path=UiPrefabBuilder.Folder+"/PopupReward.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try{
                // Preserve the root GameObject and view IDs used by the nested UI root prefab.
                while(root.transform.childCount>0)Object.DestroyImmediate(root.transform.GetChild(0).gameObject);
                UiPrefabBuilder.BuildRewardContents(root);PrefabUtility.SaveAsPrefabAsset(root,path);
            }finally{PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();RenderPreview();
        }
        public static void RenderPreview()
        {
            var root=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(UiPrefabBuilder.RootPath));
            var camObject=new GameObject("Reward preview camera");var camera=camObject.AddComponent<Camera>();
            var rt=new RenderTexture(750,1334,24);
            try{
                var ui=root.GetComponent<ScrewKingUiController>();
                ui.PreviewReward();ui.enabled=false;
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(camObject,root.scene);
                var view=ui.Reward;
                view.TitleLabel.text="REVIVE!";view.DescriptionLabel.text="Open another box\nand keep playing.";
                view.RewardButtonLabel.text="Continue";view.CancelButtonLabel.text="Give up";view.StatusLabel.text="Preview · reward simulated";
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=6.67f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.28f,.13f,.05f);camera.targetTexture=rt;
                Canvas.ForceUpdateCanvases();camera.Render();
                var previous=RenderTexture.active;RenderTexture.active=rt;var texture=new Texture2D(750,1334,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,750,1334),0,0);texture.Apply();RenderTexture.active=previous;
                Directory.CreateDirectory("../Builds/RewardPopup");File.WriteAllBytes("../Builds/RewardPopup/revive-preview.png",texture.EncodeToPNG());Object.DestroyImmediate(texture);
            }finally{camera.targetTexture=null;rt.Release();Object.DestroyImmediate(rt);Object.DestroyImmediate(camObject);Object.DestroyImmediate(root);}
        }
    }
}
