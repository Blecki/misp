using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Looping
    {

        [Test]
        public void _while()
        {
            int loops = 0;

            var script = "(while (while-test) 0)";
            Console.WriteLine("Test script: " + script);
            var Environment = TestHelper.CreateEnvironment();
            Console.WriteLine(loops);
            Environment.QuickBind("while-test", (_context, arguments) =>
                {
                    Console.WriteLine(loops);
                    ++loops;
                    return loops < 5;
                });

            var context = Environment.CompileScript(script);
            TestHelper.RunUntilFinished(context);
            Console.WriteLine("");
            Assert.AreEqual(5, loops);
        }

        [Test]
        public void map()
        {
            var Environment = TestHelper.CreateEnvironment();
            var context = Environment.CompileScript("(map x ^(0 1 2 3 4) (* x x))");
            TestHelper.RunUntilFinished(context);
            Assert.IsInstanceOf(typeof(List<Object>), context.Peek);
            var result = context.Peek as List<Object>;
            for (int i = 0; i < 5; ++i)
                Assert.AreEqual(i * i, result[i]);
        }
    }

}