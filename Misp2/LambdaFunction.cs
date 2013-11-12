using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class LambdaFunction
    {
        public Scope CapturedScope { get; private set; }
        public List<Object> CompiledCode { get; private set; }
    }
}
