using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class CompiledFunction : InvokeableFunction
    {
        public Scope CapturedScope = null;
        public List<Object> Opcode = null;
    }
}
