using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void ExceptionFunctions(FunctionSet set)
        {
            set.AddBuiltin("catch", (node, functions) =>
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

            set.AddBuiltin("throw", (node, functions) =>
                {
                    return new InstructionList(
                        new InPlace(Compiler.Compile(node.Children[1], functions)),
                        "THROW POP");
                });
        }
    }
}
