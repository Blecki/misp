using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class AutoBind
    {
        public static ScriptList ListArgument(Object obj)
        {
            if (obj is ScriptList) return obj as ScriptList;
            return new ScriptList(obj);
        }

        public static float NumericArgument(Object obj)
        {
            return Convert.ToSingle(obj);
        }

        public static int IntArgument(Object obj)
        {
            return Convert.ToInt32(obj);
        }

        public static uint UIntArgument(Object obj)
        {
            return Convert.ToUInt32(obj);
        }

        public static bool BooleanArgument(Object obj)
        {
            return Convert.ToBoolean(obj);
        }

        public static string StringArgument(Object obj)
        {
            return ScriptObject.AsString(obj);
        }

        public static ScriptObject LazyArgument(Object obj)
        {
            return obj as ScriptObject;
        }

        public static T ClassArgument<T>(Object obj) where T : class
        {
            if (!(obj is T)) throw new ScriptError("Argument wrong type", null);
            return obj as T;
        }

        public static ScriptObject GenerateMethodBinding(System.Reflection.MethodInfo method)
        {
            ScriptList boundArguments = new ScriptList();
            if (!method.IsStatic) boundArguments.Add(Arguments.Arg("object"));
            foreach (var parameter in method.GetParameters())
            {
                if (parameter.IsOptional)
                    boundArguments.Add(Arguments.Optional(parameter.Name, parameter.ParameterType.Name));
                else
                    boundArguments.Add(Arguments.Arg(parameter.Name, parameter.ParameterType.Name));
            }
            return Function.MakeSystemFunction(method.Name,
                boundArguments,
                "Auto bound function",
                (context, arguments) =>
                {
                    var parameters = method.GetParameters();
                    int start = method.IsStatic ? 0 : 1;
                    var args = new object[arguments.Count - start];
                    for (int i = 0; i < arguments.Count - start; ++i) 
                        args[i] = Convert.ChangeType(arguments[i + start], parameters[i].ParameterType);
                    return method.Invoke(method.IsStatic ? null : arguments[0], args);
                });
        }

        public static ScriptObject LazyBindStaticMethod(System.Type type, String name)
        {
            return new GenericScriptObject(
                "@lazy-reflection", name,
                "@source-object", null,
                "@source-type", type);

        }

        public static ScriptObject GenerateTypeBinding(System.Type type)
        {
            var r = new GenericScriptObject();
            r.SetProperty("@construct", Function.MakeSystemFunction("@construct",
                Arguments.Args(Arguments.Optional(Arguments.Repeat("argument"))),
                "Create a new instance of " + type.Name,
                (context, arguments) =>
                {
                    return Activator.CreateInstance(type, (arguments[0] as ScriptList).ToArray());
                }));

            foreach (var method in type.GetMethods())
                if (method.IsPublic) r.SetProperty(method.Name, GenerateMethodBinding(method));
            
            return r;
        }

        public static String TransformMethodName(String str)
        {
            var r = "";
            for (int i = 0; i < str.Length; ++i)
            {
                if (str[i] >= 'A' && str[i] <= 'Z')
                {
                    if (i != 0 && (r[r.Length - 1] != '-')) r += "-";
                    r += Char.ToLowerInvariant(str[i]);
                }
                else if (str[i] == '_') r += "-";
                else r += str[i];
            }
            return r;
        }

        public static ScriptObject GenerateLazyBindingObjectForStaticLibrary(System.Type type)
        {
            var r = new GenericScriptObject();
            foreach (var method in type.GetMethods())
                if (method.IsPublic && method.IsStatic)
                    r.SetProperty(TransformMethodName(method.Name), LazyBindStaticMethod(type, method.Name));
            return r;
        }   

        private static ScriptObject findNamespaceObject(GenericScriptObject on, String @namespace)
        {
            var pieces = @namespace.Split(new char[] { '.' });
            foreach (var piece in pieces)
            {
                if (on.GetProperty(piece) == null) on.SetProperty(piece, new GenericScriptObject());
                on = on.GetProperty(piece) as GenericScriptObject;
            }
            return on;
        }

        public static ScriptObject GenerateAssemblyBinding(System.Reflection.Assembly assembly)
        {
            var r = new GenericScriptObject();
            foreach (var type in assembly.GetTypes())
                if (type.IsPublic) findNamespaceObject(r, type.Namespace).SetProperty(type.Name, GenerateTypeBinding(type));
            return r;
        }


    }
}
