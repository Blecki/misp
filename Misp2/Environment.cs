using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Environment
    {
        //TODO: Round robin threaded execution of scripts.

        internal CompileContext CoreFunctions = new CompileContext();
        internal NativeFunctionSet NativeFunctions = new NativeFunctionSet();

        public void AddCoreFunction(
            String Name, 
            String HelpText,
            List<ArgumentDescriptor> Arguments, 
            Func<ParseNode, CompileContext, InstructionList> Emit)
        {
            CoreFunctions.CoreFunctions.Upsert(Name, new CoreFunction(Name, HelpText, Arguments, Emit));
        }

        public void AddCompileTimeConstant(String Name, Object Value)
        {
            CoreFunctions.CompileTimeConstants.Upsert(Name, new CompileTimeConstant(Name, Value));
        }

        public void QuickBind(
            String Name,
            Func<ExecutionContext, List<Object>, Object> Implementation)
        {
            NativeFunctions.Upsert(Name, new NativeFunction(Implementation));
        }

        public void SetupStandardEnvironment()
        {
            MISP.StandardLibrary.MapFunctions(this);
            MISP.StandardLibrary.LambdaFunctions(this);
            MISP.StandardLibrary.LetFunction(this);
            MISP.StandardLibrary.ExceptionFunctions(this);
            MISP.StandardLibrary.SetFunction(this);
            MISP.StandardLibrary.RecordFunction(this);
            MISP.StandardLibrary.ImperativeFunctions(this);
            MISP.StandardLibrary.MathFunctions(this);
            MISP.StandardLibrary.BooleanBranching(this);
        }
       
        public ExecutionContext CompileScript(String Script)
        {
            var parsedScript = Parser.ParseRoot(Script, "");
            var compiledScript = Compiler.Compile(parsedScript, CoreFunctions);
            return new ExecutionContext(new CodeContext(compiledScript, 0), this);
        }

        public Object RunScript(String Script)
        {
            var context = CompileScript(Script);
            while (context.ExecutionState == ExecutionState.Running)
                VirtualMachine.Execute(context);
            if (context.ExecutionState == ExecutionState.Error)
                throw new InvalidOperationException(context.ErrorMessage);
            if (context.ExecutionState == ExecutionState.Blocked)
                throw new InvalidOperationException("Script blocked while quick-running.");
            return context.Peek;
        }

    }
}
