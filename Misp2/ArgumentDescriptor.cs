using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ArgumentDescriptor
    {
        public String Name;
        public String HelpText;

        public ArgumentDescriptor(String Name, String HelpText)
        {
            this.Name = Name;
            this.HelpText = HelpText;
        }
    }
}
