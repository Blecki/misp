using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void RecordFunction(Environment environment)
        {
            environment.AddCoreFunction(
                "record",
                "Create a record from a set of named values",
                Arguments("values", "Any number of name-value pairs."),
                (node, functions) =>
                {
                    var r = new InstructionList("RECORD PUSH");

                    foreach (var child in node.Children.GetRange(1, node.Children.Count - 1))
                    {
                        r.AddInstruction(
                            "MOVE NEXT PUSH", child.Children[0].Token,
                            new InPlace(Compiler.Compile(child.Children[1], functions)),
                            "SET_MEMBER POP POP PEEK");
                    }

                    return r;
                });
        }
    }
}
