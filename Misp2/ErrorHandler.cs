using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public struct ErrorHandler
    {
        public int ScopeStackDepth;
        public CodeContext HandlerCode;

        public ErrorHandler(
            InstructionList Code, 
            int InstructionPointer, 
            int ScopeStackDepth)
        {
            this.HandlerCode = new CodeContext(Code, InstructionPointer);
            this.ScopeStackDepth = ScopeStackDepth;
        }
    }
}
