using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void LambdaFunctions(FunctionSet set)
        {
            set.AddBuiltin("lambda", (node, functions) =>
                {
                    var argumentList = new List<String>();
                    foreach (var item in node.Children[1].Children)
                        argumentList.Add(item.Token);

                    return new InstructionList(
                        "LAMBDA NEXT NEXT PUSH", //Creates a new lambda function object that captures the current scope.
                        argumentList,
                        Compiler.Compile(node.Children[2], functions));
                });
        }
    }
}
