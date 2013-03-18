using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Arguments
    {
        //public static ScriptList ParseArguments(Engine engine, params String[] args)
        //{
        //    var list = new ScriptList();
        //    foreach (var arg in args) list.Add(arg);
        //    return ParseArguments(engine, list);
        //}

        public static ScriptObject Arg(String name, String help = null)
        {
            return new GenericScriptObject(
                "@name", name,
                "@help", help
            );
        }

        public static ScriptObject Optional(String name, String help = null)
        {
            return new GenericScriptObject(
                "@name", name,
                "@optional", true,
                "@help", help
                );
        }

        public static ScriptObject Optional(ScriptObject arg)
        {
            arg["@optional"] = true;
            return arg;
        }

        public static ScriptObject Repeat(String name, String help = null)
        {
            return new GenericScriptObject(
                "@name", name,
                "@repeat", true,
                "@help", help
                );
        }

        public static ScriptObject Repeat(ScriptObject arg)
        {
            arg["@repeat"] = true;
            return arg;
        }

        public static ScriptObject Lazy(String name, String help = null)
        {
            return new GenericScriptObject(
                "@name", name,
                "@lazy", true,
                "@help", help
                );
        }

        public static ScriptObject Lazy(ScriptObject arg)
        {
            arg["@lazy"] = true;
            return arg;
        }

        public static ScriptList Args(params Object[] objs)
        {
            var r = new ScriptList();
            foreach (var obj in objs)
            {
                if (obj is ScriptObject) r.Add(obj);
                else if (obj is String) r.Add(Arg(obj.ToString()));
                else throw new InvalidProgramException();
            }
            return r;
        }

        public static ScriptObject Mutator(ScriptObject arg, String code)
        {
            arg["@mutator"] = Parser.ParseRoot(code, "");
            return arg;
        }

        /*
        public static ScriptList ParseArguments(Engine engine, ScriptList args)
        {
            var r = new ScriptList();
            foreach (var arg in args)
            {
                var info = new GenericScriptObject();

                if (!(arg is String)) throw new ScriptError("Argument names must be strings.", null);
                var parts = (arg as String).Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                String typeDecl = "";
                String semanticDecl = "";
                if (parts.Length == 2)
                {
                    typeDecl = parts[0];
                    semanticDecl = parts[1];
                }
                else if (parts.Length == 1)
                {
                    semanticDecl = parts[0];
                }
                else
                    throw new ScriptError("Invalid argument declaration", null);

                //var argInfo = new ArgumentInfo();

                if (!String.IsNullOrEmpty(typeDecl))
                {
                    if (engine.types.ContainsKey(typeDecl.ToUpperInvariant()))
                        info["@type"] = engine.types[typeDecl.ToUpperInvariant()];
                    //argInfo.type = engine.types[typeDecl.ToUpperInvariant()];
                    else
                        throw new ScriptError("Unknown type " + typeDecl, null);
                }
                else
                    info["@type"] = Type.Anything;
                    //argInfo.type = Type.Anything;

                while (semanticDecl.StartsWith("?") || semanticDecl.StartsWith("+"))
                {
                    if (semanticDecl[0] == '?') info["@optional"] = true; // argInfo.optional = true;
                    if (semanticDecl[0] == '+') info["@repeat"] = true; // argInfo.repeat = true;
                    semanticDecl = semanticDecl.Substring(1);
                }

                info["@name"] = semanticDecl;
                //argInfo.name = semanticDecl;

                r.Add(info);//argInfo);
            }

            return r;
        }
         * */
    }
}
