using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Environment
    {
        internal CompileContext CompileContext = new CompileContext();
        internal RuntimeContext RuntimeContext = new RuntimeContext();

        public void AddCoreFunction(
            String Name, 
            String HelpText,
            List<ArgumentDescriptor> Arguments, 
            Func<ParseNode, CompileContext, InstructionList> Emit)
        {
            CompileContext.CoreFunctions.Upsert(Name, new CoreFunction(Name, HelpText, Arguments, Emit));
        }

        public void AddCompileTimeConstant(String Name, Object Value)
        {
            CompileContext.CompileTimeConstants.Upsert(Name, new CompileTimeConstant(Name, Value));
        }

        public void AddNativeFunction(String Name, Func<ExecutionContext, List<Object>, Object> Implementation)
        {
            RuntimeContext.NativeFunctions.Upsert(Name, new NativeFunction(Implementation));
            RuntimeContext.NativeFunctions[Name].Name = Name;
        }

        public void AddNativeFunction(String Name, NativeFunction Function)
        {
            RuntimeContext.NativeFunctions.Upsert(Name, Function);
        }

        public void AddNativeSpecial(String Name, Func<Object> Fetch)
        {
            RuntimeContext.NativeSpecials.Upsert(Name, Fetch);
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
            MISP.StandardLibrary.WhileFunction(this);
            MISP.StandardLibrary.ForFunction(this);
        }

        public static Environment CreateStandardEnvironment()
        {
            var r = new Environment();
            r.SetupStandardEnvironment();
            return r;
        }
       
        public ExecutionContext CompileScript(String Script)
        {
            var parsedScript = Parser.ParseRoot(Script, "");
            var compiledScript = Compiler.Compile(parsedScript, CompileContext);
            return new ExecutionContext(new CodeContext(compiledScript, 0), this);
        }

        public Object RunScript(String Script)
        {
            var context = CompileScript(Script);
            return RunScript(context);
        }

        public Object RunScript(ExecutionContext context)
        {
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
