using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        public static void SerializeCode(System.IO.TextWriter to, ScriptObject root)
        {
            if (root.gsp("@type") == "string")
                to.Write(root.gsp("@prefix") + "\"" + root.gsp("@token") + "\"");
            else if (root.gsp("@type") == "stringexpression")
            {
                to.Write(root.gsp("@prefix"));
                to.Write("\"");
                foreach (var item in root._children)
                {
                    if ((item as ScriptObject).gsp("@type") == "string")
                        to.Write((item as ScriptObject).gsp("@token"));
                    else
                        SerializeCode(to, item as ScriptObject);
                }
                to.Write("\"");
            }
            else if (root.gsp("@type") == "node")
            {
                to.Write(root.gsp("@prefix") + "(");
                foreach (var item in root._children)
                {
                    SerializeCode(to, item as ScriptObject);
                    to.Write(" ");
                }
                to.Write(")");
            }
            else if (root.gsp("@type") == "memberaccess")
            {
                SerializeCode(to, root._child(0) as ScriptObject);
                to.Write(root.gsp("@token"));
                SerializeCode(to, root._child(1) as ScriptObject);
            }
            else if (root.gsp("@type") == "dictionaryentry")
            {
                to.Write(root.gsp("@prefix") + "[");
                foreach (var item in root._children)
                {
                    SerializeCode(to, item as ScriptObject);
                    to.Write(" ");
                }
                to.Write("]");
            }
            else
            {
                to.Write(root.gsp("@token"));
            }
        }
    }
}
