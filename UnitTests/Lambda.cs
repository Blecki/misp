using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class Lambda
    {
        [Test]
        public void lambdas_can_be_created()
        {
            var result = TestHelper.RunSimpleTest("(lambda (x) (* x x))");
            Assert.IsInstanceOf(typeof(InvokeableFunction), result);
        }

        [Test]
        public void lambdas_capture_scope()
        {
            var result = TestHelper.RunSimpleTest("(let ((foo 9)) (lambda (x) (* x foo)))") as LambdaFunction;
            Assert.IsNotNull(result);
            Assert.IsTrue(result.CapturedScope.HasVariable("foo"));
            Assert.AreEqual(9, result.CapturedScope.GetVariable("foo"));
        }

        [Test]
        public void lambda_can_be_called()
        {
            TestHelper.RunSimpleTest(@"
                (
                    (let ((foo 9)) 
                        (lambda (x) (* x foo))
                    ) 
                2)", 18);
        }     
    }

}