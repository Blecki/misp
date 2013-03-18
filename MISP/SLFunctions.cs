using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        internal static Scope CopyScope(Scope scope)
        {
            var r = new Scope();
            foreach (var v in scope.variables)
                r.variables.Add(v.Key, new ScriptList(v.Value[v.Value.Count - 1]));
            return r;
        }

        private ScriptObject defunImple(Context context, ScriptList arguments, bool addToScope)
        {
            var functionName = ArgumentType<String>(arguments[0]);

            ScriptList argumentInfo = null;
            try
            {
                argumentInfo = ArgumentType<ScriptList>(arguments[1]);
            }
            catch (ScriptError e)
            {
                context.RaiseNewError(e.Message, context.currentNode);
                return null;
            }

            var functionBody = ArgumentType<ScriptObject>(arguments[2]);

            var newFunction = Function.MakeFunction(
                functionName,
                argumentInfo,
                "Script-defined function",
                functionBody,
                CopyScope(context.Scope));

            if (arguments[3] != null) newFunction["@help"] = ScriptObject.AsString(arguments[3]);

            if (addToScope) (newFunction["declaration-scope"] as Scope).PushVariable(functionName, newFunction);

            return newFunction;
        }

        private void SetupFunctionFunctions()
        {
            AddFunction("defun", "name arguments code",
                (context, arguments) =>
                {
                    var r = defunImple(context, arguments, false);
                    if (context.evaluationState == EvaluationState.Normal && !String.IsNullOrEmpty(r.gsp("@name")))
                        functions.Upsert(r.gsp("@name"), r);
                    return r;
                }, 
                Arguments.Mutator(Arguments.Lazy("name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Lazy("arguments"), "(@lazy-list value)"),
                Arguments.Lazy("code"),
                Arguments.Optional("comment"));

            AddFunction("lambda", "name arguments code",
                (context, arguments) =>
                {
                    return defunImple(context, arguments, false);
                },
                Arguments.Mutator(Arguments.Lazy("name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Lazy("arguments"), "(@lazy-list value)"),
                Arguments.Lazy("code"),
                Arguments.Optional("comment"));

            AddFunction("lfun", "Creates a local function. This is functionally equivilent to using 'let' to store a function in a local variable.",
                (context, arguments) =>
                {
                    var r = defunImple(context, arguments, true);
                    if (context.evaluationState == EvaluationState.Normal) context.Scope.PushVariable(r.gsp("@name"), r);
                    return r;
                },
                Arguments.Mutator(Arguments.Lazy("name"), "(@identifier value)"),
                Arguments.Mutator(Arguments.Lazy("arguments"), "(@lazy-list value)"),
                Arguments.Lazy("code"),
                Arguments.Optional("comment"));
        }

    }
}
