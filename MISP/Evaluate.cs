using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class TimeoutError : ScriptError
    {
        public TimeoutError(ScriptObject generatedBy) : base("Execution timed out.", generatedBy) { }
    }

    public partial class Engine
    {
        public Object EvaluateString(Context context, String str, String fileName, bool discardResults = false)
        {
            try
            {
                var root = Parser.ParseRoot(str, fileName);
                return Evaluate(context, root, false, discardResults);
            }
            catch (Exception e)
            {
                context.RaiseNewError("System Exception: " + e.Message, null);
                return null;
            }
        }

        public Object Evaluate(
            Context context,
            Object what,
            bool ignoreStar = false,
            bool discardResults = false)
        {
            if (context.evaluationState != EvaluationState.Normal) throw new ScriptError("Invalid Context", null);

            if (what is String) return EvaluateString(context, what as String, "", discardResults);
            else if (!(what is ScriptObject)) return what;

            var node = what as ScriptObject;
            context.currentNode = node;

            if (context.limitExecutionTime && (DateTime.Now - context.executionStart > context.allowedExecutionTime))
            {
                context.RaiseNewError("Timeout.", node);
                return null;
            }

            if (node.gsp("@prefix") == "*" && !ignoreStar) return node;
            object result = null;

            var type = node.gsp("@type");
            if (String.IsNullOrEmpty(type)) return node; //Object is not evaluatable code.

            if (type == "string")
            {
                result = node["@token"];
            }
            else if (type == "stringexpression")
            {
                if (discardResults) //Don't bother assembling the string expression.
                    {
                        foreach (var piece in node._children)
                        {
                            if ( (piece as ScriptObject).gsp("@type") == "string")
                                continue;
                            else
                            {
                                Evaluate(context, piece);
                                if (context.evaluationState == EvaluationState.UnwindingError) return null;
                            }
                        }
                        result = null;
                    }
                    else
                    {
                        if (node._children.Count == 1) //If there's only a single item, the result is that item.
                        {
                            result = Evaluate(context, node._child(0));
                            if (context.evaluationState == EvaluationState.UnwindingError) return null;
                        }
                        else
                        {
                            var resultString = String.Empty;
                            foreach (var piece in node._children)
                            {
                                resultString += ScriptObject.AsString(Evaluate(context, piece));
                                if (context.evaluationState == EvaluationState.UnwindingError) return null;
                            }
                            result = resultString;
                        }
                    }
            }
                else if (type == "token")
                {
                    result = LookupToken(context, node.gsp("@token"));
                    if (context.evaluationState == EvaluationState.UnwindingError) return null;
                }
            else if (type == "memberaccess")
                    {
                        var lhs = Evaluate(context, node._child(0));
                        if (context.evaluationState == EvaluationState.UnwindingError) return null;
                        String rhs = "";

                        if ((node._child(1) as ScriptObject).gsp("@type") == "token")
                            rhs = (node._child(1) as ScriptObject).gsp("@token");
                        else
                            rhs = ScriptObject.AsString(Evaluate(context, node._child(1), false));
                        if (context.evaluationState == EvaluationState.UnwindingError) return null;

                        if (lhs == null) result = null;
                        else if (lhs is ScriptObject)
                        {
                            result = (lhs as ScriptObject).GetProperty(ScriptObject.AsString(rhs));
                            if (node.gsp("@token") == ":")
                            {
                                context.Scope.PushVariable("this", lhs);
                                result = Evaluate(context, result, true, false);
                                context.Scope.PopVariable("this");
                                if (context.evaluationState == EvaluationState.UnwindingError) return null;
                            }
                        }
                        else
                            result = null;
                    }
            else if (type == "node")
                    {
                        if (!ignoreStar && node.gsp("@prefix") == "*")
                        {
                            result = node;
                        }
                        else
                        {

                        bool eval = node.gsp("@prefix") != "^";

                        var arguments = new ScriptList();

                        foreach (var child in node._children)
                            {
                                bool argumentProcessed = false;

                                if (eval && arguments.Count > 0 && (Function.IsFunction(arguments[0] as ScriptObject))) 
                                    //This is a function call
                                {
                                    var prefix = (child as ScriptObject).gsp("@prefix");
                                    var func = arguments[0] as ScriptObject;
                                    var argumentInfo = Function.GetArgumentInfo(func, context, arguments.Count - 1);
                                    if (context.evaluationState == EvaluationState.UnwindingError) return null;
                                    if (argumentInfo.type.Typename == "CODE")
                                    {
                                        if (prefix == ":" || prefix == "#")
                                        {
                                            //Some prefixs override special behavior of code type.
                                            arguments.Add(Evaluate(context, child, true));
                                            if (context.evaluationState == EvaluationState.UnwindingError)
                                            {
                                                context.PushStackTrace("Arg for: " + func.gsp("@name"));
                                                return null;
                                            }
                                        }
                                        else if (prefix == "*" || String.IsNullOrEmpty(prefix))
                                            arguments.Add(child);
                                        else if (prefix == "^")
                                        {
                                            //raise warning
                                            arguments.Add(child);
                                        }
                                        else
                                        {
                                            context.RaiseNewError("Prefix invalid in this context.", child as ScriptObject);
                                            context.PushStackTrace("Arg for: " + func["@name"]);
                                            return null;
                                        }
                                        argumentProcessed = true;
                                    }
                                }

                                if (!argumentProcessed)
                                {
                                    var argument = Evaluate(context, child);
                                    if (context.evaluationState == EvaluationState.UnwindingError)
                                            {
                                                context.PushStackTrace("Arg for: " + 
                                                    ((arguments.Count > 0 && 
                                                        Function.IsFunction(arguments[0] as ScriptObject)) ? 
                                                            (arguments[0] as ScriptObject)["@name"] : "non-func"));
                                                return null;
                                            }
                                    if ((child as ScriptObject).gsp("@prefix") == "$" && argument is ScriptList)
                                        arguments.AddRange(argument as ScriptList);
                                    else
                                        arguments.Add(argument);
                                }
                            }
                        
                        if (node.gsp("@prefix") == "^") result = arguments;
                        else
                        {
                            if (arguments.Count > 0 && Function.IsFunction(arguments[0] as ScriptObject))
                            {
                               
                                    result = Function.Invoke((arguments[0] as ScriptObject), this, context,
                                        new ScriptList(arguments.GetRange(1, arguments.Count - 1)));
                                    if (context.evaluationState == EvaluationState.UnwindingError)
                                    {
                                        context.PushStackTrace((arguments[0] as ScriptObject).gsp("@name"));
                                        return null;
                                    }
                                
                            }
                            else if (arguments.Count > 0)
                                result = arguments[0];
                            else
                                result = null;
                        }
                    }
            }
            else if (type == "number")
            {
                    try
                    {
                        if (node.gsp("@token").Contains('.')) result = Convert.ToSingle(node.gsp("@token"));
                        else result = Convert.ToInt32(node.gsp("@token"));
                    }
                    catch (Exception e)
                    {
                        context.RaiseNewError("Number format error.", node);
                        return null;
                    }
            }
            else if (type == "dictionaryentry")
            {
                        var r = new ScriptList();
                        foreach (var child in node._children)
                            if ((child as ScriptObject).gsp("@type") == "token") r.Add((child as ScriptObject).gsp("@token"));
                            else
                            {
                                r.Add(Evaluate(context, child));
                                if (context.evaluationState == EvaluationState.UnwindingError) return null;
                            }
                        result = r;
                    }
            else
            {
                context.RaiseNewError("Internal evaluator error.", node);
                    return null;
            }

            if (node.gsp("@prefix") == ":" && !ignoreStar)
                result = Evaluate(context, result);
            if (context.evaluationState == EvaluationState.UnwindingError) return null;
            if (node.gsp("@prefix") == ".") result = LookupToken(context, ScriptObject.AsString(result));
            return result;
        }

        private object LookupToken(Context context, String value)
        {
            value = value.ToLowerInvariant();
            if (specialVariables.ContainsKey(value)) return specialVariables[value](context);
            if (context.Scope.HasVariable(value)) return context.Scope.GetVariable(value);
            if (functions.ContainsKey(value)) return functions[value];
            if (value.StartsWith("@") && functions.ContainsKey(value.Substring(1))) return functions[value.Substring(1)];
            context.RaiseNewError("Could not find value with name " + value + ".", context.currentNode);
            return null;
        }
    }
}
