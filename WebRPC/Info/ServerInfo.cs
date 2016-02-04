using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebRPC
{
    public class ServerInfo
    {
        public Dictionary<string,RPCTypeInfo> CustomTypes { get; set; }
        [JsonProperty(ItemTypeNameHandling=TypeNameHandling.All)]
        public Dictionary<string, HandlerInfo> Members { get; set; }
        public ServerInfo()
        {
            CustomTypes = new Dictionary<string, RPCTypeInfo>();
            Members = new Dictionary<string, HandlerInfo>();
        }
    }
}
