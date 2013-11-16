using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static List<ArgumentDescriptor> Arguments(params String[] descriptors)
        {
            System.Diagnostics.Debug.Assert(descriptors.Length % 2 == 0);
            var r = new List<ArgumentDescriptor>();
            for (var i = 0; i < descriptors.Length; i += 2)
                r.Add(new ArgumentDescriptor(descriptors[i], descriptors[i + 1]));
            return r;
        }
    }
}
