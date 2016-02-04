using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using WebRPC.Helpers;

namespace WebRPC
{
    public class RPCMember : Attribute
    {
        public static void Add(params Type[] Assemblies)
        {
            var Members = Assemblies.SelectMany(q =>
            {
                var Types = q.Assembly.GetTypes();
                return Types.Union(Types.SelectMany(w => w.GetMembers()));
            });
            foreach (var M in Members)
            {
                var AMembers = M.GetCustomAttributes<RPCMember>();
                foreach (var MA in AMembers)
                    MA.InitHandler(M);
            }
        }
        public string Name { get; set; }
        public RPCMember(string Name = null)
        {
            this.Name = Name;
        }
        protected virtual IHttpHandler CreateHandler(MemberInfo Member)
        {
           // if (SessionEditable)
              //  return new RPCMemberSessionHandler(Member);
            return new RPCMemberHandler(Member);
        }
        
        public virtual IHttpHandler InitHandler(MemberInfo Member)
        {
            if (Name == null) Name = Member.DeclaringType.Name + "/" + Member.Name;
            var H = DynamicHandlers.Get(Name);
            if (H != null) return H;
            return DynamicHandlers.Add(Name, CreateHandler(Member));
        }


        public static object Invoke(string Name, object Params)
        {
            var H = DynamicHandlers.Get(Name) as RPCMemberHandler;
            if (!(Params is JObject)) Params = JObject.FromObject(Params);
            return H.Method(Params as JObject);
        }
    }

    public class RPCMemberHandler : IHttpHandler,IDescriptive
    {
        public HandlerInfo Info { get; protected set; }
        public JMethod Method { get; protected set; }
        public bool IsReusable
        {
            get { return true; }
        }
        public event Action<HttpRequest> OnRequest;
        public event Action<HttpResponse> OnResponse;
        public event Action<Exception> OnException;
        public RPCMemberHandler(MemberInfo Member)
        {
            Method = Member.GetDelegate();
            Info = new RPCMemberInfo(Member);
        }
        public virtual void ProcessRequest(HttpContext Context)
        {
            try
            {
                if (OnRequest != null) OnRequest(Context.Request);
                var Req = Json.ParseRequest(Context.Request, Info as RPCMemberInfo);
                var Result = Method(Req);
                if (Result is IHttpHandler)
                    (Result as IHttpHandler).ProcessRequest(Context);
                else
                {
                    Context.Response.ContentType = "application/json";
                    Context.Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
                    Context.Response.Cache.SetNoStore();
                    Context.Response.Filter = new GZipStream(Context.Response.Filter, CompressionMode.Compress);
                    Context.Response.AppendHeader("Content-Encoding", "gzip");
                    Context.Response.Write(JsonConvert.SerializeObject(Result));
                    if (OnResponse != null) OnResponse(Context.Response);
                }
            }
            catch (JsonReaderException E)
            {
                if (OnException != null) OnException(E);
                DynamicHandlers.FireException(E);
                Context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                Context.Response.ContentType = "text/html";
                Context.Response.Write("Handler [" + Context.Request.Url.LocalPath + "] had failed to parse input query: <br/> " + E.FormatAsHtml());                
            }
            catch (UnauthorizedAccessException E)
            {
                if (OnException != null) OnException(E);
                DynamicHandlers.FireException(E);
                Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                Context.Response.ContentType = "text/html";
                Context.Response.Write("You have no access to [" + Context.Request.Url.LocalPath + "]:  <br/> " + E.FormatAsHtml());
            }
            catch (AuthenticationException E)
            {
                if (OnException != null) OnException(E);
                DynamicHandlers.FireException(E);
                Context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                Context.Response.ContentType = "text/html";
                Context.Response.Write("[" + Context.Request.Url.LocalPath + "]:  <br/> " + E.FormatAsHtml());
            }
            catch (Exception E)
            {
                if (OnException != null) OnException(E);
                DynamicHandlers.FireException(E);
                Context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                Context.Response.ContentType = "text/html";
                Context.Response.Write("Handler [" + Context.Request.Url.LocalPath + "] had thrown an exception:  <br/> " + E.FormatAsHtml());
            }
        }
    }
}
