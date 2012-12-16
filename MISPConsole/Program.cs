using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MISP;

namespace MISPConsole
{
    class RefTest
    {
        public bool test(string f)
        {
            return true;
        }
    }

    class Program
    {
	    static void Main(string[] args)
        {
            System.Console.Title = MISP.Engine.VERSION;
            System.Console.ForegroundColor = ConsoleColor.Green;

            MISP.Console console = new MISP.Console((s) => { System.Console.Write(s); });
            console.mispEngine.AddFunction("make-reftest", "", (context, arguments) => { return new RefTest(); });

            if (args.Length > 0)
            {
                var invoke = "(run-file \"" + args[0] + "\")";
                System.Console.WriteLine(invoke);
                console.Execute(invoke);
            }

            while (true)
            {
                System.Console.Write(":>");
                var command = System.Console.ReadLine();
                if (String.IsNullOrEmpty(command)) continue;
                if (command[0] == '\\')
                {
                    if (command.StartsWith("\\q")) return;
                    else if (command.StartsWith("\\limit"))
                    {
                        try
                        {
                            var time = Convert.ToSingle(command.Substring(7));
                            console.mispContext.allowedExecutionTime = TimeSpan.FromSeconds(time);
                            console.mispContext.limitExecutionTime = time > 0;

                            if (console.mispContext.limitExecutionTime)
                                System.Console.Write("Execution time limit set to " + console.mispContext.allowedExecutionTime + "\n");
                            else
                                System.Console.Write("Execution time limit disabled.\n");
                        }
                        catch (Exception e)
                        {
                            System.Console.Write("Error: " + e.Message + "\n");
                        }
                    }
                    else System.Console.Write("I don't understand.\n");
                }
                else
                {
                    console.Execute("(" + command + ")");
                }
            }
        }
    }
}
