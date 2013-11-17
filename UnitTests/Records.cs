using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Records
    {
        [Test]
        public void creates_record()
        {
            var Environment = TestHelper.CreateEnvironment();
            var context = Environment.CompileScript("(record (foo 8) (bar 2))");
            TestHelper.RunUntilFinished(context);
            Assert.IsInstanceOf(typeof(ScriptObject), context.Peek);
            Assert.AreEqual(8, (context.Peek as ScriptObject).GetProperty("foo"));
        }

        [Test]
        public void sets_record_member()
        {
            var Environment = TestHelper.CreateEnvironment();
            //Have to go through some hoops in the script to get the record back out again.
            var script = "(let ((f (record (bar 2)))) (return-last (set f \"bar\" 3) f))";
            var context = Environment.CompileScript(script);
            TestHelper.RunUntilFinished(context);
            Assert.IsInstanceOf(typeof(ScriptObject), context.Peek);
            Assert.AreEqual(3, (context.Peek as ScriptObject).GetProperty("bar"));
        }

        [Test]
        public void gets_record_member()
        {
            var Environment = TestHelper.CreateEnvironment();
            var script = "(record (bar 5)).bar";
            var context = Environment.CompileScript(script);
            TestHelper.RunUntilFinished(context);
            Assert.AreEqual(5, context.Peek);
        }
    }

}