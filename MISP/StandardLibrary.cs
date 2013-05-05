using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        //Designed for emitting binding objects created by Autobind.
        public static String TightFormat(Object o)
        {
            var stream = new System.IO.StringWriter();
            if (o == null) stream.Write("null\n");
            else
            {
                var obj = o as ScriptObject;
                if (obj == null) stream.Write("Not a script object.\n");
                else foreach (var prop in obj.ListProperties())
                    {
                        var val = obj.GetProperty(prop.ToString());
                        stream.Write(prop.ToString() + ": ");
                        if (val == null) stream.Write("null");
                        else if (val is ScriptObject && Function.IsFunction(val as ScriptObject))
                            stream.Write((val as ScriptObject).GetProperty("@help"));
                        else if (val is ScriptObject && (val as ScriptObject).GetProperty("@lazy-reflection") != null)
                            stream.Write("lazy bind " + (val as ScriptObject).GetProperty("@lazy-reflection") + " on " +
                                ((val as ScriptObject).GetProperty("@source-type") as System.Type).Name);
                        else
                            stream.Write(val.ToString());
                        stream.Write("\n");
                    }
            }
            return stream.ToString();
        }

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
                (context, arguments) => 
                {
                    if (arguments[0] == null)
                        return Evaluate(context, arguments[1], true, false);
                    else
                        return arguments[0];
                },
                Arguments.Arg("A"), Arguments.Lazy("B"));

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

            AddFunction("break", "Aborts loops and supplies a return value.",
                (context, arguments) =>
                {
                    context.evaluationState = EvaluationState.UnwindingBreak;
                    context.breakObject = arguments[0];
                    return null;
                }, Arguments.Arg("value"));

            AddFunction("reflect", "Examine an object using .net reflection.",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    if (arguments[0] == null) stream.Write("null\n");
                    else
                    {
                        stream.Write(arguments[0].GetType().Name + "\n");
                        foreach (var field in arguments[0].GetType().GetFields())
                            stream.Write("field: " + field.Name + " " + field.FieldType.Name + "\n");
                        foreach (var method in arguments[0].GetType().GetMethods())
                            stream.Write("method: " + method.Name + " " + method.ReturnType.Name + "\n");
                    }
                    return stream.ToString();
                }, Arguments.Arg("object"));

            AddFunction("lazy-overloads", "Examine all possible overloads of a lazy binding.",
                (context, arguments) =>
                {
                    var binding = arguments[0] as ScriptObject;
                    var stream = new System.IO.StringWriter();
                    if (binding.GetProperty("@lazy-reflection") == null) stream.Write("Not a lazy binding object.");
                    else
                    {
                        var sourceType = binding.GetProperty("@source-type") as System.Type;
                        var methods = sourceType.GetMethods().Where((m) => m.Name == binding.gsp("@lazy-reflection"));
                        stream.Write("Methods found:\n");
                        foreach (var method in methods)
                        {
                            stream.Write("args: ");
                            stream.Write(String.Join(" ", method.GetParameters().Select((p) => p.ParameterType.Name)));
                            stream.Write("\n");
                        }
                    }
                    return stream.ToString();
                }, Arguments.Arg("object"));

            AddFunction("emitt", "Emit in tight-formatting style.",
                (context, arguments) =>
                {
                    return TightFormat(arguments[0]);
                }, Arguments.Arg("object", "Meant for use with objects generated via AutoBind."));

            AddFunction("emitf", "Emit a function",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    var obj = arguments[0] as ScriptObject;
                    stream.Write("Name: ");
                    stream.Write(obj.gsp("@name") + "\nHelp: " + obj.gsp("@help") + "\nArguments: \n");
                    foreach (var arg_ in obj["@arguments"] as ScriptList)
                    {
                        stream.Write("   ");
                        var arg = arg_ as ScriptObject;
                        //stream.Write((arg["@type"] as Type).Typename + " ");
                        if (arg["@optional"] != null) stream.Write("?");
                        if (arg["@repeat"] != null) stream.Write("+");
                        if (arg["@lazy"] != null) stream.Write("*");
                        stream.Write(arg["@name"] + "  ");
                        if (arg["@mutator"] != null) Engine.SerializeCode(stream, arg["@mutator"] as ScriptObject);
                        if (arg["@help"] != null) stream.Write(" - " + arg["@help"].ToString());
                        stream.Write("\n");
                    }
                    stream.Write("\nBody: ");
                    if (obj["@function-body"] is ScriptObject)
                        Engine.SerializeCode(stream, obj["@function-body"] as ScriptObject);
                    else
                        stream.Write("System");
                    stream.Write("\n");
                    return stream.ToString();
                },
                Arguments.Arg("func", "Must be a function."));

            AddFunction("serialize", "Serialize an object",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    SerializeObject(stream, arguments[0] as ScriptObject);
                    return stream.ToString();
                },
                    Arguments.Arg("object"));


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
            SetupRegexFunctions();
        }

    }
}
