using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class VirtualMachine
    {
        public static void Execute(Context context)
        {
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
                        SetOperand(ins.SecondOperand, context.Scope.GetVariable(name), context);
                    }
                    break;
                case InstructionSet.MEMBER_LOOKUP:
                    {
                        var member_name = GetOperand(ins.FirstOperand, context).ToString();
                        var obj = GetOperand(ins.SecondOperand, context);
                        //TODO: Use reflection to find the member.
                        SetOperand(ins.ThirdOperand, null, context);
                    }
                    break;
#endregion

#region Flow Control
                case InstructionSet.LAMBDA_PREAMBLE:
                    {
                        var arguments = GetOperand(ins.FirstOperand, context) as List<Object>;
                        var argument_names = GetOperand(ins.SecondOperand, context) as List<String>;
                        if (arguments == null || argument_names == null) throw new InvalidOperationException("Improper lambda call");
                        for (var i = 0; i < argument_names.Count; ++i)
                            context.Scope.PushVariable(argument_names[i], arguments[i + 1]);
                    }
                    break;
                case InstructionSet.BEGIN_LOOP:
                    {
                        var storedContext = context.CodeContext;
                        storedContext.InstructionPointer -= 1; //Rewind to the start of this instruction
                        SetOperand(ins.FirstOperand, storedContext, context);
                    }
                    break;
                case InstructionSet.BREAK:
                    context.CodeContext = (GetOperand(ins.FirstOperand, context) as CodeContext?).Value;
                    Skip(context); //Skip the instruction stored by BEGIN_LOOP
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
                case InstructionSet.INVOKE:
                    {
                        var arguments = GetOperand(ins.FirstOperand, context) as List<Object>;
                        var function = arguments[0];
                        if (function is InvokeableFunction)
                            (function as InvokeableFunction).Invoke(context, arguments);
                        else
                            throw new InvalidOperationException("Can't invoke what isn't invokeable.");
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

#endregion

                default:
                    throw new NotImplementedException();
            }

        }

        public static void SetOperand(Operand operand, Object value, Context context)
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

        public static Object GetOperand(Operand operand, Context context)
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

        public static void Skip(Context context)
        {
            var nextInstruction = context.CodeContext.Code[context.CodeContext.InstructionPointer] as Instruction?;
            context.CodeContext.InstructionPointer += 1;
            if (!nextInstruction.HasValue) throw new InvalidOperationException("Encountered non-code in instruction stream");
            var ins = nextInstruction.Value;

            if (ins.FirstOperand == Operand.NEXT) context.CodeContext.InstructionPointer += 1;
            if (ins.SecondOperand == Operand.NEXT) context.CodeContext.InstructionPointer += 1;
            if (ins.ThirdOperand == Operand.NEXT) context.CodeContext.InstructionPointer += 1;
        }
    }
}
