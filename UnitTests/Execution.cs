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
        [Test]
        public void executes_trivial_script()
        {
            var Environment = TestHelper.CreateEnvironment();
            var context = Environment.CompileScript("0");
            TestHelper.RunUntilFinished(context);
        }

        [Test]
        public void calls_native_function()
        {
            var Environment = TestHelper.CreateEnvironment();
            bool nativeFunctionCalled = false;
            Environment.AddNativeFunction("native", (_context, arguments) =>
                {
                    nativeFunctionCalled = true;
                    return 0;
                });
            var context = Environment.CompileScript("(native)");
            TestHelper.RunUntilFinished(context);
            Assert.AreEqual(true, nativeFunctionCalled);
        }
    }

}