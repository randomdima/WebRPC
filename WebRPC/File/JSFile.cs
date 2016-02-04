using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRPC
{
    public class JSFile : RPCFile
    {
        public JSFile(string Name, string Content = null, bool Compress = false) : base(Name, Content, Compress) {

            Type = "application/javascript;charset=utf-8";
        }
 
    }
}
