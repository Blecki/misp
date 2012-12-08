using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupBranchingFunctions()
        {
            Func<Context, ScriptList, Object> equalBody =
                (context, arguments) =>
                {
                    arguments = arguments[0] as ScriptList;
                    if (arguments.Count == 0) return null;

                    var nullCount = arguments.Count((o) => { return o == null; });
                    if (nullCount == arguments.Count) return true;
                    if (nullCount > 0) return null;

                    var firstType = arguments[0].GetType();
                    bool allSameType = true;
                    foreach (var argument in arguments)
                        if (argument.GetType() != firstType) allSameType = false;
                    if (!allSameType) return null;
                    for (int i = 1; i < arguments.Count; ++i)
                    {
                        if (firstType == typeof(String))
                        {
                            if (String.Compare(arguments[i] as String, arguments[i - 1] as String,
                                StringComparison.InvariantCultureIgnoreCase) != 0) return null;
                            //if (arguments[i] as String != arguments[i - 1] as String) return null;
                        }
                        else
                            if ((dynamic)arguments[i] != (dynamic)arguments[i - 1]) return null;
                    }
                    return true;
                };

            AddFunction("=", "<n> : True if all arguments equal, null otherwise.",
                equalBody,
                Arguments.Repeat("value"));

            AddFunction("!=", "<n> : Null if all arguments equal, true otherwise.",
                (context, arguments) => { return equalBody(context, arguments) == null ? (Object)true : null; },
                Arguments.Repeat("value"));

            AddFunction("&&", "<n> : True if all arguments are true.",
                (context, arguments) =>
                {
                    foreach (var arg in arguments[0] as ScriptList) if (arg == null) return null;
                    return true;
                }, Arguments.Repeat("value"));

            AddFunction("||", "<n> : True if any argument is true.",
               (context, arguments) =>
               {
                   foreach (var arg in arguments[0] as ScriptList) if (arg != null) return true;
                   return null;
               }, Arguments.Repeat("value"));

            AddFunction(">", "true if A > B",
                (context, arguments) => { return ((dynamic)arguments[0] > (dynamic)arguments[1]) ? (Object)true : null; },
                Arguments.Arg("A"), Arguments.Arg("B"));

            AddFunction(">=", "true if A >= B",
                (context, arguments) => { return ((dynamic)arguments[0] >= (dynamic)arguments[1]) ? (Object)true : null; },
                Arguments.Arg("A"), Arguments.Arg("B"));

            AddFunction("<", "true if A < B",
    (context, arguments) => { return ((dynamic)arguments[0] < (dynamic)arguments[1]) ? (Object)true : null; },
    Arguments.Arg("A"), Arguments.Arg("B"));

            AddFunction("<=", "true if A <= B",
    (context, arguments) => { return ((dynamic)arguments[0] <= (dynamic)arguments[1]) ? (Object)true : null; },
    Arguments.Arg("A"), Arguments.Arg("B"));

            AddFunction("!", "",
                (context, arguments) => { return (arguments[0] == null) ? (Object)true : null; },
                Arguments.Arg("value"));

            AddFunction("if", "",
                (context, arguments) =>
                {
                    return (arguments[0] == null) ?
                        Evaluate(context, arguments[2], true) :
                        Evaluate(context, arguments[1], true);
                },
                Arguments.Arg("condition"), Arguments.Lazy("then"), Arguments.Optional(Arguments.Lazy("else")));
        }
    }
}
