using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRPC
{
   public  class CSSFile:RPCFile
   {
       public CSSFile(string Name, string Content = null, bool Compress = false) : base(Name, Content, Compress) {
           Type = "text/css;charset=utf-8";
       }
    }
}
