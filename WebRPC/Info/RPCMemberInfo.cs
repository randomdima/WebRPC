using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;
using WebRPC.Helpers;

namespace WebRPC
{
    public class RPCMemberInfo : HandlerInfo
    {
        public Dictionary<string, string> Parameters
        {
            get { return this["Parameters"] as Dictionary<string, string>; }
            set { this["Parameters"] = value; }
        }
        public string ReturnType { 
            get { return (string)this["ReturnType"]; }
            set { this["ReturnType"] = value; } 
        }
        public bool IsQuerable
        {
            get { return (bool)this["IsQuerable"]; }
            set { this["IsQuerable"] = value; }
        }
        public RPCMemberInfo() : base() { }
        public RPCMemberInfo(MemberInfo Member)
        {
            var RType = GetReturnType(Member);
           AddType(RType);
           ReturnType = GetTypeName(RType);
           IsQuerable = typeof(IQueryable).IsAssignableFrom(RType);
            if (Member is MethodInfo)
            {
                Parameters = new Dictionary<string, string>();
                foreach (var P in (Member as MethodInfo).GetParameters())
                {
                    Parameters.Add(P.Name,GetTypeName(P.ParameterType));
                    AddType(P.ParameterType);
                }
            }
        }
     
        public static Type GetReturnType(MemberInfo Info)
        {
            if (Info is MethodInfo) return (Info as MethodInfo).ReturnType;
            if (Info is PropertyInfo) return (Info as PropertyInfo).PropertyType;
            return null;
        }
    }
}
