using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ArgumentInfo
    {
        public String name;
        public bool optional;
        public bool repeat;
        public bool notNull;
        public Type type;

        public static List<ArgumentInfo> ParseArguments(Engine engine, params String[] args)
        {
            var list = new ScriptList();
            foreach (var arg in args) list.Add(arg);
            return ParseArguments(engine, list);
        }

        public static List<ArgumentInfo> ParseArguments(Engine engine, ScriptList args)
        {
            var r = new List<ArgumentInfo>();
            foreach (var arg in args)
            {
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

                var argInfo = new ArgumentInfo();

                if (!String.IsNullOrEmpty(typeDecl))
                {
                    if (engine.types.ContainsKey(typeDecl.ToUpperInvariant()))
                        argInfo.type = engine.types[typeDecl.ToUpperInvariant()];
                    else
                        throw new ScriptError("Unknown type " + typeDecl, null);
                }
                else
                    argInfo.type = Type.Anything;

                while (semanticDecl.StartsWith("?") || semanticDecl.StartsWith("+"))
                {
                    if (semanticDecl[0] == '?') argInfo.optional = true;
                    if (semanticDecl[0] == '+') argInfo.repeat = true;
                    semanticDecl = semanticDecl.Substring(1);
                }

                argInfo.name = semanticDecl;

                r.Add(argInfo);
            }

            return r;
        }
    }
}
