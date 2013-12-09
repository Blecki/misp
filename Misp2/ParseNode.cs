using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public enum NodeTypes
    {
        Node,
        MemberAccess,
        Token,
        Number,
        Character,
        String,
        StringExpression
    }

    public enum Prefixes
    {
        None,
        ExpandInPlace,
        AsList,
        AsLiteral,
        Lookup,
        Evaluate
    }

    public class ParseNode
    {
        public NodeTypes Type;
        public String Token;
        public List<ParseNode> Children;
        public Prefixes Prefix;

        public ParseNode(NodeTypes Type)
        {
            this.Type = Type;
            this.Children = new List<ParseNode>();
            this.Prefix = Prefixes.None;
        }

        public ParseNode(NodeTypes Type, Prefixes Prefix, String Token)
        {
            this.Type = Type;
            this.Prefix = Prefix;
            this.Token = Token;
        }
    }
}
