using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;

namespace WebRPC
{
    public static class ETagHelper
    {
        public static string GetETag(this FileInfo Info)
        {
            return GetETag(Info.LastWriteTimeUtc, Info.Length);
        }
        public static string GetETag(params object[] Data)
        {
            return string.Join("", Data.Select(q =>
            {
                if (q is DateTime) return ((DateTime)q).Ticks.ToString();
                return q.ToString();
            }));
        }
    }
    public class RPCFile : IHttpHandler//, IReadOnlySessionState
    {
#if DEBUG
        protected static string DefaultCache = "max-age=0,must-revalidate,private";
#else
        protected static string DefaultCache = "max-age=300,must-revalidate,private";
#endif
        public static string GetFullPath(string Path)
        {
            return HttpRuntime.AppDomainAppPath+Path;
        }
        public static string GetLocalPath(string Path)
        {
            return Path.Substring(HttpRuntime.AppDomainAppPath.Length);
        }
        public static List<string> GetFiles(params string[] Path)
        {
            List<string> Res = new List<string>();
            foreach (var _File in Path)
            {
                var File = _File.Replace("/","\\");
                var HasPattern = File.Contains("*");
                if (HasPattern || System.IO.Path.GetExtension(File) == "")
                {
                    var LastBlock = HasPattern ? File.LastIndexOf("\\") : File.Length;
                    var Pattern = HasPattern ? File.Substring(LastBlock + 1) : "*";
                    Res.AddRange(Directory.EnumerateFiles(GetFullPath(File.Substring(0, Math.Max(0, LastBlock))), Pattern, SearchOption.AllDirectories)
                                      .OrderBy(q => q)
                                      .Select(q => GetLocalPath(q)));
                }
                else Res.Add(File);
            }
            return Res;
        }

        public string ETag { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Compressed { get; set; }
        public string Cache { get; set; }
        public byte[] Content { get; set; }

        public RPCFile(string Name, object Content = null, bool Compress = false)
        {
            this.Name = Name;
            SetContent(Content, Compress);
            Type = MimeMapping.GetMimeMapping(Name);
        }
       
        public virtual void SetContent(object Content, bool Compress = false)
        {
            if (Content == null)
            {
                this.Content = null;
                return;
            }
            if (Content is byte[])
                this.Content = Content as byte[];
            else this.Content = UTF8Encoding.UTF8.GetBytes(Content.ToString());

            Compressed = Compress;
            if (Compress)
            {
                using (var msi = new MemoryStream(this.Content))
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress))
                        msi.CopyTo(gs);
                    this.Content = mso.ToArray();
                }
            }
            //Content Hash based ETag
            using (var sha1 = new SHA1CryptoServiceProvider())
                ETag = Convert.ToBase64String(sha1.ComputeHash(this.Content));
        }

        
        public virtual bool IfCached(HttpContext Context)
        {
            Context.Response.AddHeader("Cache-Control", Cache??DefaultCache);
           // Context.Response.Expires = -1;
            if (ETag !=null && Context.Request.Headers["If-None-Match"] == ETag)
            {
                Context.Response.StatusCode = (int)System.Net.HttpStatusCode.NotModified;
                return true;
            }
            return false;
        }
        public virtual void ApplyHeaders(HttpContext Context)
        {
            if (ETag != null) Context.Response.AddHeader("ETag", ETag);
            if(Compressed) Context.Response.AddHeader("Content-Encoding", "gzip");

            Context.Response.AddHeader("Content-Disposition", "filename=\"" + Name + "\"");
            Context.Response.ContentType = Type;
        }

        public bool IsReusable
        {
            get { return true; }
        }

        public virtual void ProcessRequest(HttpContext Context)
        {           
            if (IfCached(Context)) return;  
            ApplyHeaders(Context);
            Context.Response.BinaryWrite(Content);   
        }
    }
}
