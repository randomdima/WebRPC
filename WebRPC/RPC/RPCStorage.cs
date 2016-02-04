using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using WebRPC.Helpers;

namespace WebRPC
{  
    public class RPCStorage : RPCMember
    {
        public static void Reset(string Name = "Storage")
        {
            RPCStorageFile Storage = DynamicHandlers.Get(Name) as RPCStorageFile;
            Storage.Reset();
        }
        public RPCStorage(string Name = "Storage"):base(Name) { }
        protected override IHttpHandler CreateHandler(MemberInfo Member) 
        {
            return new RPCStorageFile(Name);
        }
        public override IHttpHandler InitHandler(MemberInfo Member)
        {
            RPCStorageFile Storage = base.InitHandler(Member) as RPCStorageFile;
            Storage.Members.Add(Member.Name, Member.GetDelegate());
            return Storage;
        }
    }
    public class RPCSession : RPCStorage
    {
        public static void Reset(string Name = "Session")
        {
            RPCSessionFile Storage = DynamicHandlers.Get(Name) as RPCSessionFile;
            Storage.Reset();
        }
        public static void ResetAll(string Name = "Session")
        {
            RPCSessionFile Storage = DynamicHandlers.Get(Name) as RPCSessionFile;
            Storage.ResetAll();
        }
        public RPCSession(string Name = "Session"):base(Name)
        {
        }
        protected override IHttpHandler CreateHandler(MemberInfo Member)
        {
            return new RPCSessionFile(Name);
        }
    }

    public class RPCStorageFile : JSFile,IDescriptive
    {
#if DEBUG
        protected static string DefaultStorageCache = "max-age=0,must-revalidate,private";
#else
        protected static string DefaultStorageCache = "max-age=120,must-revalidate,private";
#endif
        public Dictionary<string, JMethod> Members = new Dictionary<string, JMethod>();
        public HandlerInfo Info{get;  private set;}
        public virtual void Reset()
        {
            Content = null;
        }
        public RPCStorageFile(string Name) : base(Name) {             
            Info = new RPCStorageInfo();
            Cache = DefaultStorageCache;
        }
        public override void ProcessRequest(HttpContext Context)
        {
            if(Content==null)
                SetContent("window." + Name.Replace(".js","") + "=" + JsonConvert.SerializeObject(Members.ToDictionary(q => q.Key, q => q.Value())), true);
            base.ProcessRequest(Context);
        }


    }
    public class RPCSessionFile : RPCStorageFile
    {
        protected string ProfileName { get; set; }
        public RPCSessionFile(string Name) : base(Name) {
            ProfileName = "_" + Name;
        }
        public void Reset()
        {
            RPCStorageFile Storage = Profile.Get<RPCStorageFile>(ProfileName);
            if (Storage!=null) Storage.Reset();
        }
        public void ResetAll()
        {
            Profile.Foreach<RPCStorageFile>(ProfileName, q => q.Reset());
        }
        public override void ProcessRequest(HttpContext Context)
        {
            RPCStorageFile Storage = Profile.Get(ProfileName, q => new RPCStorageFile(Name) { Members = Members });     
            Storage.ProcessRequest(Context);
        }
    }
}
