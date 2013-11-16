using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void LambdaFunctions(Environment environment)
        {
            environment.AddCoreFunction(
                "lambda",
                "Create a lambda function object that can be invoked.",
                Arguments("arguments", "A list of arguments that can be passed to the lambda.",
                        "code", "The body of the lambda."),
                (node, functions) =>
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
