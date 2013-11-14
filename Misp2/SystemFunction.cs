using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class SystemFunction : InvokeableFunction
    {
        public Func<Context, List<Object>, Object> SystemImplementation { get; private set; }

        public override void Invoke(Context context, List<Object> arguments)
        {
            var result = SystemImplementation.Invoke(context, arguments.GetRange(1, arguments.Count - 1));
            VirtualMachine.SetOperand(Operand.PUSH, result, context);
        }

        public SystemFunction(Func<Context, List<Object>, Object> Implementation)
        {
            this.SystemImplementation = Implementation;
        }
    }
}
