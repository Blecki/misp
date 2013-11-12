using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ParseNode
    {
        public String Type;
        public String Token;
        public List<ParseNode> Children;
        public String Prefix;

        public ParseNode(String Type)
        {
            this.Type = Type;
            this.Children = new List<ParseNode>();
        }
    }
}
