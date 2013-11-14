using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class InvokeableFunction
    {
        public virtual void Invoke(Context context, List<Object> arguments) { throw new NotImplementedException(); }
    }
}
