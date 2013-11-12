using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class SystemFunction
    {
        public Func<Context, ScriptList, Object> SystemImplementation { get; private set; }
    }
}
