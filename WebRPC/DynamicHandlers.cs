using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Routing;
using System.Web.SessionState;
using WebRPC.Helpers;

namespace WebRPC
{

    public partial class DynamicHandlers :IRouteHandler//, IHttpHandlerFactory//, IReadOnlySessionState
    {
        public static bool Impersonate = false;
        public static string Root = "";
        public static Dictionary<string, IHttpHandler> Handlers = new Dictionary<string, IHttpHandler>();
        public static ServerInfo Info=new ServerInfo();

        public static event Action<Exception> OnException;
        public bool IsReusable
        {
            get { return true; }
        }
        static DynamicHandlers()
        {
            Json.InitDefaults();
        }

        public static void AddRoutes(string Root="")
        {
            DynamicHandlers.Root = Root;
            RouteTable.Routes.Add(new Route(Root + "{A}/{B}", new DynamicHandlers()));
            RouteTable.Routes.Add(new Route(Root + "{A}", new DynamicHandlers()));
            RouteTable.Routes.Add(new Route(Root, new DynamicHandlers()));
        }
        public IHttpHandler GetHttpHandler(RequestContext requestContext)
        {
            string A = (string)requestContext.RouteData.Values["A"];
            string B = (string)requestContext.RouteData.Values["B"];
            string Path = A==null?"":( B == null ? A : (A + "/" + B));
            IHttpHandler Handler = DynamicHandlers.Get(Path);
            if (Handler == null)
            {
                requestContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                requestContext.HttpContext.Response.Write("Handler [" + Path + "] does not exist.");
                requestContext.HttpContext.Response.End();
            }
            return Handler;
        }

        public static void FireException(Exception E)
        {
            if (OnException!=null) OnException(E);
        }
        public static IHttpHandler Add(string Path, IHttpHandler Handler)
        {
            Path = Path.Replace("\\", "/").Replace("../","");
            Handlers.Add(Path.ToLower(), Handler);

            if (Handler is IDescriptive)
            {
                var I = (Handler as IDescriptive).Info;
                if (I.CustomTypes != null)
                    foreach (var T in I.CustomTypes)
                        if (!Info.CustomTypes.ContainsKey(T.Name))
                            Info.CustomTypes.Add(T.Name, new RPCTypeInfo(T));
                Info.Members.Add(Path, I);
            }
            return Handler;
        }
        public static IHttpHandler Get(string Path)
        {
            IHttpHandler Handler=null;
            Handlers.TryGetValue(Path.ToLower(), out Handler);
            return Handler;
        }
        public static void AddInfo()
        {
            Add("Info", new JSFile("Info", JsonConvert.SerializeObject(Info), true));
        }

    }
}
