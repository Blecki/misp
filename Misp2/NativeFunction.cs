using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class NativeFunction : InvokeableFunction
    {
        public String Name = null;
        public List<ArgumentDescriptor> Arguments = null;
        public String HelpText = null;
        public Func<Context, List<Object>, Object> NativeImplementation { get; private set; }

        public NativeFunction(
            String Name,
            String HelpText,
            List<ArgumentDescriptor> Arguments,
            Func<Context, List<Object>, Object> NativeImplementation)
        {
            this.Name = Name;
            this.Arguments = Arguments;
            this.HelpText = HelpText;
            this.NativeImplementation = NativeImplementation;
        }

        public NativeFunction(Func<Context, List<Object>, Object> NativeImplementation)
        {
            this.NativeImplementation = NativeImplementation;
        }

        public override InvokationResult Invoke(Context context, List<Object> arguments)
        {
            var result = NativeImplementation.Invoke(context, arguments.GetRange(1, arguments.Count - 1));
            VirtualMachine.SetOperand(Operand.PUSH, result, context);

            return InvokationResult.Success;
        }
    }
}
