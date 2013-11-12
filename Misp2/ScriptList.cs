using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ScriptList : List<Object>
    {
        public ScriptList(IEnumerable<Object> collection) : base(collection) { }
        public ScriptList() { }

        public ScriptList(params Object[] items)
        {
            foreach (var item in items) Add(item);
        }
    }
}
