using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Compilation
    {
        private Environment Environment;

        [SetUp]
        public void Setup()
        {
            Environment = new Environment();
            Environment.SetupStandardEnvironment();
        }

        [Test]
        public void environment_compiles_trivial_script()
        {
            var context = Environment.CompileScript("(foo)");
            Assert.AreNotEqual(null, context);
        }

        [Test]
        public void core_functions_expanded()
        {
            bool coreFunctionExpanded = false;
            Environment.AddCoreFunction("frobnicate", "", new List<ArgumentDescriptor>(), (node, set) =>
            {
                coreFunctionExpanded = true;
                Assert.AreEqual(2, node.Children.Count);
                return new InstructionList();
            });
            var context = Environment.CompileScript("(frobnicate foo)");
            Assert.AreNotEqual(null, context);
            Assert.AreEqual(true, coreFunctionExpanded);
        }
    }

}