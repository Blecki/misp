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
            var functionSet = new MISP.FunctionSet();
            MISP.StandardLibrary.MapFunctions(functionSet);
            MISP.StandardLibrary.LambdaFunctions(functionSet);
            MISP.StandardLibrary.LetFunction(functionSet);
            MISP.StandardLibrary.ExceptionFunctions(functionSet);

            var compiled = MISP.Compiler.Compile(MISP.Parser.ParseRoot(code, ""), functionSet);
            var stream = new System.IO.StreamWriter("test.txt");
            MISP.Compiler.DumpOpcode(compiled, stream);

            stream.WriteLine("****OUTPUT****");

            var systemFunction = new MISP.SystemFunction((_context, arguments) =>
                    {
                        foreach (var item in arguments)
                            stream.WriteLine(item.ToString());
                        return null;
                    });

            var context = new MISP.Context(new MISP.CodeContext(compiled, 0));
            context.Scope.PushVariable("print", systemFunction);

            try
            {
                while (true) MISP.VirtualMachine.Execute(context);
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
