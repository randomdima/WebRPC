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
    public class RPCTypeInfo
    {
        public string Name { get; set; }
        public string KeyField { get; set; }
        public Dictionary<string, string> Fields { get; set; }
        public RPCTypeInfo() { }
        public RPCTypeInfo(Type Type)
        {
            Fields = new Dictionary<string, string>();
            Name = Type.Name;           
            foreach (var P in Type.GetProperties())
            {
                if(HandlerInfo.IsCustomType(P.PropertyType))continue;
                if (P.GetCustomAttributes(typeof(KeyAttribute), true).Length > 0)
                    KeyField=P.Name;
                Fields.Add(P.Name, HandlerInfo.GetTypeName(P.PropertyType));
            }
        }
    }
}
