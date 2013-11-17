using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void SetFunction(Environment environment)
        {
            environment.AddCoreFunction(
                "set",
                "Assign a value to a member of an object.",
                Arguments("object", "The object to set the member of.", 
                "member", "The name of the member.", 
                "value", "The new value of the member."),
                (node, functions) =>
                {
                    return new InstructionList(
                        new InPlace(Compiler.Compile(node.Children[3], functions)), //The value
                        "MOVE PEEK PUSH",   //Duplicate value so we can leave it on the stack.
                        new InPlace(Compiler.Compile(node.Children[1], functions)),  //The object
                        "SWAP_TOP", 
                        new InPlace(Compiler.Compile(node.Children[2], functions)), //The name
                        "SWAP_TOP",
                        "SET_MEMBER POP POP POP");
                });
        }
    }
}
