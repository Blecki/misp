using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Thread
    {
        public ExecutionContext Context;
        public int Priority;
        public DateTime LastRan;

        public int RateImportance()
        {
            return (DateTime.Now - LastRan).Milliseconds * Priority;
        }

        public Thread(ExecutionContext Context, int Priority)
        {
            this.Context = Context;
            this.Priority = Priority;
            LastRan = DateTime.Now;
        }
    }

    public class ThreadComparer : IComparer<Thread>
    {
        public int Compare(Thread x, Thread y)
        {
               return y.RateImportance() - x.RateImportance();
        }
    }

    public class ThreadPool
    {
        public List<Thread> RunningScripts = new List<Thread>();

        public int ScriptCount { get { return RunningScripts.Count; } }
        public int CyclesAvailable { get; set; }
        public int PerScriptAllotment { get; set; }

        public ThreadPool()
        {
            CyclesAvailable = 100;
            PerScriptAllotment = 10;
        }

        public void StartScript(ExecutionContext context, int priority)
        {
            RunningScripts.Add(new Thread(context, priority));
        }

        public void Execute()
        {
            if (RunningScripts.Count == 0) return;

            int cyclesUsed = 0;
            RunningScripts.Sort(new ThreadComparer());
            var nextThread = 0;
            var currentThread = RunningScripts[0];
            currentThread.LastRan = DateTime.Now;
            int cyclesThisThread = 0;
            while (cyclesUsed < CyclesAvailable)
            {
                VirtualMachine.Execute(currentThread.Context);
                cyclesThisThread += 1;

                if (currentThread.Context.ExecutionState == ExecutionState.Error ||
                    currentThread.Context.ExecutionState == ExecutionState.Finished)
                {
                    RunningScripts.Remove(currentThread);
                    if (RunningScripts.Count == 0) return;
                    currentThread = RunningScripts[nextThread];
                    currentThread.LastRan = DateTime.Now;
                    cyclesThisThread = 0;
                }
                else
                {
                    if (cyclesThisThread >= PerScriptAllotment * currentThread.Priority)
                    {
                        nextThread += 1;
                        if (nextThread >= RunningScripts.Count) nextThread = 0;
                        currentThread = RunningScripts[nextThread];
                        currentThread.LastRan = DateTime.Now;
                        cyclesThisThread = 0;
                    }
                }

                cyclesUsed += 1;
            }
        }

        public void ExecuteEachUntilBlock()
        {
            if (RunningScripts.Count == 0) return;

            for (var i = 0; i < RunningScripts.Count; )
            {
                var currentThread = RunningScripts[i];

                while (currentThread.Context.ExecutionState == ExecutionState.Running)
                    VirtualMachine.Execute(currentThread.Context);

                if (currentThread.Context.ExecutionState == ExecutionState.Blocked)
                    ++i;
                else
                    RunningScripts.RemoveAt(i);
            }
        }

    }
}
