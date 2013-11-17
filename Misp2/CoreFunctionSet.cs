using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class CoreFunctionSet 
    {
        internal Dictionary<String, CoreFunction> CoreFunctions = new Dictionary<string, CoreFunction>();
        internal Dictionary<String, CompileTimeConstant> CompileTimeConstants = new Dictionary<string, CompileTimeConstant>();        
    }
}
