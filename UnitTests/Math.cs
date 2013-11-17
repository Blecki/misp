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
        public void run_test(String script, Object expectedResult)
        {
            var Environment = TestHelper.CreateEnvironment();
            var context = Environment.CompileScript(script);
            TestHelper.RunUntilFinished(context);
            Assert.AreEqual(expectedResult, context.Peek);
        }

        [Test]
        public void addition()
        {
            run_test("(+ 1 1)", 2);
            run_test("(+ 0 5)", 5);
            run_test("(+ 1.5 20)", 21.5f);
            run_test("(+ -5 5)", 0);
            run_test("(+ 1 1 1 1)", 4);
        }

        [Test]
        public void subtraction()
        {
            run_test("(- 1 1)", 0);
            run_test("(- 0 5)", -5);
            run_test("(- 1.5 20)", -18.5f);
            run_test("(- -5 5)", -10);
            run_test("(- 10 2 2)", 6);
        }

        [Test]
        public void multiplication()
        {
            run_test("(* 1 1)", 1);
            run_test("(* 0 5)", 0);
            run_test("(* 1.5 20)", 30.0f);
            run_test("(* -5 5)", -25);
            run_test("(* 2 2 2 2)", 16);
        }

        [Test]
        public void division()
        {
            run_test("(/ 1 1)", 1);
            run_test("(/ 0 5)", 0);
            run_test("(/ 20 2)", 10);
            run_test("(/ -25 5)", -5);
        }
    }

}