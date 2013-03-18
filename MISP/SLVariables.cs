using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    internal class LetVariable
    {
        public String name;
        public Object cleanupCode;
    }

    public partial class Engine
    {
        private void SetupVariableFunctions()
        {
            AddFunction("var",
                "name value : Assign value to a variable named [name].", (context, arguments) =>
                {
                    if (specialVariables.ContainsKey(ScriptObject.AsString(arguments[0])))
                        context.RaiseNewError("Can't assign to protected variable name.", context.currentNode);
                    else
                    {
                        try
                        {
                            context.Scope.ChangeVariable(ScriptObject.AsString(arguments[0]), arguments[1]);
                        }
                        catch (Exception e) { context.RaiseNewError(e.Message, context.currentNode); }
                    }
                    return arguments[1];
                },
                Arguments.Mutator(Arguments.Lazy("name"), "(@identifier value)"),
                Arguments.Arg("value"));

            AddFunction("let",
                "^( ^(\"name\" value ?cleanup-code) ^(...) ) code : Create temporary variables, run code. Optional clean-up code for each variable.",
                (context, arguments) =>
                {
                    var variables = ArgumentType<ScriptObject>(arguments[0]);
                    if (!String.IsNullOrEmpty(variables.gsp("@prefix"))) { /* Raise warning */ }

                    var code = ArgumentType<ScriptObject>(arguments[1]);
                    var cleanUp = new List<LetVariable>();
                    Object result = null;

                    foreach (var item in variables._children)
                    {
                        var sitem = item as ScriptObject;
                        if (sitem._children.Count <= 1 || sitem._children.Count > 3)
                        {
                            context.RaiseNewError("Bad variable definition in let", context.currentNode);
                            goto RUN_CLEANUP;
                        }

                        var nameObject = sitem._child(0) as ScriptObject;
                        var varName = "";

                        if (nameObject.gsp("@type") == "token")
                            varName = nameObject.gsp("@token");
                        else
                            varName = AutoBind.StringArgument(Evaluate(context, nameObject, true));
                        if (context.evaluationState == EvaluationState.UnwindingError) goto RUN_CLEANUP;

                        var varValue = Evaluate(context, sitem._child(1), false);
                        if (context.evaluationState == EvaluationState.UnwindingError) goto RUN_CLEANUP;

                        context.Scope.PushVariable(varName, varValue);
                        cleanUp.Add(new LetVariable
                        {
                            name = varName,
                            cleanupCode = sitem._children.Count == 3 ? sitem._child(2) : null
                        });
                    }

                    result = Evaluate(context, code, true);

                RUN_CLEANUP:
                    foreach (var item in cleanUp)
                    {
                        if (item.cleanupCode != null) Evaluate(context, item.cleanupCode, true, true);
                        context.Scope.PopVariable(item.name);
                    }

                    return result;
                },
                Arguments.Lazy("variables"),
                Arguments.Lazy("code"));
        }
    }
}
