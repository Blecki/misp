using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    internal static class PrefixCheck
    {
        private static Dictionary<NodeTypes, List<Prefixes>> allowed = null;
        internal static bool CheckPrefix(ParseNode node)
        {
            if (allowed == null)
            {
                allowed = new Dictionary<NodeTypes, List<Prefixes>>();
                
                allowed.Add(NodeTypes.Node, new List<Prefixes>());
                allowed[NodeTypes.Node].Add(Prefixes.ExpandInPlace);
                allowed[NodeTypes.Node].Add(Prefixes.AsList);
                allowed[NodeTypes.Node].Add(Prefixes.AsLiteral);
                allowed[NodeTypes.Node].Add(Prefixes.Lookup);
                allowed[NodeTypes.Node].Add(Prefixes.Evaluate);

                allowed.Add(NodeTypes.Token, new List<Prefixes>());
                allowed[NodeTypes.Token].Add(Prefixes.ExpandInPlace);
                allowed[NodeTypes.Token].Add(Prefixes.Lookup);
                allowed[NodeTypes.Token].Add(Prefixes.Evaluate);

                allowed.Add(NodeTypes.String, new List<Prefixes>());
                allowed[NodeTypes.String].Add(Prefixes.Lookup);
                allowed[NodeTypes.String].Add(Prefixes.Evaluate);
            }

            if (node.Prefix == Prefixes.None) return true;
            if (allowed.ContainsKey(node.Type)) return allowed[node.Type].Contains(node.Prefix);
            return false;
        }
    }
}
