using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    public class TestHelper
    {
        public static Environment CreateEnvironment()
        {
            var r = new Environment();
            r.SetupStandardEnvironment();
            return r;
        }

        public static void RunUntilFinished(Context context)
        {
            Debug.DumpCompiledCode(context, Console.Out);
            while (context.ExecutionState == ExecutionState.Running)
                VirtualMachine.Execute(context);
            if (context.ExecutionState == ExecutionState.Error)
                Console.WriteLine(context.ErrorMessage);
            Assert.AreEqual(ExecutionState.Finished, context.ExecutionState);
        }

        public static Object ReturnValue(Context context)
        {
            Assert.DoesNotThrow(() => { var x = context.Peek; });
            return context.Peek;
        }
    }
}
