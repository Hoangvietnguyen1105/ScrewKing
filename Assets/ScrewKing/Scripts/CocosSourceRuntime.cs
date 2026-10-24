using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace ScrewKing
{
    [Serializable] public sealed class SourceGeneration { public LevelData level; public int[] initial, queue; public int randomDraws; }
    [Serializable] public sealed class SourcePose { public int key; public double x,y,angle; }
    [Serializable] public sealed class SourcePoses { public SourcePose[] boards; }

    /// <summary>
    /// Native JavaScriptCore executes the shipped generator and Box2D code, without a WebView.
    /// Unity owns rendering/input; simulation never passes through Unity's different Box2D solver.
    /// This backend currently targets the macOS build only.
    /// </summary>
    public sealed class CocosSourceRuntime : IDisposable
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] static extern int SKSourceCreate();
        [DllImport("__Internal")] static extern IntPtr SKSourceCall(int id,string operation,string args);
        [DllImport("__Internal")] static extern void SKSourceFree(IntPtr pointer);
        [DllImport("__Internal")] static extern void SKSourceDestroy(int id);
        int webContext;
        [Serializable] sealed class Reply { public bool ok; public string value,error; }
#else
        const string Library="/System/Library/Frameworks/JavaScriptCore.framework/JavaScriptCore";
        [DllImport(Library)] static extern IntPtr JSGlobalContextCreate(IntPtr definition);
        [DllImport(Library)] static extern void JSGlobalContextRelease(IntPtr context);
        [DllImport(Library)] static extern IntPtr JSStringCreateWithUTF8CString([MarshalAs(UnmanagedType.LPUTF8Str)]string value);
        [DllImport(Library)] static extern void JSStringRelease(IntPtr value);
        [DllImport(Library)] static extern IntPtr JSEvaluateScript(IntPtr context,IntPtr code,IntPtr target,IntPtr url,int line,out IntPtr error);
        [DllImport(Library)] static extern IntPtr JSValueToStringCopy(IntPtr context,IntPtr value,out IntPtr error);
        [DllImport(Library)] static extern UIntPtr JSStringGetMaximumUTF8CStringSize(IntPtr value);
        [DllImport(Library)] static extern UIntPtr JSStringGetUTF8CString(IntPtr value,byte[] buffer,UIntPtr length);
        IntPtr context;

#endif
        public CocosSourceRuntime()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            webContext=SKSourceCreate();
            if(webContext<=0)throw new Exception("Could not create WebGL source runtime");
#else
            context=JSGlobalContextCreate(IntPtr.Zero);
            if(context==IntPtr.Zero)throw new Exception("Could not create source simulation context.");
            foreach(string name in new[]{"CocosPhysics","CocosRules","Bridge"})Eval(Resources.Load<TextAsset>("Source/"+name+".js").text);
#endif
        }
        #if !UNITY_WEBGL || UNITY_EDITOR
        string ToText(IntPtr value)
        {
            IntPtr result=JSValueToStringCopy(context,value,out var error);
            if(result==IntPtr.Zero)return "<unprintable source error>";
            int capacity=(int)JSStringGetMaximumUTF8CStringSize(result).ToUInt64();
            var bytes=new byte[capacity];int count=(int)JSStringGetUTF8CString(result,bytes,(UIntPtr)capacity).ToUInt64();
            JSStringRelease(result);return Encoding.UTF8.GetString(bytes,0,Math.Max(0,count-1));
        }
        public string Eval(string code)
        {
            if(context==IntPtr.Zero)throw new ObjectDisposedException(nameof(CocosSourceRuntime));
            IntPtr script=JSStringCreateWithUTF8CString(code);
            try
            {
                var result=JSEvaluateScript(context,script,IntPtr.Zero,IntPtr.Zero,1,out var error);
                if(error!=IntPtr.Zero)throw new Exception("Cocos source simulation: "+ToText(error));
                return result==IntPtr.Zero?"":ToText(result);
            }
            finally{JSStringRelease(script);}
        }
        #else
        public string Eval(string code)=>throw new NotSupportedException("WebGL uses fixed source operations, not dynamic code evaluation.");
        #endif
        string Invoke(string operation,string args,string nativeCode)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            if(webContext==0)throw new ObjectDisposedException(nameof(CocosSourceRuntime));
            IntPtr pointer=SKSourceCall(webContext,operation,args);
            if(pointer==IntPtr.Zero)throw new Exception("Source bridge returned no response");
            try {
                var reply=JsonUtility.FromJson<Reply>(Marshal.PtrToStringUTF8(pointer));
                if(reply==null || !reply.ok)throw new Exception("Source bridge: "+reply?.error);
                return reply.value;
            } finally { SKSourceFree(pointer); }
#else
            return Eval(nativeCode);
#endif
        }
        [Serializable] sealed class Quoted { public string value; }
        static string Quote(string value)
        {
            string json=JsonUtility.ToJson(new Quoted{value=value});
            return json.Substring(9,json.Length-10);
        }
        public SourceGeneration Generate(LevelData data,int seed)
        {
            var original=Resources.Load<TextAsset>("Data/Levels/"+data.id);
            string raw=original?original.text:JsonUtility.ToJson(data);
            return GenerateRaw(raw,seed);
        }
        public SourceGeneration GenerateRaw(string raw,int seed)
        {
            string args="["+Quote(raw)+","+seed+"]";
            return JsonUtility.FromJson<SourceGeneration>(Invoke("Generate",args,"SourceGenerate("+Quote(raw)+","+seed+")"));
        }
        public int Pick(int[] values){string list="["+string.Join(",",values)+"]";return int.Parse(Invoke("Pick","["+list+"]","SourcePick("+list+")"));}
        public void ResetPhysics()=>Invoke("ResetPhysics","[]","SourcePhysicsReset()");
        public void AddBoard(int key,int layer,int index)=>Invoke("AddBoard",$"[{key},{layer},{index},-230]",$"SourceBoardAdd({key},{layer},{index},-230)");
        public void ShowBoard(int key,bool show){string value=show?"true":"false";Invoke("ShowBoard",$"[{key},{value}]",$"SourceBoardVisible({key},{value})");}
        public void Dynamic(int key)=>Invoke("Dynamic",$"[{key}]",$"SourceBoardDynamic({key})");
        public void Release(int key,int index)=>Invoke("Release",$"[{key},{index}]",$"SourceRelease({key},{index})");
        public void Remove(int key)=>Invoke("Remove",$"[{key}]",$"SourceBoardRemove({key})");
        public SourcePoses Step(float delta){string dt=delta.ToString("R",System.Globalization.CultureInfo.InvariantCulture);return JsonUtility.FromJson<SourcePoses>(Invoke("Step","["+dt+"]","SourcePhysicsStep("+dt+")"));}
        public bool Covers(int key,Vector2 point,float radius){string args=FormattableString.Invariant($"{key},{point.x*100},{point.y*100},{radius*100}");return Invoke("Covers","["+args+"]","SourceCovers("+args+")")=="true";}
        public void Dispose(){
#if UNITY_WEBGL && !UNITY_EDITOR
            if(webContext!=0){SKSourceDestroy(webContext);webContext=0;}
#else
            if(context!=IntPtr.Zero){JSGlobalContextRelease(context);context=IntPtr.Zero;}
#endif
        }
    }
}
