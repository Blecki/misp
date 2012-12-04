using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Arguments
    {
        public static ScriptList ParseArguments(Engine engine, params String[] args)
        {
            var list = new ScriptList();
            foreach (var arg in args) list.Add(arg);
            return ParseArguments(engine, list);
        }

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
    }
}
