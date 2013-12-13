using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class StringExpressions
    {
        [Test]
        public void basic()
        {
            TestHelper.RunSimpleTest("(let ((foo 5)) \"foo: (foo)\")", "foo: 5");
        }

        [Test]
        public void escaped()
        {
            TestHelper.RunSimpleTest("(let ((foo 5)) $\"foo: (foo)\")", "foo: (foo)");
        }


    }

}