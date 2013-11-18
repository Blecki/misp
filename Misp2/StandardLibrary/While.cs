using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void WhileFunction(Environment environment)
        {
            environment.AddCoreFunction(
                "while",
                "Repeat some code so long as the condition is true. Returns the result of the last run of the code, unless there are no runs, then it returns 0.",
                Arguments("condition", "Repeat while this is true",
                        "code", "Code to repeat"),
                (node, functions) =>
                {
                    return new InstructionList(
                        "MOVE NEXT PUSH", 0, //Reserve an empty place on the stack.                         [0]
                        "MARK PUSH",                                                                    //  [0 M]
                        new InPlace(Compiler.Compile(node.Children[1], functions)), //The condition         [0 M B]
                        "IF_FALSE POP",                                                                 //  [0 M]
                        "SKIP NEXT", 1,
                            "BRANCH PUSH NEXT",                                                         //  [0 M M]
                            new InstructionList(
                                "MOVE POP NONE", //Clean branch MARK off stack.                         //  [0 M]
                                "SWAP_TOP",                                                             //  [M 0]
                                "MOVE POP NONE", //Clean result of last iteration off stack.            //  [M]
                                new InPlace(Compiler.Compile(node.Children[2], functions)),             //  [M R]
                                "SWAP_TOP", //Continue from loop start MARK                             //  [R M]
                                "CONTINUE POP"),                                                        //  [R]
                        "MOVE POP NONE"); //Remove left over MARK from stack
                            
                });
        }
    }
}
