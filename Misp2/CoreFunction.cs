using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class CoreFunction
    {
        public String Name = null;
        public List<ArgumentDescriptor> Arguments = new List<ArgumentDescriptor>();
        public String HelpText = null;
        public Func<ParseNode, CompileContext, InstructionList> EmitOpcode = null;

        public CoreFunction(
            String Name,
            String HelpText,
            List<ArgumentDescriptor> Arguments,
            Func<ParseNode, CompileContext, InstructionList> emit)
        {
            this.Name = Name;
            this.Arguments = Arguments;
            this.HelpText = HelpText;
            this.EmitOpcode = emit;
        }
    }
}
