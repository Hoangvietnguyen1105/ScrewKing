using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
namespace ScrewKing
{
    [Serializable] public sealed class TikTokSessionResult {public string playerId,sessionToken;}
    public interface ITikTokSessionGateway {void Exchange(string code,Action<TikTokSessionResult> result);}
    public sealed class TikTokSessionGateway:ITikTokSessionGateway
    {
        [Serializable] sealed class LoginBody{public string code;}
        readonly MonoBehaviour host;readonly string url;
        public TikTokSessionGateway(MonoBehaviour owner,string endpoint){host=owner;url=endpoint;}
        public static bool IsHttps(string value)=>Uri.TryCreate(value,UriKind.Absolute,out var u)&&u.Scheme==Uri.UriSchemeHttps&&!string.IsNullOrWhiteSpace(u.Host)&&string.IsNullOrEmpty(u.UserInfo);
        public void Exchange(string code,Action<TikTokSessionResult> result){if(!IsHttps(url)||string.IsNullOrEmpty(code)){result(null);return;}host.StartCoroutine(Send(code,result));}
        IEnumerator Send(string code,Action<TikTokSessionResult> result)
        {
            using(var request=new UnityWebRequest(url,"POST")){
                request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(new LoginBody{code=code})));
                request.downloadHandler=new DownloadHandlerBuffer();request.timeout=12;request.SetRequestHeader("Content-Type","application/json");
                yield return request.SendWebRequest();
                TikTokSessionResult session=null;
                if(request.result==UnityWebRequest.Result.Success){try{session=JsonUtility.FromJson<TikTokSessionResult>(request.downloadHandler.text);}catch{}}
                if(session==null||string.IsNullOrEmpty(session.playerId)||session.playerId.Length>256||string.IsNullOrEmpty(session.sessionToken))session=null;
                result(session); // Authorization codes and response bodies are deliberately never logged.
            }
        }
    }
}
