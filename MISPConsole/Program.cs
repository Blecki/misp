using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MISP;

namespace MISPConsole
{
    class Program
    {
	    static void Main(string[] args)
        {
            System.Console.Title = MISP.Engine.VERSION;
            System.Console.ForegroundColor = ConsoleColor.Green;

            MISP.Console console = new MISP.Console((s) => { System.Console.Write(s); });

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
