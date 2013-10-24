using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISPConsole
{
    class Program
    {
        static MISP.Console console;

        static void Main(string[] args)
        {
            System.Console.Title = MISP.Engine.VERSION;
            System.Console.ForegroundColor = ConsoleColor.Green;

            console = new MISP.Console(Console.Write,
                (engine) =>
                {
                    var globals = new MISP.GenericScriptObject();
                    engine.AddGlobalVariable("@globals", c => globals);
                });

            foreach (var arg in args)
                console.ExecuteCommand(arg);

            while (true)
            {
                Console.Write(":>");
                var input = Console.ReadLine();
                console.ExecuteCommand(input);
            }
        }
    }
}
