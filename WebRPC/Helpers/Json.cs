using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace WebRPC.Helpers
{
    public class NoGMTJavaScriptDateTimeConverter : JavaScriptDateTimeConverter
    {
        public static NoGMTJavaScriptDateTimeConverter Instance;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.Value == null) return null;
            var rs = reader.Value.ToString();
            return rs==null?null:((object)DateTime.Parse(rs));
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var D = (DateTime)value;
            writer.WriteRawValue("new Date(" + D.Year + "," + (D.Month - 1) + "," + D.Day + ")");
        }
    }
    public class JReference : JObject
    {
        public object Reference { get; set; }
        public JReference(object reference)
        {
            Reference = reference;
        }    }
    public class KnownTypesBinder : SerializationBinder
    {
        public static KnownTypesBinder Instance;

        public IList<Type> KnownTypes { get; set; }

        public override Type BindToType(string assemblyName, string typeName)
        {
            return KnownTypes.SingleOrDefault(t => t.Name == typeName);
        }

        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            assemblyName = null;
            typeName = serializedType.Name;
        }
    }
    public delegate object JMethod(JObject Params=null);
    public static class Json
    {
        public static void InitDefaults()
        {
            if (KnownTypesBinder.Instance != null) return;
            KnownTypesBinder.Instance= new KnownTypesBinder() { 
                KnownTypes = new List<Type>() {
                    typeof(RPCStorageInfo),
                    typeof(RPCMemberInfo),
                    typeof(HandlerInfo)
                } 
            };
            NoGMTJavaScriptDateTimeConverter.Instance = new NoGMTJavaScriptDateTimeConverter();

            JsonConvert.DefaultSettings = () =>
                new JsonSerializerSettings() { 
                    Converters = new List<JsonConverter>() { NoGMTJavaScriptDateTimeConverter.Instance },
                    Binder=  KnownTypesBinder.Instance,
                    NullValueHandling = NullValueHandling.Ignore
                };
        }

        public static object GetDefaultValue(Type Type)
        {
            if (Type.IsValueType)
            {
                return Activator.CreateInstance(Type);
            }
            return null;
        }
        public static object GetParamDefaultValue(ParameterInfo Param)
        {
            if (Param.HasDefaultValue) return Param.DefaultValue;
            return GetDefaultValue(Param.ParameterType);
        }

        public static JObject ParseRequest(HttpRequest Request,RPCMemberInfo Info)
        {
            JObject res=null;
            if (Request.QueryString.Keys.Count > 0)
            {
                if (Request.QueryString.AllKeys[0] == null)
                    res = JObject.Parse(Request.QueryString[0]);
                else
                {
                    res = new JObject();
                    foreach (var P in Request.QueryString.AllKeys)
                        res[P] = Request.QueryString[P];
                }
            }

            if (Request.HttpMethod == "POST")
            {
                var form= Request.Unvalidated.Form;
                if (form.Count > 0)
                {
                    string Root = form[""];
                    if (Root != null) res = JObject.Parse(Root);
                    else if (res == null) res = new JObject();
                    foreach (string q in form.AllKeys)
                        if(q.Length>0)
                            res[q] = form[q];
                }
                if (Request.Files.Count > 0 && Info!=null)
                {
                    if (res == null) res = new JObject();
                    var Param=Info.Parameters.FirstOrDefault(q => q.Value == "HttpFileCollection");
                    if (Param.Key != null)
                        res.Add(Param.Key, new JReference(Request.Files));
                    else
                    {
                        foreach (var q in Request.Files.AllKeys)
                            res[q] =new JReference(Request.Files[q]);
                    }                    
                }
            }
            return res;
        }

        private static JsonSerializer JsonSerializer = new JsonSerializer();
        public static T Field<T>(JObject Obj,string Name,T Def)
        {
            if (Obj == null) return Def;
            var Val = Obj[Name];
            if (Val == null) { 
                Obj=Obj["Where"] as JObject;
                if(Obj==null) return Def;
                Val = Obj[Name];
                if (Val == null) return Def;
                Obj.Remove(Name);
            }
            else
                Obj.Remove(Name);
            var Ref=Val as JReference;
            if (Ref != null) 
                return (T)Ref.Reference;
            return Val.ToObject<T>(JsonSerializer);
        }
        private static MethodInfo DisposeMethod = typeof(IDisposable).GetMethod("Dispose");
        private static MethodInfo QInvokeMethod = typeof(QueryableHelper).GetMethod("Invoke", new Type[] { typeof(IQueryable), typeof(JObject) });
        private static MethodInfo EInvokeMethod = typeof(QueryableHelper).GetMethod("Invoke", new Type[] { typeof(IEnumerable), typeof(JObject) });
        private static Expression GetLambda_Exec(Expression Caller, ParameterExpression Param)
        {
            // J => {Method(J);return null;}
            if (Caller.Type == typeof(void))
                return Expression.Block(Caller,Expression.Constant(null));
            
            // J=> Exec(Method(J))
            if (typeof(IQueryable).IsAssignableFrom(Caller.Type))            
                return Expression.Call(QInvokeMethod, Caller, Param);

            

            // J=>(object)Method(J)
            if(Caller.Type.IsValueType)
                return Expression.Convert(Caller, typeof(object));

            if (typeof(IEnumerable).IsAssignableFrom(Caller.Type))
                return Expression.Call(EInvokeMethod, Caller, Param);

            // J=>Method(J)
            return Caller;
        }
        public static JMethod GetDelegate(this MethodInfo MI)
        {
            ParameterExpression JParam = Expression.Parameter(typeof(JObject), "j");

            //Creating Param Convert/Assign -> F(J["A"],J["B"],...)
            Expression[] PBinder = MI.GetParameters().Select(q => Expression.Call(typeof(Json), "Field", new Type[] { q.ParameterType },
                                                                        JParam,
                                                                        Expression.Constant(q.Name),
                                                                        Expression.Constant(GetParamDefaultValue(q), q.ParameterType)))
                                        .ToArray();
           
            // Simple Static Method Invoke
            // J=> Class.Method(J["A"],J["B"],...)
            if (MI.IsStatic)
                return Expression.Lambda<JMethod>(GetLambda_Exec(Expression.Call(MI, PBinder), JParam), JParam).Compile();

            //Constructor for not static methods
            Expression Constructor=Expression.New(MI.DeclaringType.GetConstructor(new Type[] { }));
            
            //if not disposable -> inline call
            //J=> (new Class()).Method(J["A"],J["B"],...)
            if (!typeof(IDisposable).IsAssignableFrom(MI.DeclaringType))
                return Expression.Lambda<JMethod>(GetLambda_Exec(Expression.Call(Constructor, MI, PBinder), JParam), JParam).Compile();

            //if disposable -> need to dispose 
            ParameterExpression Instance = Expression.Variable(MI.DeclaringType, "i");
            
            //J=> using(var i=new Class()) i.Method(J["A"],J["B"],...);
            var res=Expression.Block(
                new[]{Instance},
                Expression.TryFinally(
                        GetLambda_Exec(Expression.Call(Expression.Assign(Instance, Constructor), MI, PBinder),JParam),
                        Expression.Call(Instance, DisposeMethod)));

            return Expression.Lambda<JMethod>(res, JParam).Compile();
        }
        public static JMethod GetDelegate(this TypeInfo TI)
        {
            object Constant;
            if (TI.IsEnum)
                Constant = Enum.GetValues(TI).Cast<object>().ToDictionary(q => q.ToString(), q => (int)q);
            else Constant = TI.GetFields(BindingFlags.Public | BindingFlags.Static).ToDictionary(q => q.Name, q => q.GetValue(null));
            return q => Constant;
        }
        public static JMethod GetDelegate(this FieldInfo FI)
        {
            ParameterExpression JParam = Expression.Parameter(typeof(JObject), "j");

            // Simple Static Method Invoke
            // J=> Class.Method(J["A"],J["B"],...)
            if (FI.IsStatic)
                return Expression.Lambda<JMethod>(GetLambda_Exec(Expression.Field(null,FI), JParam),JParam).Compile();

            //Constructor for not static methods
            Expression Constructor = Expression.New(FI.DeclaringType.GetConstructor(new Type[] { }));

            //if not disposable -> inline call
            //J=> (new Class()).Method(J["A"],J["B"],...)
            if (!typeof(IDisposable).IsAssignableFrom(FI.DeclaringType))
                return Expression.Lambda<JMethod>(GetLambda_Exec(Expression.Field(Constructor, FI), JParam), JParam).Compile();

            //if disposable -> need to dispose 
            ParameterExpression Instance = Expression.Variable(FI.DeclaringType, "i");

            //J=> using(var i=new Class()) i.Method(J["A"],J["B"],...);
            var res = Expression.Block(
                new[] { Instance },
                Expression.TryFinally(
                        GetLambda_Exec(Expression.Field(Expression.Assign(Instance, Constructor), FI), JParam),
                        Expression.Call(Instance, DisposeMethod)));

            return Expression.Lambda<JMethod>(res, JParam).Compile();
        }
        public static JMethod GetDelegate(this MemberInfo Q)
        {
            var MI = Q as MethodInfo;
            if (MI != null) return MI.GetDelegate();
            var PI = Q as PropertyInfo;
            if (PI != null) return PI.GetMethod.GetDelegate();
            var FI = Q as FieldInfo;
            if (FI != null) return FI.GetDelegate();
            var TI = Q as TypeInfo;
            if (TI != null) return TI.GetDelegate();
            throw new Exception("Member Type is not supported");
        }
    }
}
