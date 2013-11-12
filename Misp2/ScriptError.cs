using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ScriptError : Exception 
    {
        public ScriptError(String msg) : base(msg)
        {
        } 
    }
}
