using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Environment
    {
        internal FunctionSet BuiltInFunctions = new FunctionSet();

        public void AddCoreFunction(
            String Name, 
            String HelpText,
            List<ArgumentDescriptor> Arguments, 
            Func<ParseNode, FunctionSet, InstructionList> Emit)
        {
            BuiltInFunctions.Upsert(Name, new CoreFunction(Name, HelpText, Arguments, Emit));
        }

        public void SetupStandardEnvironment()
        {
            MISP.StandardLibrary.MapFunctions(this);
            MISP.StandardLibrary.LambdaFunctions(this);
            MISP.StandardLibrary.LetFunction(this);
            MISP.StandardLibrary.ExceptionFunctions(this);
        }
       
        public Context CompileScript(String Script)
        {
            var parsedScript = Parser.ParseRoot(Script, "");
            var compiledScript = Compiler.Compile(parsedScript, BuiltInFunctions);
            return new Context(new CodeContext(compiledScript, 0));
        }



    }
}
