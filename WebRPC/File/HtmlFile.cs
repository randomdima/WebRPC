using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRPC
{
    public class HtmlFile:RPCFile
    {
        public static void Add(string Name="", string Title = null, string Icon = null, bool Minify=false, string[] Scripts=null,string[] Styles=null)
        {
            DynamicHandlers.Add(Name, new HtmlFile(Name, Title, Icon, Minify, Scripts,Styles));
        }
        public HtmlFile(string Name, string Content = null, bool Compress = false) : base(Name, Content, Compress) {
            Type = "text/html;charset=utf-8";
        }
        public HtmlFile(string Name = "", string Title = null, string Icon = null, bool Minify = false, string[] Scripts = null, string[] Styles = null)
            : base(Name)
        {
            StringBuilder SB = new StringBuilder();
            SB.AppendLine("<html><head>\n<meta http-equiv='x-ua-compatible' content='IE=edge'>");
            if (Title != null)
            {
                SB.Append("<title>");
                SB.Append(Title);
                SB.AppendLine("</title>");
            }

            if (Icon != null)
            {
                SB.Append("<link rel='shortcut icon' href='");
                SB.Append(Icon);
                SB.AppendLine("' />");
            }

            if (Scripts != null)
                foreach (string S in Scripts)
                    if (!S.EndsWith(".js"))
                    {
                        SB.AppendLine("<script  src='" + S + "'></script>");
                        continue;
                    }
                    else
                    {
                        var Files = JSBundle.GetScripts(S);
                        if (Files.Count > 1 && Minify)
                        {
                            var ScriptName = S.Replace("/*.js", "_js");
                            DynamicHandlers.Add(ScriptName, new JSBundle(ScriptName, Files));
                            SB.AppendLine("<script  src='" + ScriptName + "'></script>");
                        }
                        else
                            foreach (var F in Files)
                                SB.AppendLine("<script  src='" + F + "'></script>");
                    }

            if (Styles != null)
                foreach (string S in Styles)
                    if (!S.EndsWith(".css"))
                    {
                        SB.AppendLine("<link rel='stylesheet' href='" + S + "'>");
                        continue;
                    }
                    else
                    {
                        var Files = CSSBundle.GetStyles(S);
                        if (Files.Count > 1 && Minify)
                        {
                            var StyleName = S.Replace("/*.css", "_css");
                            DynamicHandlers.Add(StyleName, new CSSBundle(StyleName, Files));
                            SB.AppendLine("<link rel='stylesheet' href='" + StyleName + "'>");
                        }
                        else
                            foreach (var F in Files)
                                SB.AppendLine("<link rel='stylesheet' href='" + F + "'>");
                    }
            SB.AppendLine("</head><body></body></html>");
            SetContent(SB.ToString(), true);
            Type = "text/html;charset=utf-8";
        }
    }
}
