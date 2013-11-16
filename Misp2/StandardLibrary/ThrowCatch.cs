using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void ExceptionFunctions(Environment environment)
        {
            environment.AddCoreFunction(
                "catch",
                @"Catch an error. The first argument is executed. If an error occurs, the second argument is executed.",
                Arguments("code", "The code to catch errors from.",
                          "handler", "The code to run when an error occurs."),
                (node, functions) =>
                {
                    return new InstructionList(
                        "CATCH NEXT NEXT",
                        new InstructionList(
                            new InPlace(Compiler.Compile(node.Children[2], functions)),
                            "SWAP_TOP",
                            "POP_VARIABLE NEXT", "error", //Cleanup the thrown object
                            "BREAK POP"), //Return us to normal control flow after an exception is handled.
                        new InstructionList(
                            new InPlace(Compiler.Compile(node.Children[1], functions)),
                            "SWAP_TOP", //Swap result and catch context
                            "CLEANUP NEXT", 1, //Cleanup catch context
                            "SWAP_TOP", //Swap result and return point
                            "BREAK POP")); 
                       
                });

            environment.AddCoreFunction(
                "throw", 
                "Throw an error that can be caught by a catch higher in the call stack.",
                Arguments("object", "The error object to throw."),
                (node, functions) =>
                {
                    return new InstructionList(
                        new InPlace(Compiler.Compile(node.Children[1], functions)),
                        "THROW POP");
                });
        }
    }
}
