using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void MathFunctions(Environment environment)
        {
            environment.AddCoreFunction(
                "+",
                "Add values together",
                Arguments("values", "One or more values to add"),
                (node, functions) =>
                {
                    var r = new InstructionList();

                    r.AddRange(Compiler.Compile(node.Children[1], functions));

                    for (var i = 2; i < node.Children.Count; ++i)
                    {
                        r.AddRange(Compiler.Compile(node.Children[i], functions));
                        r.AddInstruction("ADD POP POP PUSH");
                    }

                    return r;
                });

            environment.AddCoreFunction(
                "-",
                "Subtract values",
                Arguments("values", "One or more values to add"),
                (node, functions) =>
                {
                    var r = new InstructionList();

                    r.AddRange(Compiler.Compile(node.Children[1], functions));

                    for (var i = 2; i < node.Children.Count; ++i)
                    {
                        r.AddRange(Compiler.Compile(node.Children[i], functions));
                        r.AddInstruction("SUBTRACT POP POP PUSH");
                    }

                    return r;
                });

            environment.AddCoreFunction(
                "*",
                "Multiply values together",
                Arguments("values", "One or more values to add"),
                (node, functions) =>
                {
                    var r = new InstructionList();

                    r.AddRange(Compiler.Compile(node.Children[1], functions));

                    for (var i = 2; i < node.Children.Count; ++i)
                    {
                        r.AddRange(Compiler.Compile(node.Children[i], functions));
                        r.AddInstruction("MULTIPLY POP POP PUSH");
                    }

                    return r;
                });

            environment.AddCoreFunction(
                "/",
                "Divide A by B.",
                Arguments("A", "The dividend", "B", "The divisor"),
                (node, functions) =>
                {
                    var r = new InstructionList();

                    r.AddRange(Compiler.Compile(node.Children[2], functions));
                    r.AddRange(Compiler.Compile(node.Children[1], functions));
                    r.AddInstruction("DIVIDE POP POP PUSH");

                    return r;
                });
        }
    }
}
