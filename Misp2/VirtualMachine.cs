using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class VirtualMachine
    {
        public static void Execute(ExecutionContext context)
        {
            //When an error represents bad output from the compiler or a built in function,
            //      an exception is thrown in C#. 
            //When an error represents faulty input code, an exception is thrown in MISP.

            if (context.ExecutionState != ExecutionState.Running) return;

            if (context.CodeContext.InstructionPointer >= context.CodeContext.Code.Count)
            {
                context.ExecutionState = ExecutionState.Finished;
                return;
            }

            var nextInstruction = context.CodeContext.Code[context.CodeContext.InstructionPointer] as Instruction?;
            context.CodeContext.InstructionPointer += 1;
            if (!nextInstruction.HasValue) throw new InvalidOperationException("Encountered non-code in instruction stream");
            var ins = nextInstruction.Value;

            switch (ins.Opcode)
            {
                case InstructionSet.YIELD:
                    return;

#region Moving things around
                case InstructionSet.MOVE:
                    {
                        var v = GetOperand(ins.FirstOperand, context);
                        SetOperand(ins.SecondOperand, v, context);
                    }
                    break;
#endregion

#region Lookup
                case InstructionSet.LOOKUP:
                    {
                        var name = GetOperand(ins.FirstOperand, context).ToString();
                        Object value = null;
                        if (context.Scope.HasVariable(name))
                            value = context.Scope.GetVariable(name);
                        else if (context.Environment.RuntimeContext.NativeFunctions.ContainsKey(name))
                            value = context.Environment.RuntimeContext.NativeFunctions[name];
                        else if (context.Environment.RuntimeContext.NativeSpecials.ContainsKey(name))
                            value = context.Environment.RuntimeContext.NativeSpecials[name]();
                        else
                        {
                            Throw(new InvalidOperationException("Could not resolve name '" + name + "'."), context);
                            break;
                        }
                        SetOperand(ins.SecondOperand, value, context);
                    }
                    break;
                case InstructionSet.LOOKUP_MEMBER:
                    {
                        var memberName = GetOperand(ins.FirstOperand, context).ToString();
                        var obj = GetOperand(ins.SecondOperand, context);
                        Object value = null;

                        if (obj == null)
                        {
                            Throw(new InvalidOperationException("Could not inspect members of NULL."), context);
                            break;
                        }

                        if (obj is ScriptObject) //Special handling of script objects.
                        {
                            var scriptObject = obj as ScriptObject;
                            if (scriptObject.HasProperty(memberName))
                                value = scriptObject.GetProperty(memberName);
                            else
                            {
                                Throw(new InvalidOperationException("Could not find member " + memberName + " on generic object."),
                                    context);
                                break;
                            }
                        }
                        else
                        {
                            var lookupResult = LookupMemberWithReflection(obj, memberName);
                            if (lookupResult.FoundMember == false)
                            {
                                Throw(new InvalidOperationException("Could not find member " + memberName + " on type " +
                                    obj.GetType().Name + "."), context);
                                break;
                            }
                            else
                                value = lookupResult.Member;
                        }

                        SetOperand(ins.ThirdOperand, value, context);
                    }
                    break;

                case InstructionSet.SET_MEMBER:
                    {
                        var value = GetOperand(ins.FirstOperand, context);
                        var memberName = GetOperand(ins.SecondOperand, context).ToString();
                        var obj = GetOperand(ins.ThirdOperand, context);


                        if (obj == null)
                        {
                            Throw(new InvalidOperationException("Could not set members of NULL."), context);
                            break;
                        }

                        if (obj is ScriptObject) //Special handling of script objects.
                        {
                            var scriptObject = obj as ScriptObject;
                            scriptObject.SetProperty(memberName, value);
                        }
                        else
                        {
                            var lookupResult = SetMemberWithReflection(obj, memberName, value);
                            if (lookupResult.FoundMember == false)
                            {
                                Throw(new InvalidOperationException("Could not find settable member " + memberName + " on type " +
                                    obj.GetType().Name + "."), context);
                                break;
                            }
                        }
                    }
                    break;

                case InstructionSet.RECORD:
                    SetOperand(ins.FirstOperand, new ScriptObject(), context);
                    break;
#endregion

#region Flow Control
                
                case InstructionSet.MARK:
                    {
                        var storedContext = context.CodeContext;
                        storedContext.InstructionPointer -= 1; //Rewind to the start of this instruction
                        SetOperand(ins.FirstOperand, storedContext, context);
                    }
                    break;
                case InstructionSet.BREAK:
                    {
                        var breakContext = GetOperand(ins.FirstOperand, context);
                        context.CodeContext = (breakContext as CodeContext?).Value;
                        Skip(context); //Skip the instruction stored by BEGIN_LOOP
                    }
                    break;
                case InstructionSet.SWAP_TOP:
                    {
                        var a = GetOperand(Operand.POP, context);
                        var b = GetOperand(Operand.POP, context);
                        SetOperand(Operand.PUSH, a, context);
                        SetOperand(Operand.PUSH, b, context);
                    }
                    break;
                case InstructionSet.BRANCH:
                    {
                        var storedContext = context.CodeContext;
                        storedContext.InstructionPointer -= 1; //Rewind to the start of this instruction
                        SetOperand(ins.FirstOperand, storedContext, context);
                    }
                    context.CodeContext = new CodeContext(GetOperand(ins.SecondOperand, context) as InstructionList, 0);
                    break;
                case InstructionSet.CONTINUE:
                    context.CodeContext = (GetOperand(ins.FirstOperand, context) as CodeContext?).Value;
                    break;
                case InstructionSet.CLEANUP:
                    {
                        var count = (GetOperand(ins.FirstOperand, context) as int?).Value;
                        while (count > 0)
                        {
                            context.Stack.Pop();
                            --count;
                        }
                    }
                    break;

                #endregion

#region Lambdas
                case InstructionSet.INVOKE:
                    {
                        var arguments = GetOperand(ins.FirstOperand, context) as List<Object>;
                        var function = arguments[0];
                        if (function is InvokeableFunction)
                        {
                            var InvokationResult = (function as InvokeableFunction).Invoke(context, arguments);
                            if (InvokationResult.InvokationSucceeded == false)
                            {
                                Throw(new InvalidOperationException(InvokationResult.ErrorMessage), context);
                                break;
                            }
                        }
                        else
                            Throw(new InvalidOperationException("Can't invoke what isn't invokeable."), context);
                    }
                    break;
                case InstructionSet.LAMBDA:
                    {
                        var arguments = GetOperand(ins.FirstOperand, context) as List<String>;
                        var code = GetOperand(ins.SecondOperand, context) as InstructionList;
                        var lambda = LambdaFunction.CreateLambda(context.Scope.Capture(), arguments, code);
                        SetOperand(ins.ThirdOperand, lambda, context);
                    }
                    break;
                case InstructionSet.PUSH_SCOPE:
                    {
                        var scope = GetOperand(ins.FirstOperand, context) as Scope;
                        context.PushScope(scope);
                    }
                    break;
                case InstructionSet.POP_SCOPE:
                    {
                        var scope = context.Scope;
                        context.PopScope();
                        SetOperand(ins.FirstOperand, scope, context);
                    }
                    break;
                case InstructionSet.PEEK_SCOPE:
                    {
                        var scope = context.Scope;
                        SetOperand(ins.FirstOperand, scope, context);
                    }
                    break;
                    
#endregion

#region Lists
                case InstructionSet.EMPTY_LIST:
                    SetOperand(ins.FirstOperand, new List<Object>(), context);
                    break;
                case InstructionSet.APPEND:
                    {
                        var v = GetOperand(ins.FirstOperand, context);
                        var l = GetOperand(ins.SecondOperand, context) as List<Object>;
                        l.Add(v);
                        SetOperand(ins.ThirdOperand, l, context);
                    }
                    break;
                case InstructionSet.APPEND_RANGE:
                    {
                        var v = GetOperand(ins.FirstOperand, context);
                        var l = GetOperand(ins.SecondOperand, context) as List<Object>;
                        l.AddRange(v as List<Object>);
                        SetOperand(ins.ThirdOperand, l, context);
                    }
                    break;

                case InstructionSet.PREPEND:
                    {
                        var v = GetOperand(ins.FirstOperand, context);
                        var l = GetOperand(ins.SecondOperand, context) as List<Object>;
                        l.Insert(0, v);
                        SetOperand(ins.ThirdOperand, l, context);
                    }
                    break;
                case InstructionSet.PREPEND_RANGE:
                    {
                        var v = GetOperand(ins.FirstOperand, context);
                        var l = GetOperand(ins.SecondOperand, context) as List<Object>;
                        l.InsertRange(0, v as List<Object>);
                        SetOperand(ins.ThirdOperand, l, context);
                    }
                    break;

                case InstructionSet.LENGTH:
                    {
                        var v = (GetOperand(ins.FirstOperand, context) as List<Object>).Count;
                        SetOperand(ins.SecondOperand, v, context);
                    }
                    break;

                case InstructionSet.INDEX:
                    {
                        var i = GetOperand(ins.FirstOperand, context) as int?;
                        var l = GetOperand(ins.SecondOperand, context) as List<Object>;
                        SetOperand(ins.ThirdOperand, l[i.Value], context);
                    }
                    break;
#endregion

#region Variables
                case InstructionSet.PUSH_VARIABLE:
                    {
                        var v = GetOperand(ins.FirstOperand, context);
                        var name = GetOperand(ins.SecondOperand, context).ToString();
                        context.Scope.PushVariable(name, v);
                    }
                    break;
                case InstructionSet.SET_VARIABLE:
                    {
                        var v = GetOperand(ins.FirstOperand, context);
                        var name = GetOperand(ins.SecondOperand, context).ToString();
                        context.Scope.ChangeVariable(name, v);
                    }
                    break;
                case InstructionSet.POP_VARIABLE:
                    {
                        var name = GetOperand(ins.FirstOperand, context).ToString();
                        var v = context.Scope.GetVariable(name);
                        context.Scope.PopVariable(name);
                        SetOperand(ins.SecondOperand, v, context);
                    }
                    break;
#endregion

#region Loop control
                case InstructionSet.DECREMENT:
                    {
                        var v = GetOperand(ins.FirstOperand, context) as int?;
                        SetOperand(ins.SecondOperand, v.Value - 1, context);
                    }
                    break;
                case InstructionSet.LESS:
                    {
                        var v0 = GetOperand(ins.FirstOperand, context) as int?;
                        var v1 = GetOperand(ins.SecondOperand, context) as int?;
                        SetOperand(ins.ThirdOperand, v0.Value < v1.Value, context);
                    }
                    break;
                case InstructionSet.IF_TRUE:
                    {
                        var b = GetOperand(ins.FirstOperand, context) as bool?;
                        if (!b.HasValue || !b.Value) Skip(context);
                    }
                    break;
                case InstructionSet.IF_FALSE:
                    {
                        var b = GetOperand(ins.FirstOperand, context) as bool?;
                        if (b.HasValue && b.Value) Skip(context);
                    }
                    break;
                case InstructionSet.SKIP:
                    {
                        var distance = (GetOperand(ins.FirstOperand, context) as int?).Value;
                        while (distance > 0) { Skip(context); --distance; }
                    }
                    break;

                case InstructionSet.EQUAL:
                    {
                        dynamic a = GetOperand(ins.FirstOperand, context);
                        dynamic b = GetOperand(ins.SecondOperand, context);
                        var result = (a == b);
                        SetOperand(ins.ThirdOperand, result, context);
                    }
                    break;

#endregion

#region Error Handling
                case InstructionSet.CATCH:
                    {
                        var returnPoint = new CodeContext(context.CodeContext.Code, context.CodeContext.InstructionPointer - 1);
                        var handler = GetOperand(ins.FirstOperand, context) as InstructionList;
                        var code = GetOperand(ins.SecondOperand, context) as InstructionList;
                        SetOperand(Operand.PUSH, returnPoint, context); //Push the return point
                        var catchContext = new ErrorHandler(handler, 0, context.ScopeStackDepth);
                        SetOperand(Operand.PUSH, catchContext, context);
                        context.CodeContext = new CodeContext(code, 0);
                    }
                    break;
                case InstructionSet.THROW:
                    {
                        var errorObject = GetOperand(ins.FirstOperand, context);
                        Throw(errorObject, context);
                    }
                    break;
#endregion

#region Maths
                case InstructionSet.ADD:
                    {
                        dynamic first = GetOperand(ins.FirstOperand, context);
                        dynamic second = GetOperand(ins.SecondOperand, context);
                        var result = first + second;
                        SetOperand(ins.ThirdOperand, result, context);
                    }
                    break;
                case InstructionSet.SUBTRACT:
                    {
                        dynamic first = GetOperand(ins.FirstOperand, context);
                        dynamic second = GetOperand(ins.SecondOperand, context);
                        var result = second - first;
                        SetOperand(ins.ThirdOperand, result, context);
                    }
                    break;
                case InstructionSet.MULTIPLY:
                    {
                        dynamic first = GetOperand(ins.FirstOperand, context);
                        dynamic second = GetOperand(ins.SecondOperand, context);
                        var result = first * second;
                        SetOperand(ins.ThirdOperand, result, context);
                    }
                    break;
                case InstructionSet.DIVIDE:
                    {
                        dynamic first = GetOperand(ins.FirstOperand, context);
                        dynamic second = GetOperand(ins.SecondOperand, context);
                        var result = first / second;
                        SetOperand(ins.ThirdOperand, result, context);
                    }
                    break;

#endregion

                default:
                    throw new NotImplementedException();
            }

        }

        public static void Throw(Object errorObject, ExecutionContext context)
        {
            while (true)
            {
                if (context.Stack.Count == 0)
                {
                    context.ExecutionState = ExecutionState.Error;
                    context.ErrorMessage = "Unhandled exception: " + errorObject;
                    break;
                }

                var topOfStack = context.Stack.Pop();
                if (topOfStack is ErrorHandler)
                {
                    var handler = (topOfStack as ErrorHandler?).Value;
                    context.CodeContext = handler.HandlerCode;
                    context.TrimScopes(handler.ScopeStackDepth);
                    context.Scope.PushVariable("error", errorObject);
                    break;
                }
            }
        }

        public static void SetOperand(Operand operand, Object value, ExecutionContext context)
        {
            switch (operand)
            {
                case Operand.NEXT: throw new InvalidOperationException("Can't set to next");
                case Operand.NONE: break; //Silently ignore.
                case Operand.PEEK: context.Stack.Pop(); context.Stack.Push(value); break;
                case Operand.POP: throw new InvalidOperationException("Can't set to pop");
                case Operand.PUSH: context.Stack.Push(value); break;
            }
        }

        public static Object GetOperand(Operand operand, ExecutionContext context)
        {
            switch (operand)
            {
                case Operand.NEXT: return context.CodeContext.Code[context.CodeContext.InstructionPointer++];
                case Operand.NONE: throw new InvalidOperationException("Can't fetch from nothing");
                case Operand.PEEK: return context.Stack.Peek();
                case Operand.POP: return context.Stack.Pop();
                case Operand.PUSH: throw new InvalidOperationException("Can't fetch from push");
                default: throw new InvalidProgramException();
            }
        }

        public static void Skip(ExecutionContext context)
        {
            if (context.CodeContext.InstructionPointer >= context.CodeContext.Code.Count)
            {
                context.ExecutionState = ExecutionState.Finished;
                return;
            }

            var nextInstruction = context.CodeContext.Code[context.CodeContext.InstructionPointer] as Instruction?;
            context.CodeContext.InstructionPointer += 1;
            if (!nextInstruction.HasValue) throw new InvalidOperationException("Encountered non-code in instruction stream");
            var ins = nextInstruction.Value;

            if (ins.FirstOperand == Operand.NEXT) context.CodeContext.InstructionPointer += 1;
            if (ins.SecondOperand == Operand.NEXT) context.CodeContext.InstructionPointer += 1;
            if (ins.ThirdOperand == Operand.NEXT) context.CodeContext.InstructionPointer += 1;
        }

        public struct MemberLookupResult
        {
            public bool FoundMember;
            public Object Member;

            public static MemberLookupResult Success(Object Member) { return new MemberLookupResult { Member = Member, FoundMember = true }; }
            public static MemberLookupResult Failure { get { return new MemberLookupResult { FoundMember = false }; }}
        }

        public static MemberLookupResult LookupMemberWithReflection(Object obj, String memberName)
        {
            System.Diagnostics.Debug.Assert(obj != null);

            var type = obj.GetType();

            var field = type.GetField(memberName);
            if (field != null)
                return MemberLookupResult.Success(field.GetValue(obj));

            var property = type.GetProperty(memberName);
            if (property != null)
                return MemberLookupResult.Success(property.GetValue(obj, null));

            var methods = type.FindMembers(
                System.Reflection.MemberTypes.Method,
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                new System.Reflection.MemberFilter((minfo, _obj) => { return minfo.Name == memberName; }),
                memberName);
            if (methods.Length != 0)
                return MemberLookupResult.Success(new OverloadedReflectionFunction(obj, memberName));

            return MemberLookupResult.Failure;
        }

        public static MemberLookupResult SetMemberWithReflection(Object obj, String memberName, Object value)
        {
            System.Diagnostics.Debug.Assert(obj != null);

            var type = obj.GetType();

            var field = type.GetField(memberName);
            if (field != null)
            {
                field.SetValue(obj, value);
                return MemberLookupResult.Success(value);
            }

            var property = type.GetProperty(memberName);
            if (property != null)
            {
                property.SetValue(obj, value, null);
                return MemberLookupResult.Success(value);
            }

            return MemberLookupResult.Failure;
        }
    }
}
