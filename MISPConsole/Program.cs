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

            MISP.Console console = new MISP.Console((s) => { System.Console.Write(s); }, null);

            var autoBindTest = MISP.AutoBind.GenerateTypeBinding(typeof(MISP.Engine));
            console.environment.engine.AddGlobalVariable("Engine", (context) => { return autoBindTest; });
            
            if (args.Length > 0)
            {
                var invoke = "(run-file \"" + args[0] + "\")";
                System.Console.WriteLine(invoke);
                console.ExecuteCommand(invoke);
            }

            while (true)
            {
                System.Console.Write(":>");
                var command = System.Console.ReadLine();
                if (String.IsNullOrEmpty(command)) continue;
                
                    console.ExecuteCommand("(" + command + ")");
            }
        }
    }
}
