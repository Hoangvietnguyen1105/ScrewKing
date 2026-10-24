using System;
namespace ScrewKing
{
    public interface ITikTokMiniGameApi:IDisposable
    {
        bool IsLive {get;}
        event Action Resumed;
        void Initialize(Action<bool> result);
        bool Supports(TikTokMission mission);
        void Login(Action<string> success,Action failure);
        void Navigate(TikTokMission mission,Action<bool> result);
        void CheckReward(TikTokMission mission,Action<bool,bool> result);
        string LoadSave();
        bool Save(string json);
    }
    public sealed class PreviewTikTokApi:ITikTokMiniGameApi
    {
        public bool IsLive=>false;
        public event Action Resumed {add{} remove{}}
        public void Initialize(Action<bool> result)=>result(false);
        public bool Supports(TikTokMission mission)=>false;
        public void Login(Action<string> success,Action failure)=>failure();
        public void Navigate(TikTokMission mission,Action<bool> result)=>result(false);
        public void CheckReward(TikTokMission mission,Action<bool,bool> result)=>result(false,false);
        public string LoadSave()=>UnityEngine.PlayerPrefs.GetString(TikTokCapabilities.LocalSaveKey,"");
        public bool Save(string json){UnityEngine.PlayerPrefs.SetString(TikTokCapabilities.LocalSaveKey,json);UnityEngine.PlayerPrefs.Save();return true;}
        public void Dispose(){}
    }
}
