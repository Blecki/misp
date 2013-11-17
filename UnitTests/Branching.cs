using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Branching
    {
        [Test]
        public void _if()
        {
            TestHelper.RunSimpleTest("(if true 0 1)", 0);
            TestHelper.RunSimpleTest("(if false 0 1)", 1);
            TestHelper.RunSimpleTest("(if (= 1 1) 0 1)", 0);
            TestHelper.RunSimpleTest("(if (= 1 0) 0 1)", 1);
        }

    }

}