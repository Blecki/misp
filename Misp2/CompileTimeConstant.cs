using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class CompileTimeConstant
    {
        public String Name = null;
        public Object Value = null;

        public CompileTimeConstant(
            String Name,
            Object Value)
        {
            this.Name = Name;
            this.Value = Value;
        }
    }
}
