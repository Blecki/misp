using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Function
    {
        public static ScriptObject MakeSystemFunction(
            String name, 
            ScriptList arguments,
            String help,
            Func<Context, ScriptList, Object> func)
        {
            return new GenericScriptObject(
                "@name", name,
                "@arguments", arguments,
                "@help", help,
                "@function-body", func);
        }

public static ScriptObject MakeFunction(
            String name, 
            ScriptList arguments,
            String help,
            ScriptObject body,
            ScriptObject declarationScope)
        {
            return new GenericScriptObject(
                "@name", name,
                "@arguments", arguments,
                "@help", help,
                "@function-body", body,
                "@declaration-scope", declarationScope);
        }

        public static bool IsFunction(ScriptObject obj)
        {
            if (obj == null) return false;
            return obj["@function-body"] != null;
        }

        public static bool IsSystemFunction(ScriptObject obj)
        {
            if (obj == null) return false;
            return obj["@function-body"] is Func<Context, ScriptList, Object>;
        }

        public static ScriptObject GetArgumentInfo(ScriptObject func, Context context, int index)
        {
            var arguments = func["@arguments"] as ScriptList;
            if (arguments == null) return null;

            if (index >= arguments.Count)
            {
                if (arguments.Count > 0 && (arguments[arguments.Count - 1] as ScriptObject)["@repeat"] != null)
                    return arguments[arguments.Count - 1] as ScriptObject;
                else
                {
                    context.RaiseNewError("Argument out of bounds", null);
                    return null;
                }
            }
            else
                return arguments[index] as ScriptObject;
        }

        public static Object Invoke(ScriptObject func, Engine engine, Context context, ScriptList arguments)
        {
            var name = func.gsp("@name");
            var argumentInfo = func["@arguments"] as ScriptList;

            if (context.trace != null)
            {
                context.trace(new String('.', context.traceDepth) + "Entering " + name +"\n");
                context.traceDepth += 1;
            }

            var newArguments = new ScriptList();
            //Check argument types
            if (argumentInfo.Count == 0 && arguments.Count != 0)
            {
                context.RaiseNewError("Function expects no arguments.", context.currentNode);
                return null;
            }

                int argumentIndex = 0;
                for (int i = 0; i < argumentInfo.Count; ++i)
                {
                    var info = argumentInfo[i] as ScriptObject;

                    if (info["@repeat"] != null)
                    {
                        var list = new ScriptList();
                        while (argumentIndex < arguments.Count) //Handy side effect: If no argument is passed for an optional repeat
                        {                                       //argument, it will get an empty list.
                            list.Add((info["@type"] as Type).ProcessArgument(context, arguments[argumentIndex]));
                            if (context.evaluationState == EvaluationState.UnwindingError) return null;
                            ++argumentIndex;
                        }
                        newArguments.Add(list);
                    }
                    else
                    {
                        if (argumentIndex < arguments.Count)
                        {
                            newArguments.Add((info["@type"] as Type).ProcessArgument(context, arguments[argumentIndex]));
                            if (context.evaluationState == EvaluationState.UnwindingError) return null;
                        }
                        else if (info["@optional"] != null)
                            newArguments.Add((info["@type"] as Type).CreateDefault());
                        else
                        {
                            context.RaiseNewError("Not enough arguments to " + name, context.currentNode);
                            return null;
                        }
                        ++argumentIndex;
                    }
                }
                if (argumentIndex < arguments.Count)
                {
                    context.RaiseNewError("Too many arguments to " + name, context.currentNode);
                    return null;
                }
            

            Object r = null;
            
            if (func["@function-body"] is ScriptObject)
            {
                var declarationScope = func["@declaration-scope"];
                if (declarationScope is GenericScriptObject)
                {
                    var newScope = new Scope();
                    foreach (var valueName in (declarationScope as GenericScriptObject).properties)
                        newScope.PushVariable(valueName.Key, valueName.Value);
                    func["@declaration-scope"] = newScope;
                }

                context.PushScope(func["@declaration-scope"] as Scope);

                for (int i = 0; i < argumentInfo.Count; ++i)
                    context.Scope.PushVariable((argumentInfo[i] as ScriptObject).gsp("@name"), newArguments[i]);

                r = engine.Evaluate(context, func["@function-body"], true);

                for (int i = 0; i < argumentInfo.Count; ++i)
                    context.Scope.PopVariable((argumentInfo[i] as ScriptObject).gsp("@name"));

                context.PopScope();
            }
            else
            {
                try
                {
                    r = (func["@function-body"] as Func<Context, ScriptList, Object>)(context, newArguments);
                }
                catch (Exception e)
                {
                    context.RaiseNewError("System Exception: " + e.Message, context.currentNode);
                    return null;
                }
            }
            

            if (context.trace != null)
            {
                context.traceDepth -= 1;
                context.trace(new String('.', context.traceDepth) + "Leaving " + name +
                    (context.evaluationState == EvaluationState.UnwindingError ?
                    (" -Error: " + context.errorObject.GetLocalProperty("message").ToString()) :
                    "") + "\n");
            }

            return r;
        }
        
        

    }
}
