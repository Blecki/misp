using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MISP
{
    [TestFixture]
    public class ThreadPoolTests
    {
        Environment Environment;
        ThreadPool ThreadPool;
        Action<ExecutionContext> ThreadCallback;
        List<ExecutionContext> Contexes = new List<ExecutionContext>();

        [SetUp]
        public void Setup()
        {
            Environment = TestHelper.CreateEnvironment();
            Environment.QuickBind("test", (context, arguments) =>
                {
                    ThreadCallback(context);
                    return 0;
                });
            ThreadPool = new MISP.ThreadPool();
        }

        public ExecutionContext AddScript(String Script, int Priority)
        {
            var context = Environment.CompileScript(Script);
            Contexes.Add(context);
            ThreadPool.StartScript(context, Priority);
            return context;
        }

        [Test]
        public void threads_are_added()
        {
            AddScript("0", 0);
            AddScript("0", 0);
            Assert.AreEqual(2, ThreadPool.ScriptCount);
        }

        [Test]
        public void threads_complete()
        {
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);
            AddScript("(+ 0 1 2 3 4 5 6 7 8 9)", 1);

            while (ThreadPool.ScriptCount > 0) ThreadPool.Execute();
        }

        [Test]
        public void high_priority_runs_most_often()
        {
            var runFrequency = new Dictionary<ExecutionContext, int>();
            ThreadCallback += (context) =>
                {
                    if (runFrequency.ContainsKey(context))
                        runFrequency[context] += 1;
                    else
                        runFrequency.Add(context, 1);
                };

            AddScript("(while true (test))", 1);
            AddScript("(while true (test))", 1);
            Console.WriteLine("High priority is thread " + Contexes.Count);
            var highPriority = AddScript("(while true (test))", 10);

            for (int i = 0; i < 1000; ++i)
                ThreadPool.Execute();

            foreach (var entry in runFrequency)
                Console.WriteLine("Thread " + Contexes.IndexOf(entry.Key) + " - " + entry.Value);

            var maximumFrequency = runFrequency.Max((kvp) => { return kvp.Value; });
            Assert.AreEqual(true, runFrequency.ContainsKey(highPriority));
            Assert.AreEqual(maximumFrequency, runFrequency[highPriority]);
            Assert.AreEqual(Contexes.Count, runFrequency.Count);
            Assert.AreEqual(false, runFrequency.ContainsValue(0));
        }
    }

}