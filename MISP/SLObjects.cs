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
                    var r = new GenericScriptObject();
                    foreach (var item in arguments[0] as ScriptList)
                    {
                        var list = item as ScriptList;
                        if (list == null || list.Count != 2) throw new ScriptError("Record expects only pairs as arguments.", context.currentNode);
                        r[ScriptObject.AsString(list[0])] = list[1];
                    }
                    return r;
                },
                Arguments.Mutator(Arguments.Repeat(Arguments.Optional("pairs")), "(@list value)"));

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
                    if (arguments[0] is ScriptObject)
                    {
                        try
                        {
                            (arguments[0] as ScriptObject)[ScriptObject.AsString(arguments[1])] = arguments[2];
                        }
                        catch (Exception e)
                        {
                            context.RaiseNewError("System Exception: " + e.Message, context.currentNode);
                        }
                    }
                    else
                    {
                        var field = arguments[0].GetType().GetField(ScriptObject.AsString(arguments[1]));
                        if (field != null) field.SetValue(arguments[0], arguments[2]);
                    }
                    return arguments[2];
                },
                Arguments.Arg("object"),
                Arguments.Mutator(Arguments.Lazy("name"), "(@identifier-if-token value)"),
                Arguments.Arg("value"));

            AddFunction("multi-set", "Set multiple members of an object.",
                (context, arguments) =>
                {
                    var obj = ArgumentType<ScriptObject>(arguments[0]);
                    var vars = ArgumentType<ScriptList>(arguments[1]);
                    foreach (var item in vars)
                    {
                        var l = ArgumentType<ScriptList>(item);
                        if (l == null || l.Count != 2) throw new ScriptError("Multi-set expects a list of pairs.", null);
                        obj.SetProperty(ScriptObject.AsString(l[0]), l[1]);
                    }
                    return obj;
                },
                Arguments.Arg("object"),
                Arguments.Mutator(Arguments.Arg("list"), "(@list value)"));

            AddFunction("delete", "Deletes a property from an object.",
                (context, arguments) =>
                {
                    var value = (arguments[0] as ScriptObject)[ScriptObject.AsString(arguments[1])];
                    (arguments[0] as ScriptObject).DeleteProperty(ScriptObject.AsString(arguments[1]));
                    return value;
                },
                    Arguments.Arg("object"),
                    Arguments.Arg("property-name"));
        }

    }
}
