using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupObjectFunctions()
        {
            AddFunction("members", "Lists all members of an object",
                (context, arguments) =>
                {
                    var obj = ArgumentType<ScriptObject>(arguments[0]);
                    return obj.ListProperties();
                },
                    Arguments.Arg("object"));

            AddFunction("record", "Create a new record.",
                (context, arguments) =>
                {
                    var obj = new GenericScriptObject();
                    var vars = AutoBind.ListArgument(arguments[0]);
                    foreach (var item in vars)
                    {
                        var l = ArgumentType<ScriptObject>(item);
                        if (l == null || l._children.Count != 2) 
                            throw new ScriptError("Record expects a list of pairs.", null);
                        var arg = l._child(0) as ScriptObject;
                        string mname = "";
                        if (arg != null && arg.gsp("@type") == "token") mname = arg.gsp("@token");
                        else mname = Evaluate(context, arg, true).ToString();
                        obj.SetProperty(mname, Evaluate(context, l._child(1)));
                    }
                    return obj;
                },
                Arguments.Repeat(Arguments.Lazy("pairs")));

            AddFunction("clone", "Clone a record.",
                (context, arguments) =>
                {
                    var r = new GenericScriptObject(arguments[0] as ScriptObject);
                    foreach (var item in arguments[1] as ScriptList)
                    {
                        var list = item as ScriptList;
                        if (list == null || list.Count != 2) throw new ScriptError("Record expects only pairs as arguments.", context.currentNode);
                        r[ScriptObject.AsString(list[0])] = list[1];
                    }
                    return r;
                },
                Arguments.Arg("object"),
                Arguments.Mutator(Arguments.Repeat(Arguments.Optional("pairs")), "(@list value)"));

            AddFunction("set", "Set a member on an object.",
                (context, arguments) =>
                {
                    if (arguments[0] == null) return arguments[2];
                    return SetObjectProperty(context, arguments[0], ScriptObject.AsString(arguments[1]), arguments[2]);
                },
                Arguments.Arg("object"),
                Arguments.Mutator(Arguments.Lazy("name"), "(@identifier-if-token value)"),
                Arguments.Arg("value"));

            AddFunction("multi-set", "Set multiple members of an object.",
                (context, arguments) =>
                {
                    var obj = ArgumentType<ScriptObject>(arguments[0]);
                    var vars = AutoBind.ListArgument(arguments[1]);
                    foreach (var item in vars)
                    {
                        var l = ArgumentType<ScriptObject>(item);
                        if (l == null || l._children.Count != 2) throw new ScriptError("Multi-set expects a list of pairs.", null);
                        var arg = l._child(0) as ScriptObject;
                        string mname = "";
                        if (arg != null && arg.gsp("@type") == "token") mname = arg.gsp("@token");
                        else mname = Evaluate(context, arg, true).ToString();
                        SetObjectProperty(context, obj, mname, Evaluate(context, l._child(1)));
                    }
                    return obj;
                },
                Arguments.Arg("object"),
                Arguments.Repeat(Arguments.Lazy("pairs")));

            AddFunction("delete", "Deletes a property from an object.",
                (context, arguments) =>
                {
                    var value = (arguments[0] as ScriptObject)[ScriptObject.AsString(arguments[1])];
                    if (arguments[0] is Scope)
                        (arguments[0] as Scope).PopVariable(ScriptObject.AsString(arguments[1]));
                    else
                        (arguments[0] as ScriptObject).DeleteProperty(ScriptObject.AsString(arguments[1]));
                    return value;
                },
                    Arguments.Arg("object"),
                    Arguments.Arg("property-name"));
        }

        private static object SetObjectProperty(Context context, Object obj, String name, Object value)
        {
            if (obj is ScriptObject)
            {
                try
                {
                    (obj as ScriptObject)[name] = value;
                }
                catch (Exception e)
                {
                    context.RaiseNewError("System Exception: " + e.Message, context.currentNode);
                }
            }
            else
            {
                var field = obj.GetType().GetField(ScriptObject.AsString(name));
                if (field != null) field.SetValue(obj, value);
                else
                {
                    var prop = obj.GetType().GetProperty(ScriptObject.AsString(name));
                    if (prop != null) prop.SetValue(obj, value, null);
                    else
                        context.RaiseNewError("Could not find member with name '" + ScriptObject.AsString(name) + "' on object" +
                            " of type '" + obj.GetType().ToString() + "'", context.currentNode);
                }
            }
            return value;
        }

    }
}
