using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebRPC
{
    public interface IDescriptive
    {
        HandlerInfo Info { get; }
    }
    public class HandlerInfo:Dictionary<string,object>
    {
        public static string GetTypeName(Type type)
        {
            type = GetNotNullableType(type);
            var Element = GetEnumerableElementType(type);
            if (Element != null)
            {
                return Element.Name + "[]";
            }
            else return type.Name;
        }
        public static Type GetEnumerableElementType(Type type)
        {
            if (typeof(IEnumerable).IsAssignableFrom(type))
                return type.GetElementType() ?? type.GetGenericArguments().FirstOrDefault();
            return null;
        }
        public static Type GetNotNullableType(Type Type)
        {
            return Nullable.GetUnderlyingType(Type) ?? Type;
            //if (Type.IsGenericType && Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            //    return Type.GetGenericArguments().FirstOrDefault();
            //return Type;
        }

        public static List<Type> KnownTypes = new List<Type>() { typeof(HttpFileCollection), typeof(HttpPostedFile) };
        public static bool IsCustomType(Type type)
        {
            if (KnownTypes.Contains(type)) return false;
            return !type.IsValueType && type!=typeof(void) &&  System.Type.GetTypeCode(type) == TypeCode.Object;
        }

        public List<Type> CustomTypes { get; private set; }
        public bool AddType(Type Type)
        {
            if (CustomTypes == null) CustomTypes = new List<System.Type>();
            ////If Collections
            Type = GetEnumerableElementType(Type)??Type;
            
            //If type is custom
            if (Type == null || !IsCustomType(Type)) return false;
            if(!CustomTypes.Contains(Type))
                CustomTypes.Add(Type);
            return true;
        }
    }
}
