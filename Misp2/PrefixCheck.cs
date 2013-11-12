using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    internal static class PrefixCheck
    {
        private static Dictionary<String, List<String>> allowed = null;
        internal static bool CheckPrefix(ParseNode node)
        {
            if (allowed == null)
            {
                var allTypes = new string[] { "node", "memberaccess", "string", "number", "token" };
                allowed = new Dictionary<string, List<string>>();
                foreach (var type in allTypes) allowed.Add(type, new List<string>());

                allowed["node"].Add("$");
                allowed["node"].Add("^");
                allowed["node"].Add("*");
                allowed["node"].Add("#");
                allowed["node"].Add(":");

                allowed["token"].Add("$");
                allowed["token"].Add("#");
                allowed["token"].Add(":");

                allowed["string"].Add("*");
                allowed["string"].Add(":");
            }

            if (String.IsNullOrEmpty(node.Prefix)) return true;
            if (allowed.ContainsKey(node.Type)) return allowed[node.Type].Contains(node.Prefix);
            return false;
        }
    }
}
