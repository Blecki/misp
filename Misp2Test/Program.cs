using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Misp2Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var code = @"(catch (bar) (print error))";

            var Environment = new MISP.Environment();
            Environment.SetupStandardEnvironment();
            

            var context = Environment.CompileScript(code);
            var stream = new System.IO.StreamWriter("test.txt");
            MISP.Debug.DumpCompiledCode(context, stream);

            stream.WriteLine("****OUTPUT****");

            Environment.QuickBind("print", (_context, arguments) =>
            {
                foreach (var item in arguments)
                    stream.WriteLine(item.ToString());
                return null;
            });

            try
            {
                while (context.ExecutionState == MISP.ExecutionState.Running) MISP.VirtualMachine.Execute(context);
            }
            catch (Exception e)
            {
                stream.WriteLine(e.Message);
                stream.WriteLine(e.StackTrace);
            }

            stream.Close();

        }
    }
}
