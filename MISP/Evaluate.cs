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

        private void evaluateNodeChild(bool callFunction, Object child, ScriptList arguments, Context context)
        {
            bool lazyArgument = false;
            var prefix = (child as ScriptObject).gsp("@prefix");

            if (callFunction && arguments.Count > 0 && (Function.IsFunction(arguments[0] as ScriptObject)))
            {
                var argumentInfo = Function.GetArgumentInfo(arguments[0] as ScriptObject, context, arguments.Count - 1);
                if (context.evaluationState != EvaluationState.Normal) return;
                lazyArgument = argumentInfo["@lazy"] != null;
            }

            if (lazyArgument && prefix != ":" && prefix != "#")
                arguments.Add(child);
            else
            {
                var argument = Evaluate(context, child);
                if (context.evaluationState != EvaluationState.Normal) return;
                if (prefix == "$" && argument is ScriptList)
                    arguments.AddRange(argument as ScriptList);
                else
                    arguments.Add(argument);
            }
        }

        public Object Evaluate(
            Context context,
            Object what,
            bool ignoreStar = false,
            bool discardResults = false)
        {
            if (context.evaluationState != EvaluationState.Normal)
                throw new ScriptError("Invalid Context", null);
            if (context.callDepth >= context.maximumCallDepth)
            {
                context.RaiseNewError("Overflow.", null);
                return null;
            }

            context.callDepth += 1; //All exit points must decrement depth.

            if (what is String)
            {
                var r = EvaluateString(context, what as String, "", discardResults);
                context.callDepth -= 1;
                return r;
            }
            else if (!(what is ScriptObject))
            {
                context.callDepth -= 1;
                return what;
            }

            var node = what as ScriptObject;
            context.currentNode = node;

            if (context.limitExecutionTime && (DateTime.Now - context.executionStart > context.allowedExecutionTime))
            {
                context.RaiseNewError("Timeout.", node);
                context.callDepth -= 1;
                return null;
            }

            if (node.gsp("@prefix") == "*" && !ignoreStar) //Object is a quoted node
            { 
                context.callDepth -= 1; 
                return node; 
            }
            
            var type = node.gsp("@type");
            if (String.IsNullOrEmpty(type)) 
            { 
                context.callDepth -= 1; 
                return node; 
            } //Object is not evaluatable code.

            object result = null;

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
                        if ((piece as ScriptObject).gsp("@type") == "string")
                            continue;
                        else
                        {
                            Evaluate(context, piece);
                            if (context.evaluationState != EvaluationState.Normal)
                            {
                                context.callDepth -= 1;
                                return null;
                            }
                        }
                    }
                    result = null;
                }
                else
                {
                    if (node._children.Count == 1) //If there's only a single item, the result is that item.
                    {
                        result = Evaluate(context, node._child(0));
                        if (context.evaluationState != EvaluationState.Normal)
                        {
                            context.callDepth -= 1;
                            return null;
                        }
                    }
                    else
                    {
                        var resultString = String.Empty;
                        foreach (var piece in node._children)
                        {
                            resultString += ScriptObject.AsString(Evaluate(context, piece));
                            if (context.evaluationState != EvaluationState.Normal)
                            { 
                                context.callDepth -= 1; 
                                return null; 
                            }
                        }
                        result = resultString;
                    }
                }
            }
            else if (type == "token")
            {
                result = LookupToken(context, node.gsp("@token"));
                if (context.evaluationState != EvaluationState.Normal)
                {
                    context.callDepth -= 1;
                    return null;
                }
            }
            else if (type == "memberaccess")
            {
                var lhs = Evaluate(context, node._child(0));
                if (context.evaluationState != EvaluationState.Normal)
                { context.callDepth -= 1; return null; }
                String rhs = "";

                if ((node._child(1) as ScriptObject).gsp("@type") == "token")
                    rhs = (node._child(1) as ScriptObject).gsp("@token");
                else
                    rhs = ScriptObject.AsString(Evaluate(context, node._child(1), false));
                if (context.evaluationState != EvaluationState.Normal) { context.callDepth -= 1; return null; }

                if (lhs == null) result = null;
                else if (lhs is ScriptObject)
                {
                    result = (lhs as ScriptObject).GetProperty(ScriptObject.AsString(rhs));
                    if (node.gsp("@token") == ":")
                    {
                        context.Scope.PushVariable("this", lhs);
                        result = Evaluate(context, result, true, false);
                        context.Scope.PopVariable("this");
                        if (context.evaluationState != EvaluationState.Normal)
                        { context.callDepth -= 1; return null; }
                    }
                }
                else
                {
                    var field = lhs.GetType().GetField(ScriptObject.AsString(rhs));
                    if (field != null)
                        result = field.GetValue(lhs);
                    else
                    {
                        var prop = lhs.GetType().GetProperty(ScriptObject.AsString(rhs));
                        if (prop != null)
                            result = prop.GetValue(lhs, null);
                        else
                        {
                            var members = lhs.GetType().FindMembers(System.Reflection.MemberTypes.Method,
                                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                                new System.Reflection.MemberFilter((minfo, obj) => { return minfo.Name == obj.ToString(); }),
                                ScriptObject.AsString(rhs));
                            if (members.Length != 0)
                            {
                                result = new GenericScriptObject(
                                    "@lazy-reflection", ScriptObject.AsString(rhs),
                                    "@source-object", lhs,
                                    "@source-type", lhs.GetType());
                            }
                            else
                                result = null;
                        }
                    }
                }
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
                        evaluateNodeChild(eval, child, arguments, context);
                        if (context.evaluationState != EvaluationState.Normal)
                        {
                            context.callDepth -= 1;
                            return null;
                        }
                    }

                    if (node.gsp("@prefix") == "^") result = arguments;
                    else
                    {
                        if (arguments.Count > 0 && Function.IsFunction(arguments[0] as ScriptObject))
                        {

                            result = Function.Invoke((arguments[0] as ScriptObject), this, context,
                                new ScriptList(arguments.GetRange(1, arguments.Count - 1)));
                            if (context.evaluationState != EvaluationState.Normal)
                            {
                                if (context.evaluationState == EvaluationState.UnwindingError)
                                    context.PushStackTrace((arguments[0] as ScriptObject).gsp("@name"));
                                context.callDepth -= 1; 
                                return null;
                            }

                        }
                        else if (arguments.Count > 0 &&
                            arguments[0] is ScriptObject &&
                            (arguments[0] as ScriptObject).GetProperty("@lazy-reflection") != null)
                        {
                            var sObj = arguments[0] as ScriptObject;
                            var argumentTypes = arguments.GetRange(1, arguments.Count - 1).Select(
                                (obj) => obj.GetType()).ToArray();
                            var sourceObject = sObj.GetProperty("@source-object");
                            var method = (sObj.GetProperty("@source-type") as System.Type)
                                .GetMethod(sObj.gsp("@lazy-reflection"), argumentTypes);
                            if (method == null) 
                                throw new ScriptError("Could not find overload for " +
                                    sObj.gsp("@lazy-reflection") + " that takes argument types " +
                                    String.Join(", ", argumentTypes.Select((t) => t.Name)) + " on type " +
                                    sObj.GetProperty("@source-type").ToString(), what as ScriptObject);
                            result = method.Invoke(sourceObject, arguments.GetRange(1, arguments.Count - 1).ToArray());
                        }
                        else if (arguments.Count > 0)
                            result = arguments[0];
                        else
                            result = null;
                    }
                }
            }
            else if (type == "root")
            {
                var results = new ScriptList();
                foreach (var child in node._children)
                {
                    results.Add(Evaluate(context, child, false, false));
                    if (context.evaluationState != EvaluationState.Normal)
                        {
                            context.callDepth -= 1;
                            return null;
                        }
                    }
                return results;
            }
            else if (type == "number")
            {
                try
                {
                    if (node.gsp("@token").Contains('.')) result = Convert.ToSingle(node.gsp("@token"));
                    else
                    {
                        var numberString = node.gsp("@token");
                        if (numberString.StartsWith("0x"))
                        {
                            result = Convert.ToInt32(numberString.Substring(2), 16);
                        }
                        else if (numberString.StartsWith("0b"))
                        {
                            var accumulator = 0;
                            foreach (var c in numberString.Substring(2))
                            {
                                accumulator <<= 1;
                                if (c == '1') accumulator += 1;
                            }
                            result = accumulator;
                        }
                        else
                            result = Int32.Parse(numberString);
                    }
                }
                catch (Exception e)
                {
                    context.RaiseNewError("Number format error.", node);
                    { context.callDepth -= 1; return null; }
                }
            }
            else if (type == "char")
            {
                context.callDepth -= 1;
                return node.gsp("@token")[0];
            }
            else
            {
                context.RaiseNewError("Internal evaluator error.", node);
                { context.callDepth -= 1; return null; }
            }

            if (node.gsp("@prefix") == ":" && !ignoreStar)
                result = Evaluate(context, result);
            if (context.evaluationState != EvaluationState.Normal) { context.callDepth -= 1; return null; };
            if (node.gsp("@prefix") == ".") result = LookupToken(context, ScriptObject.AsString(result));
            context.callDepth -= 1;
            return result;
        }

        private object LookupToken(Context context, String value)
        {
            //value = value.ToLowerInvariant();
            if (specialVariables.ContainsKey(value)) return specialVariables[value](context);
            if (context.Scope.HasVariable(value)) return context.Scope.GetVariable(value);
            if (functions.ContainsKey(value)) return functions[value];
            if (value.StartsWith("@") && functions.ContainsKey(value.Substring(1))) return functions[value.Substring(1)];
            context.RaiseNewError("Could not find value with name " + value + " in this context.", context.currentNode);
            return null;
        }
    }
}
