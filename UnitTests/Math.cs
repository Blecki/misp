using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Math
    {
        [Test]
        public void addition()
        {
            TestHelper.RunSimpleTest("(+ 1 1)", 2);
            TestHelper.RunSimpleTest("(+ 0 5)", 5);
            TestHelper.RunSimpleTest("(+ 1.5 20)", 21.5f);
            TestHelper.RunSimpleTest("(+ -5 5)", 0);
            TestHelper.RunSimpleTest("(+ 1 1 1 1)", 4);
        }

        [Test]
        public void subtraction()
        {
            TestHelper.RunSimpleTest("(- 1 1)", 0);
            TestHelper.RunSimpleTest("(- 0 5)", -5);
            TestHelper.RunSimpleTest("(- 1.5 20)", -18.5f);
            TestHelper.RunSimpleTest("(- -5 5)", -10);
            TestHelper.RunSimpleTest("(- 10 2 2)", 6);
        }

        [Test]
        public void multiplication()
        {
            TestHelper.RunSimpleTest("(* 1 1)", 1);
            TestHelper.RunSimpleTest("(* 0 5)", 0);
            TestHelper.RunSimpleTest("(* 1.5 20)", 30.0f);
            TestHelper.RunSimpleTest("(* -5 5)", -25);
            TestHelper.RunSimpleTest("(* 2 2 2 2)", 16);
        }

        [Test]
        public void division()
        {
            TestHelper.RunSimpleTest("(/ 1 1)", 1);
            TestHelper.RunSimpleTest("(/ 0 5)", 0);
            TestHelper.RunSimpleTest("(/ 20 2)", 10);
            TestHelper.RunSimpleTest("(/ -25 5)", -5);
        }
    }

}