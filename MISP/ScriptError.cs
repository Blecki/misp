using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ScriptError : Exception 
    {
        public ScriptObject generatedAt = null;

        public ScriptError(String msg, ScriptObject generatedAt) : base(msg)
        {
            this.generatedAt = generatedAt;
        } 
    }
}
