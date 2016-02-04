using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using System.Security.Principal;

namespace WebRPC
{
    public class RPCProxy : IHttpHandler//, IReadOnlySessionState
    {
        public static RPCProxy Add(string Name, string Url)
        {
            return DynamicHandlers.Add(Name, new RPCProxy() { Url = Url }) as RPCProxy;
        }
        public string Url { get; set; }
        public bool IsReusable
        {
            get { return true; }
        }

        private static List<string> RespHeaders = new List<string>() { "ETag", "Content-Disposition", "Content-Encoding", "Cache-Control" };//, "Expires", "Pragma"};
        protected virtual void ProxyResponse(HttpResponse target, HttpWebResponse source)
        {
            target.StatusCode = (int)source.StatusCode;
            target.Charset = source.CharacterSet;
            target.ContentType = source.ContentType;
           
            foreach (string H in RespHeaders)
            {
                var V = source.Headers[H];
                if (V != null)
                    target.AppendHeader(H, V);
            }
            using (var RespStream = source.GetResponseStream())
                RespStream.CopyTo(target.OutputStream);
        }

        private static List<string> ReqHeaders = new List<string>() { "Accept-Encoding", "If-None-Match"};
        protected virtual HttpWebResponse ProxyRequest(HttpRequest source)
        {          
            HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(Url + "?" + source.QueryString.ToString());
            Req.KeepAlive = false;
            Req.ImpersonationLevel = System.Security.Principal.TokenImpersonationLevel.Delegation;
            Req.UseDefaultCredentials = true;

            // *** Set properties
            Req.Timeout = 60000;
            Req.UserAgent = source.UserAgent;
            Req.Method = source.HttpMethod;
            Req.ContentType = source.ContentType;

            Req.Headers.Add("Impersonate", source.LogonUserIdentity.Name);
            foreach (string H in ReqHeaders)
            {
                var V = source.Headers[H];
                if (V != null)
                    Req.Headers.Add(H, V);
            }

            if (Req.Method == "POST")
                using (var ReqStream = Req.GetRequestStream())
                {
                    source.InputStream.CopyTo(ReqStream);
                }
            return Req.GetResponse() as HttpWebResponse;            
        }
        public void ProcessRequest(HttpContext context)
        {
            try
            {
               // WindowsIdentity identity = (WindowsIdentity)HttpContext.Current.User.Identity;
              //  using (identity.Impersonate())
               // {
                    ProxyResponse(context.Response, ProxyRequest(context.Request));
               // }
            }
            catch (System.Net.WebException ex)
            {
                ProxyResponse(context.Response, (HttpWebResponse)ex.Response);
            }
        }
    }
}
