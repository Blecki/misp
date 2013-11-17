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

        public static void RunSimpleTest(String script, Object expectedResult)
        {
            Console.WriteLine("Test script: " + script);
            var Environment = TestHelper.CreateEnvironment();
            var context = Environment.CompileScript(script);
            TestHelper.RunUntilFinished(context);
            Console.WriteLine("");
            Assert.AreEqual(expectedResult, context.Peek);
        }
    }
}
