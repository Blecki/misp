using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupStandardLibrary()
        {
            
            //types.Add("STRING", new TypeString());
            //types.Add("INTEGER", new TypePrimitive(typeof(int), true));
            //types.Add("LIST", new TypeList());
            //types.Add("OBJECT", new TypePrimitive(typeof(ScriptObject), false));
            //types.Add("CODE", new TypePrimitive(typeof(ScriptObject), false));
            //types.Add("IDENTIFIER", new TypeString());
            //types.Add("FUNCTION", new TypePrimitive(typeof(ScriptObject), false));
            //types.Add("ANYTHING", Type.Anything);
            //types.Add("FLOAT", new TypePrimitive(typeof(float), true));

            //foreach (var type in types)
            //    type.Value.Typename = type.Key;

            specialVariables.Add("null", (c) => { return null; });
            specialVariables.Add("functions", (c) => { return new ScriptList(functions.Select((pair) => { return pair.Value; })); });
            specialVariables.Add("true", (c) => { return true; });
            specialVariables.Add("false", (c) => { return null; });
            specialVariables.Add("@scope", (c) => { return c.Scope; });

            //AddFunction("net-module", "Loads a module from a .net assembly",
            //    (context, arguments) =>
            //    {
            //        NetModule.LoadModule(context, this, ScriptObject.AsString(arguments[0]), ScriptObject.AsString(arguments[1]));
            //        return null;
            //    }, "string assembly", "string module");

            AddFunction("@list", "If the argument is a list, great. If not, now it is.",
                (context, arguments) =>
                {
                    if (arguments[0] == null) return new ScriptList();
                    if (arguments[0] is ScriptList) return arguments[0];
                    return new ScriptList(arguments[0]);
                },
                    Arguments.Arg("value"));

            AddFunction("@lazy-list", "Mutates a lazy argument into a list. Effectively makes the ^ optional.",
                (context, arguments) =>
                {
                    var node = arguments[0] as ScriptObject;
                    if (node == null) return new ScriptList();
                    var r = new ScriptList();
                    foreach (var child in node._children)
                        r.Add(Evaluate(context, child));
                    return r;
                }, Arguments.Arg("arg"));

            AddFunction("@identifier", "Mutates a lazy argument. If it's a token, return as a string. If not, evaluate and return as a string.",
                (context, arguments) =>
                {
                    var arg = arguments[0] as ScriptObject;
                    if (arg != null && arg.gsp("@type") == "token") return arg.gsp("@token");
                    return ScriptObject.AsString(Evaluate(context, arg, true));
                }, Arguments.Arg("arg"));

            AddFunction("@identifier-if-token", "Mutates a lazy argument. If it's a token, return as a string. If not, evaluate and return.",
               (context, arguments) =>
               {
                   var arg = arguments[0] as ScriptObject;
                   if (arg != null && arg.gsp("@type") == "token") return arg.gsp("@token");
                   return Evaluate(context, arg, true);
               }, Arguments.Arg("arg"));

            AddFunction("arg", "create an argument", (context, arguments) =>
            {
                if (arguments[0] is ScriptObject)
                    return arguments[0];
                else return Arguments.Arg(ScriptObject.AsString(arguments[0]));
            }, Arguments.Mutator(Arguments.Lazy("name"), "(@identifier value)"));

            AddFunction("arg-lazy", "create a lazy argument", (context, arguments) =>
                {
                    if (arguments[0] is ScriptObject)
                    {
                        (arguments[0] as ScriptObject)["@lazy"] = true;
                        return arguments[0];
                    }
                    else return Arguments.Lazy(Arguments.Arg(ScriptObject.AsString(arguments[0])));
                }, Arguments.Mutator(Arguments.Lazy("name"), "(@identifier-if-token value)"));

            AddFunction("arg-optional", "create an optional argument", (context, arguments) =>
            {
                if (arguments[0] is ScriptObject)
                {
                    (arguments[0] as ScriptObject)["@optional"] = true;
                    return arguments[0];
                }
                else return Arguments.Optional(Arguments.Arg(ScriptObject.AsString(arguments[0])));
            }, Arguments.Mutator(Arguments.Lazy("name"), "(@identifier-if-token value)"));

            AddFunction("arg-repeat", "create a repeat argument", (context, arguments) =>
            {
                if (arguments[0] is ScriptObject)
                {
                    (arguments[0] as ScriptObject)["@repeat"] = true;
                    return arguments[0];
                }
                else return Arguments.Repeat(Arguments.Arg(ScriptObject.AsString(arguments[0])));
            }, Arguments.Mutator(Arguments.Lazy("name"), "(@identifier-if-token value)"));

            AddFunction("arg-mutator", "Add a mutator to an argument", (context, arguments) =>
                {
                    if (arguments[0] is ScriptObject)
                    {
                        (arguments[0] as ScriptObject)["@mutator"] = arguments[1];
                        return arguments[0];
                    }
                    else
                    {
                        var r = Arguments.Arg(ScriptObject.AsString(arguments[0]));
                        r["@mutator"] = arguments[1];
                        return r;
                    }
                }, Arguments.Mutator(Arguments.Lazy("name"), "(@identifier-if-token value)"), Arguments.Lazy("mutator"));

            AddFunction("eval", "Evaluates it's argument.",
                (context, arguments) =>
                {
                    return Evaluate(context, arguments[0], true);
                },
                    Arguments.Arg("arg"));

            AddFunction("lastarg", "Returns last argument",
                (context, arguments) =>
                {
                    return (arguments[0] as ScriptList).LastOrDefault();
                },
                    Arguments.Repeat("child"));

            AddFunction("nop", "Returns null.",
                (context, arguments) => { return null; },
                Arguments.Optional(Arguments.Repeat("value")));

            AddFunction("coalesce", "B if A is null, A otherwise.",
                (context, arguments) => { return (arguments[0] == null) ? arguments[1] : arguments[0]; },
                Arguments.Arg("A"), Arguments.Arg("B"));

            AddFunction("raise-error", "Raises a new error.",
                (context, arguments) =>
                {
                    context.RaiseNewError(MISP.ScriptObject.AsString(arguments[0]), context.currentNode);
                    return null;
                },
                Arguments.Arg("message"));

            AddFunction("catch-error", "Evaluate and return A unless error generated; if error Evaluate and return B.",
                (context, arguments) =>
                {
                    var result = Evaluate(context, arguments[0], true, false);
                    if (context.evaluationState == EvaluationState.UnwindingError)
                    {
                        context.evaluationState = EvaluationState.Normal;
                        return Evaluate(context, arguments[1], true, false);
                    }
                    return result;
                },
                    Arguments.Lazy("good"),
                    Arguments.Lazy("bad"));

            

            SetupVariableFunctions();
            SetupObjectFunctions();
            SetupMathFunctions();
            SetupFunctionFunctions();
            SetupBranchingFunctions();
            SetupLoopFunctions();
            SetupListFunctions();
            SetupStringFunctions();
            SetupEncryptionFunctions();
            SetupFileFunctions();
        }

    }
}
