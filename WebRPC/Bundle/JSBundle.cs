using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRPC
{
    public class JSBundle : JSFile
    {
        public static List<string> GetScripts(params string[] Path)
        {
            return Path.SelectMany(P => GetFiles(P).Where(q => (Math.Abs(q.LastIndexOf("\\")) > q.LastIndexOf("\\_")) && !q.EndsWith("vsdoc.js"))).ToList();            
        }
        public static JSBundle Add(string Name, params string[] Path)
        {
            if (Path.Length == 0) Path = new string[] { Name };
            return DynamicHandlers.Add(Name, new JSBundle(Name, GetScripts(Path))) as JSBundle;
        }
        public JSBundle(string Name, List<string> Files)
            : base(Name)
        {
            var Text = string.Join(";\n", Files.Select(q => File.ReadAllText(GetFullPath(q))));
            this.SetContent(new Minifier().MinifyJavaScript(Text, new CodeSettings() { LocalRenaming = LocalRenaming.KeepAll }), true);
        }
    }
}
