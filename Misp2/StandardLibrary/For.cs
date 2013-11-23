using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void ForFunction(Environment environment)
        {
            environment.AddCoreFunction(
                "for",
                "Create a list by invoking the code with a value ranging from START to END",
                Arguments("variable-name", "The name of the variable that will hold the item in the interior code.",
                        "start", "The starting value",
                        "end-condition", "If true, keep running",
                        "increment", "Ran every loop to update value of counter",
                        "code", "The code to invoke each iteration."),
                (node, functions) =>
                {
                    var counterName = node.Children[1].Token;
                    var startingValue = Compiler.Compile(node.Children[2], functions);
                    var condition = Compiler.Compile(node.Children[3], functions);
                    var increment = Compiler.Compile(node.Children[4], functions);
                    var body = Compiler.Compile(node.Children[5], functions);

                    return new InstructionList(
                        "EMPTY_LIST PUSH",
                        "PUSH_VARIABLE POP NEXT", "__result",
                        new InPlace(startingValue),
                        "PUSH_VARIABLE POP NEXT", counterName,
                        "MARK PUSH",
                        new InPlace(condition),
                        "IF_FALSE POP",
                        "SKIP NEXT", 1,
                            "BRANCH PUSH NEXT",
                            new InstructionList(
                                "MOVE POP NONE", //Clean branch MARK off stack
                                "LOOKUP NEXT PUSH", "__result",
                                new InPlace(body),
                                "APPEND POP POP PUSH",
                                "SET_VARIABLE POP NEXT", "__result",
                                new InPlace(increment),
                                "SET_VARIABLE POP NEXT", counterName,
                                "CONTINUE POP"
                                ),
                        "MOVE POP NONE", //Cleanup left over MARK.
                        "POP_VARIABLE NEXT", counterName,
                        "LOOKUP NEXT PUSH", "__result",
                        "POP_VARIABLE NEXT", "__result");
                });
        
        }
    }
}
