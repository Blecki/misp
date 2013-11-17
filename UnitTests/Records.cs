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
        private Environment Environment;

        [SetUp]
        public void Setup()
        {
            Environment = new Environment();
            Environment.SetupStandardEnvironment();
        }

        [Test]
        public void creates_record()
        {
            var context = Environment.CompileScript("(record (foo 8) (bar 2))");
            while (context.ExecutionState == ExecutionState.Running)
                VirtualMachine.Execute(context);
            Assert.AreEqual(ExecutionState.Finished, context.ExecutionState);
            Assert.IsInstanceOf(typeof(ScriptObject), context.Peek);
            Assert.AreEqual(8, (context.Peek as ScriptObject).GetProperty("foo"));
        }

        [Test, Property("emit-assembly", "true")]
        public void sets_record_member()
        {
            //Have to go through some hoops in the script to get the record back out again.
            var script = "(let ((f (record (bar 2)))) (return-last (set f \"bar\" 3) f))";
            var context = Environment.CompileScript(script);
            if (TestContext.CurrentContext.Test.Properties["emit-assembly"] as String == "true")
                Debug.DumpCompiledCode(context, Console.Out);
            while (context.ExecutionState == ExecutionState.Running)
                VirtualMachine.Execute(context);
            Assert.AreEqual(ExecutionState.Finished, context.ExecutionState);
            Assert.IsInstanceOf(typeof(ScriptObject), context.Peek);
            Assert.AreEqual(3, (context.Peek as ScriptObject).GetProperty("bar"));
        }
    }

}