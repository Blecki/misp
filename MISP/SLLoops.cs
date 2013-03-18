using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupLoopFunctions()
        {
            AddFunction("map", "variable_name list code : Transform one list into another",
                (context, arguments) =>
                {
                    var vName = ArgumentType<String>(arguments[0]);
                    var list = ArgumentType<ScriptList>(arguments[1]);
                    var code = ArgumentType<ScriptObject>(arguments[2]);
                    var result = new ScriptList();
                    context.Scope.PushVariable(vName, null);
                    foreach (var item in list)
                    {
                        context.Scope.ChangeVariable(vName, item);
                        result.Add(Evaluate(context, code, true));
                    }
                    context.Scope.PopVariable(vName);
                    return result;
                },
                Arguments.Mutator(Arguments.Lazy("variable-name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Arg("in"), "(@list value)"),
                Arguments.Lazy("code"));

            AddFunction("mapi", "Like map, except the variable will hold the index.",
                (context, arguments) =>
                {
                    var vName = ArgumentType<String>(arguments[0]);
                    var list = ArgumentType<ScriptList>(arguments[1]);
                    var code = ArgumentType<ScriptObject>(arguments[2]);
                    var result = new ScriptList();
                    context.Scope.PushVariable(vName, null);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        context.Scope.ChangeVariable(vName, i);
                        result.Add(Evaluate(context, code, true));
                    }
                    context.Scope.PopVariable(vName);
                    return result;
                },
                Arguments.Mutator(Arguments.Lazy("variable-name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Arg("in"), "(@list value)"),
                Arguments.Lazy("code"));


            AddFunction("mapex",
                "variable_name start code next : Like map, but the next element is the result of 'next'. Stops when next = null.",
                (context, arguments) =>
                {
                    var vName = ArgumentType<String>(arguments[0]);
                    var code = ArgumentType<ScriptObject>(arguments[2]);
                    var next = ArgumentType<ScriptObject>(arguments[3]);
                    var result = new ScriptList();
                    var item = arguments[1];
                    context.Scope.PushVariable(vName, null);

                    while (item != null)
                    {
                        context.Scope.ChangeVariable(vName, item);
                        result.Add(Evaluate(context, code, true));
                        item = Evaluate(context, next, true);
                    }

                    context.Scope.PopVariable(vName);
                    return result;
                },
                Arguments.Mutator(Arguments.Lazy("variable-name"), "(@identifier value)"),
                Arguments.Arg("start"),
                Arguments.Lazy("code"),
                Arguments.Lazy("next"));

            AddFunction("for",
                "variable_name list code : Execute code for each item in list. Returns result of last run of code.",
                (context, arguments) =>
                {
                    var vName = ArgumentType<String>(arguments[0]);
                    var list = ArgumentType<ScriptList>(arguments[1]);
                    var func = ArgumentType<ScriptObject>(arguments[2]);
                    context.Scope.PushVariable(vName, null);
                    Object result = null;
                    foreach (var item in list)
                    {
                        context.Scope.ChangeVariable(vName, item);
                        result = Evaluate(context, func, true);
                    }

                    context.Scope.PopVariable(vName);

                    return result;
                },
                Arguments.Mutator(Arguments.Lazy("variable-name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Arg("list"), "(@list value)"),
                Arguments.Lazy("code"));

            AddFunction("while",
                "condition code : Repeat code while condition evaluates to true.",
                (context, arguments) =>
                {
                    var cond = ArgumentType<ScriptObject>(arguments[0]);
                    var code = ArgumentType<ScriptObject>(arguments[1]);

                    while (context.evaluationState == EvaluationState.Normal && Evaluate(context, cond, true) != null)
                        if (context.evaluationState == EvaluationState.Normal) Evaluate(context, code, true);
                    return null;
                },
                Arguments.Lazy("condition"),
                Arguments.Lazy("code"));

        }
    }
}
