using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupListFunctions()
        {
            AddFunction("length", "list : Returns length of list.",
                (context, arguments) =>
                {
                    var list = arguments[0] as ScriptList;
                    return list == null ? 0 : list.Count;
                }, Arguments.Arg("list"));

            AddFunction("count", "variable_name list code : Returns number of items in list for which code evaluated to true.",
                (context, arguments) =>
                {
                    var vName = ArgumentType<String>(arguments[0]);
                    var list = ArgumentType<ScriptList>(arguments[1]);
                    var func = ArgumentType<ScriptObject>(arguments[2]);

                    context.Scope.PushVariable(vName, null);
                    var result = (int)list.Count((o) =>
                    {
                        context.Scope.ChangeVariable(vName, o);

                        return Evaluate(context, func, true) != null;
                    });
                    context.Scope.PopVariable(vName);
                    return result;
                },
                Arguments.Mutator(Arguments.Lazy("variable-name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Arg("in"), "(@list value)"),
                Arguments.Lazy("code"));

            AddFunction("where", "variable_name list code : Returns new list containing only the items in list for which code evaluated to true.",
                (context, arguments) =>
                {
                    var vName = ArgumentType<String>(arguments[0]);
                    var list = ArgumentType<ScriptList>(arguments[1]);
                    var func = ArgumentType<ScriptObject>(arguments[2]);

                    context.Scope.PushVariable(vName, null);
                    var result = new ScriptList(list.Where((o) =>
                    {
                        context.Scope.ChangeVariable(vName, o);
                        return Evaluate(context, func, true) != null;
                    }));
                    context.Scope.PopVariable(vName);
                    return result;
                },
                Arguments.Mutator(Arguments.Lazy("variable-name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Arg("in"), "(@list value)"),
                Arguments.Lazy("code"));

            AddFunction("cat", "<n> : Combine N lists into one",
                (context, arguments) =>
                {
                    var result = new ScriptList();
                    foreach (var arg in arguments[0] as ScriptList)
                    {
                        if (arg is ScriptList) result.AddRange(arg as ScriptList);
                        else result.Add(arg);
                    }
                    return result;
                }, Arguments.Repeat(Arguments.Optional("item")));

            AddFunction("last",
                "list : Returns last item in list.",
                (context, arguments) =>
                {
                    return ArgumentType<ScriptList>(arguments[0]).LastOrDefault();
                },
                Arguments.Mutator(Arguments.Arg("list"), "(@list value)"));

            AddFunction("first",
                "list : Returns first item in list.",
                (context, arguments) =>
                {
                    if (arguments[2] == null)
                        return ArgumentType<ScriptList>(arguments[0]).FirstOrDefault();
                    else
                    {
                        var vName = ArgumentType<String>(arguments[1]);
                        var list = ArgumentType<ScriptList>(arguments[0]);
                        var func = ArgumentType<ScriptObject>(arguments[2]);

                        context.Scope.PushVariable(vName, null);
                        var result = list.FirstOrDefault((o) => {
                            context.Scope.ChangeVariable(vName, o);
                            return Evaluate(context, func, true) != null;
                        });
                        context.Scope.PopVariable(vName);
                        return result;
                    }
                },
                Arguments.Mutator(Arguments.Arg("list"), "(@list value)"),
                Arguments.Optional(Arguments.Mutator(Arguments.Lazy("variable-name"), "(@identifier value)")),
                Arguments.Optional(Arguments.Lazy("code")));

            AddFunction("index",
                "list n : Returns nth element in list.",
                (context, arguments) =>
                {
                    var index = arguments[1] as int?;
                    if (index == null || !index.HasValue) return null;
                    if (index.Value < 0) return null;

                    var list = ArgumentType<ScriptList>(arguments[0]);
                    if (index.Value >= list.Count) return null;
                    return list[index.Value];
                },
                Arguments.Mutator(Arguments.Arg("list"), "(@list value)"),
                Arguments.Arg("n"));


            //functions.Add("sub-list", Function.MakeSystemFunction("sub-list",
            //    Arguments.ParseArguments(this, "list list", "integer start", "integer ?length"),
            //    "list start length: Returns a elements in list between start and start+length.",
            //    (context, arguments) =>
            //    {
            //        var list = ArgumentType<ScriptList>(arguments[0]);
            //        var start = arguments[1] as int?;
            //        if (start == null || !start.HasValue) return new ScriptList();
            //        int? length = arguments[2] as int?;
            //        if (length == null || !length.HasValue) length = list.Count;

            //        if (start.Value < 0) { length -= start; start = 0; }
            //        if (start.Value >= list.Count) return new ScriptList();
            //        if (length.Value <= 0) return new ScriptList();
            //        if (length.Value + start.Value >= list.Count) length = list.Count - start.Value;

            //        return new ScriptList(list.GetRange(start.Value, length.Value));
            //    }));

            //functions.Add("sort", Function.MakeSystemFunction("sort",
            //    Arguments.ParseArguments(this, "string variable_name", "list in", "code code"),
            //    "vname list sort_func: Sorts elements according to sort func; sort func returns integer used to order items.",
            //    (context, arguments) =>
            //    {
            //        var vName = ScriptObject.AsString(arguments[0]);
            //        var list = ArgumentType<ScriptList>(arguments[1]);
            //        var sortFunc = ArgumentType<ScriptObject>(arguments[2]);

            //        var comparer = new ListSortComparer(this, vName, sortFunc, context);
            //        list.Sort(comparer);
            //        return list;
            //    }));

            //functions.Add("reverse", Function.MakeSystemFunction("reverse",
            //    Arguments.ParseArguments(this, "list list"),
            //    "list: Reverse the list.",
            //    (context, arguments) =>
            //    {
            //        var list = ArgumentType<ScriptList>(arguments[0]);
            //        list.Reverse();
            //        return list;
            //    }));

            AddFunction("array", "Create a list of items by running code N times",
                (context, arguments) =>
                {
                    var count = arguments[0] as int?;
                    if (count == null || !count.HasValue) return null;
                    var result = new ScriptList();
                    for (int i = 0; i < count; ++i)
                        result.Add(Evaluate(context, arguments[1], true));
                    return result;
                },
                Arguments.Arg("count"),
                Arguments.Lazy("code"));
        }

        private class ListSortComparer : IComparer<Object>
        {
            Engine evaluater;
            Context context;
            ScriptObject func;
            String vName;

            internal ListSortComparer(Engine evaluater, String vName, ScriptObject func, Context context)
            {
                this.evaluater = evaluater;
                this.vName = vName;
                this.func = func;
                this.context = context;
            }

            private int rank(Object o)
            {
                context.Scope.PushVariable(vName, o);
                var r = evaluater.Evaluate(context, func, true) as int?;
                context.Scope.PopVariable(vName);
                if (r != null && r.HasValue) return r.Value;
                return 0;
            }

            public int Compare(object x, object y)
            {
                return rank(x) - rank(y);
            }
        }
    }
}
