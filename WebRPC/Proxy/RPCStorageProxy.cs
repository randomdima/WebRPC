using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;

namespace WebRPC
{
    public class RPCStorageProxy : RPCProxy
    {
        public static RPCStorageProxy Add(string Name, string Url)
        {
            return DynamicHandlers.Add(Name, new RPCStorageProxy() { Url = Url }) as RPCStorageProxy;
        }
    }
}
