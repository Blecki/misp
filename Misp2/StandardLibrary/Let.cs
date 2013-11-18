using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void LetFunction(Environment environment)
        {
            environment.AddCoreFunction(
                "let",
                "Assign values to names, and clean them up afterwards.",
                Arguments("variables", "A list of name-value pairs. Each name is assigned " + 
                "the corrosponding value for the duration of the function.",
                        "code", "The code to execute once variables are assigned."),
                (node, functions) =>
                {
                    var variables = node.Children[1].Children;
                    var bodyCode = Compiler.Compile(node.Children[2], environment.CompileContext);

                    var r = new InstructionList();
                    
                    foreach (var v in variables)
                    {
                        r.AddInstruction(
                            new InPlace(Compiler.Compile(v.Children[1], environment.CompileContext)),
                            "PUSH_VARIABLE POP NEXT", v.Children[0].Token);
                    }
                    
                    r.AddInstruction(new InPlace(bodyCode));

                    foreach (var v in variables)
                    {
                        r.AddInstruction("POP_VARIABLE NEXT", v.Children[0].Token);
                    }

                    return r;
                });
        }
    }
}
