using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Execution
    {
        private Environment Environment;

        [SetUp]
        public void Setup()
        {
            Environment = new Environment();
            Environment.SetupStandardEnvironment();
        }

        [Test]
        public void executes_trivial_script()
        {
            var context = Environment.CompileScript("0");
            while (context.ExecutionState == ExecutionState.Running)
                VirtualMachine.Execute(context);
            Assert.AreEqual(ExecutionState.Finished, context.ExecutionState);
        }

        [Test]
        public void calls_native_function()
        {
            bool nativeFunctionCalled = false;
            Environment.QuickBind("native", (_context, arguments) =>
                {
                    nativeFunctionCalled = true;
                    return 0;
                });
            var context = Environment.CompileScript("(native)");
            while (context.ExecutionState == ExecutionState.Running)
                VirtualMachine.Execute(context);
            Assert.AreEqual(true, nativeFunctionCalled);
            Assert.AreEqual(ExecutionState.Finished, context.ExecutionState);
        }
    }

}