using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void ImperativeFunctions(Environment environment)
        {
            environment.AddCoreFunction(
                "return-last",
                "Result is the last of it's arguments.",
                Arguments("nodes", "1 or more sub expressions."),
                (node, functions) =>
                {
                    var r = new InstructionList();

                    for (int i = 0; i < node.Children.Count - 1; ++i)
                    {
                        r.AddRange(Compiler.Compile(node.Children[i + 1], functions));
                        if (i != node.Children.Count - 2) r.AddInstruction("MOVE POP NONE");
                    }

                    return r;
                });
        }
    }
}
