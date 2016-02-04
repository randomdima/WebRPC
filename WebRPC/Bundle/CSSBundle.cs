using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRPC
{
    public class CSSBundle:CSSFile
    {
        public static List<string> GetStyles(params string[] Path)
        {
            return Path.SelectMany(P => GetFiles(P).Where(q => (Math.Abs(q.LastIndexOf("\\")) > q.LastIndexOf("\\_")))).ToList();  
        }
        public static CSSBundle Add(string Name, params string[] Path)
        {
            if (Path.Length == 0) Path = new string[] { Name };
            return DynamicHandlers.Add(Name, new CSSBundle(Name, GetStyles(Path))) as CSSBundle;
        }
        public CSSBundle(string Name, List<string> Files)
            : base(Name) 
        {
            var Text = string.Join("\n", Files.Select(q => File.ReadAllText(GetFullPath(q))));
            this.SetContent(new Minifier().MinifyStyleSheet(Text), true);
        }
    }
}
