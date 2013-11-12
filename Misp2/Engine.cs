using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        public static string VERSION = "MISP 0.2a";

        internal Dictionary<String, ScriptObject> functions = new Dictionary<String, ScriptObject>();
        internal Dictionary<String, Func<Context, Object>> specialVariables
            = new Dictionary<string, Func<Context, object>>();
        
        public static T ArgumentType<T>(Object obj) where T : class
        {
            return obj as T;
        }
        
        public Engine()
        {
            this.SetupStandardLibrary();
        }

        public ScriptObject AddFunction(String name, String comment, Func<Context, ScriptList, Object> func, 
            params ScriptObject[] arguments)
        {
            var r = Function.MakeSystemFunction(name, new ScriptList(arguments), comment, func);
            functions.Add(name, r);
            return r;
        }

        //public Function MakeLambda(String name, String comment, Func<Context, ScriptList, Object> func,
        //    params String[] arguments)
        //{
        //    return Function2.MakeSystemFunction(name, ArgumentInfo.ParseArguments(this, arguments), comment, func);
        //}

        public void AddGlobalVariable(String name, Func<Context, Object> getFunc)
        {
            specialVariables.Add(name, getFunc);
        }
    }
}
