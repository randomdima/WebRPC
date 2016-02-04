using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using WebRPC.Helpers;

namespace WebRPC
{
    public class RPCClient
    {
        public static ServerInfo GetInfo(string Url)
        {
            
            var request = (HttpWebRequest)HttpWebRequest.Create(Url + "Info");

            request.UseDefaultCredentials = true;
            request.Headers.Add(HttpRequestHeader.AcceptEncoding, "gzip,deflate");
            //request.Headers.Add(HttpRequestHeader.Cookie, "ASP.NET_SessionId=u0x4f0mdiznkhdxgjbvbbwfw");
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var encoding = ASCIIEncoding.UTF8;
            using (var reader = new System.IO.StreamReader(request.GetResponse().GetResponseStream(), encoding))
            {
                string responseText = reader.ReadToEnd();
                Json.InitDefaults();
                return JsonConvert.DeserializeObject<ServerInfo>(responseText);
            }
        }

        public static string Add(string Name = "$", string Url = null)
        {
            ServerInfo Info;
            if (Url!=null)
            {
                Info = GetInfo(Url);
                foreach (var M in Info.Members)
                {
                    if (M.Value is RPCStorageInfo)
                        RPCStorageProxy.Add(M.Key, Url + M.Key);
                    else RPCProxy.Add(M.Key, Url + M.Key);
                }
            }
            else Info = DynamicHandlers.Info;
            
            var Content = Properties.Resources.JSClient;
            if (Name != "$")
                Content = Content.Replace("$", Name);
            var WName = "window." + Name;
            Content += WName+".root='"+DynamicHandlers.Root+"';\n"+WName + ".info=" + JsonConvert.SerializeObject(Info) + ";\n" + WName + ".init();";
            var CName = "RPCClient" + Name;
            DynamicHandlers.Add(CName, new JSFile(CName, Content, true));
            return CName;
        }

        //public static string GenerateCsharpCode(string Name,string Url)
        //{
        //    var SB = new StringBuilder();
        //    var Info = GetInfo(Url);
        //    SB.AppendLine("using System.ComponentModel.DataAnnotations;\nusing System;\nusing WebRPC;\n");
        //    SB.AppendLine("namespace " + Name);
        //    SB.AppendLine("{");
        //    foreach (var T in Info.CustomTypes)
        //    {
        //        var Type = T.Value;
        //        SB.AppendLine(" public class "+Type.Name);
        //        SB.AppendLine(" {");
        //        foreach(var P in Type.Fields)
        //        {
        //            if (Type.KeyField == P.Key)
        //                SB.AppendLine("     [Key]");
        //            SB.AppendLine("     public "+P.Value+" "+P.Key+"{get;set;}");
        //        }
        //        SB.AppendLine(" }");
        //    }

        //    var Classes = Info.Members.Where(q => q.Value is RPCMemberInfo).Select(q => new
        //    {
        //        Class = q.Key.Split('/')[0],
        //        Method = q.Key.Split('/')[1],
        //        Data = q.Value as RPCMemberInfo
        //    }).GroupBy(q => q.Class);
        //    foreach (var Class in Classes)
        //    {
        //        SB.AppendLine(" public static class "+Class.Key);
        //        SB.AppendLine(" {");
        //        foreach (var Method in Class)
        //        {
        //            var MethodUrl = Url+Method.Class+"/"+Method.Method;
        //            if (Method.Data.IsQuerable)
        //                Method.Data.Parameters.Add("Query", "QuerableRequest");
        //            bool Void=Method.Data.ReturnType=="Void";
        //            SB.AppendLine("     public static " + (Void?"void":Method.Data.ReturnType) + " " + Method.Method+"("+string.Join(",",Method.Data.Parameters.Select(q=>q.Value+" "+ q.Key))+")");
        //            SB.AppendLine("     {");
        //            if (!Void) SB.AppendLine("      return ");
        //            SB.AppendLine("         RPCClient.CallMethod<" + (Void?"object":Method.Data.ReturnType) + ">(\"" + MethodUrl + "\",new {" + string.Join(",", Method.Data.Parameters.Select(q => q.Key)) + "});");
        //            SB.AppendLine("     }");
        //        }      
        //        SB.AppendLine(" }");
        //    }

        //    SB.AppendLine("}");
        //    return SB.ToString();
        //}

        //public static T CallMethod<T>(string Url,object Params)
        //{
        //    var Data= Json.Serialize(Params);

        //    bool Post = Data.Length > 200;
        //    HttpWebRequest Req = (HttpWebRequest)WebRequest.Create(Url+(Post?"":("?"+Data)));
        //    // *** Set properties
        //    Req.Timeout = 60000;
        //    Req.Method = Post?"POST":"GET";
        //    Req.Headers.Add("Accept-Encoding", "gzip");
        //    Req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

        //    if (Post)
        //        using (var writer = new StreamWriter(Req.GetRequestStream()))
        //        {
        //            writer.Write(Data);
        //        }

        //    var Resp = ((HttpWebResponse)Req.GetResponse()).GetResponseStream();
        //    return Json.Deserealize<T>(Resp);            
        //}
    }
}
