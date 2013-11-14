using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class BuiltInFunction
    {
        //Generates in-line code to implement function.
        //EG; MAP generates code to loop over an element and invoke other code mutliple times

        public Func<ParseNode, FunctionSet, InstructionList> EmitOpcode = null;

        public BuiltInFunction(Func<ParseNode, FunctionSet, InstructionList> emit)
        {
            this.EmitOpcode = emit;
        }
    }
}
