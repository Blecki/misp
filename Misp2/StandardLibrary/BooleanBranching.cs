using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void BooleanBranching(Environment environment)
        {
            environment.AddCompileTimeConstant("true", true);
            environment.AddCompileTimeConstant("false", false);

            environment.AddCoreFunction(
                "=",
                "Compare values for equality",
                Arguments("A", "first value", "B", "second value"),
                (node, functions) =>
                {
                    return new InstructionList(
                        new InPlace(Compiler.Compile(node.Children[1], functions)),
                        new InPlace(Compiler.Compile(node.Children[2], functions)),
                        "EQUAL POP POP PUSH");
                });

            environment.AddCoreFunction(
                "<",
                "True if A < B",
                Arguments("A", "first value", "B", "second value"),
                (node, functions) =>
                {
                    return new InstructionList(
                        new InPlace(Compiler.Compile(node.Children[2], functions)),
                        new InPlace(Compiler.Compile(node.Children[1], functions)),
                        "LESS POP POP PUSH");
                });
            
            environment.AddCoreFunction(
                "if",
                "Consider a condition. If true, execute the first branch. If false, the second.",
                Arguments("condition", "The condition to consider",
                        "then", "Branch if true",
                        "else", "Branch if false"),
                (node, functions) =>
                {
                    var condition = Compiler.Compile(node.Children[1], functions);
                    var thenBranch = Compiler.Compile(node.Children[2], functions);
                    var elseBranch = Compiler.Compile(node.Children[3], functions);

                    return new InstructionList(
                        new InPlace(condition),
                        "IF_FALSE POP",
                        "SKIP NEXT", 2,
                        "BRANCH PUSH NEXT",
                        new InstructionList(
                            new InPlace(thenBranch),
                            "SWAP_TOP",
                            "BREAK POP"),
                        "SKIP NEXT", 1,
                        "BRANCH PUSH NEXT",
                        new InstructionList(
                            new InPlace(elseBranch),
                            "SWAP_TOP",
                            "BREAK POP"));
                });
        }
    }
}
