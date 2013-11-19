using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISPConsole
{
    class Program
    {
        static MISP.Environment Environment;

        static void Main(string[] args)
        {
            System.Console.Title = "MISP Console";
            System.Console.ForegroundColor = ConsoleColor.Green;

            Environment = new MISP.Environment();
            Environment.SetupStandardEnvironment();

            while (true)
            {
                Console.Write(":>");
                var input = Console.ReadLine();

                MISP.ExecutionContext context = null;
                try
                {
                    context = Environment.CompileScript(input);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Compilation error: " + e.Message);
                    continue;
                }

                while (context.ExecutionState == MISP.ExecutionState.Running)
                    MISP.VirtualMachine.Execute(context);

                if (context.ExecutionState == MISP.ExecutionState.Error)
                {
                    Console.WriteLine("Runtime error: " + context.ErrorMessage);
                    continue;
                }
                else if (context.ExecutionState == MISP.ExecutionState.Blocked)
                {
                    Console.WriteLine("Execution blocked.");
                    continue;
                }
                else
                {
                    try
                    {
                        Console.WriteLine(context.Peek.ToString());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error fetching result: " + e.Message);
                    }
                }
            }
        }
    }
}
