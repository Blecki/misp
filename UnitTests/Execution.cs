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

        [Test]
        public void specials_called()
        {
            var Environment = TestHelper.CreateEnvironment();
            bool specialCalled = false;
            Environment.AddNativeSpecial("native", () =>
            {
                specialCalled = true;
                return 5;
            });
            var context = Environment.CompileScript("native");
            TestHelper.RunUntilFinished(context);
            Assert.AreEqual(true, specialCalled);
        }

        [Test]
        public void str_format()
        {
            var Environment = TestHelper.CreateEnvironment();
            Environment.AddNativeFunction("str_format", (_context, arguments) =>
                {
                    return String.Format(arguments[0].ToString(), arguments.GetRange(1, arguments.Count - 1).ToArray());
                });
            var context = Environment.CompileScript("(str_format \"{0}\" 42)");
            TestHelper.RunUntilFinished(context);
            Assert.AreEqual("42", context.Peek);
        }
    }

}