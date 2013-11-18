using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class RuntimeContext
    {
        internal Dictionary<String, NativeFunction> NativeFunctions = new Dictionary<String, NativeFunction>();
        internal Dictionary<String, Func<Object>> NativeSpecials = new Dictionary<String, Func<Object>>();
    }
}
