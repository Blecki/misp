using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class LambdaFunction : InvokeableFunction
    {
        public InstructionList Opcode = null;
        public List<String> ArgumentNames = null;
        public Scope CapturedScope = null;

        public override InvokationResult Invoke(ExecutionContext context, List<object> arguments)
        {
            context.Stack.Push(context.CodeContext);
            context.CodeContext = new CodeContext(Opcode, 0);

            context.PushScope(CapturedScope.Capture());
            for (int i = 0; i < ArgumentNames.Count; ++i)
                context.Scope.PushVariable(ArgumentNames[i], arguments[i + 1]);

            return InvokationResult.Success;
        }

        public static LambdaFunction CreateLambda(Scope CapturedScope, List<String> ArgumentNames, InstructionList Code)
        {
            var r = new LambdaFunction();
            r.CapturedScope = CapturedScope;
            r.ArgumentNames = ArgumentNames;
            r.Opcode = new InstructionList(
                new InPlace(Code),
                "POP_SCOPE",        //Cleanup the function when it's finished.
                "SWAP_TOP",
                "BREAK POP");
            return r;
        }
    }
}
