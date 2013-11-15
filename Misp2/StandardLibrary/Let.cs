using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void LetFunction(FunctionSet set)
        {
            set.AddBuiltin("let", (node, functions) =>
                {
                    var variables = node.Children[1].Children;
                    var bodyCode = Compiler.Compile(node.Children[2], set);

                    var r = new InstructionList();
                    
                    foreach (var v in variables)
                    {
                        r.AddInstruction(
                            new InPlace(Compiler.Compile(v.Children[1], set)),
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
