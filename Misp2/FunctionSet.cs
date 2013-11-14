using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class FunctionSet : Dictionary<String, BuiltInFunction>
    {
        public void AddBuiltin(String name, Func<ParseNode, FunctionSet, InstructionList> emit)
        {
            this.Upsert(name, new BuiltInFunction(emit));
        }
    }
}
