using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class StaticOverloadedReflectionFunction : InvokeableFunction
    {
        internal System.Type ThisType;
        internal String MethodName;

        public StaticOverloadedReflectionFunction(System.Type ThisType, String MethodName)
        {
            this.ThisType = ThisType;
            this.MethodName = MethodName;
        }

         public override InvokationResult Invoke(ExecutionContext context, List<Object> arguments)
        {
            var trimmedArguments = arguments.GetRange(1, arguments.Count - 1);
             var argumentTypes = trimmedArguments.Select((obj) => obj.GetType()).ToArray();

             var method = ThisType.GetMethod(MethodName, argumentTypes);
             if (method == null)
             {
                 var errorMessage = String.Format("Could not find overload for {0} that takes argument types {1} on {2}.",
                     MethodName,
                     String.Join(", ", argumentTypes.Select(t => t.Name)),
                     ThisType.Name);
                 return InvokationResult.Failure(errorMessage);
             }

             if (!method.IsStatic) return InvokationResult.Failure(
                     String.Format("Method with name {0} found on {1} is not static.",
                        MethodName, ThisType.Name));

             var result = method.Invoke(null, trimmedArguments.ToArray());

             VirtualMachine.SetOperand(Operand.PUSH, result, context);
             return InvokationResult.Success;
        }
    }
}
