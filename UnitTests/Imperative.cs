using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Imperative
    {
        [Test]
        public void return_last()
        {
            TestHelper.RunSimpleTest("(return-last 0 1 2 3)", 3);
        }

        [Test]
        public void return_first()
        {
            TestHelper.RunSimpleTest("(return-first 0 1 2 3)", 0);
        }
    }

}