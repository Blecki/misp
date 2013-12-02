using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void MapFunctions(Environment environment)
        {
            environment.AddCoreFunction(
                "map",
                "Transform one list into another by invoking code for each element.",
                Arguments("variable-name", "The name of the variable that will hold the item in the interior code.",
                        "list", "A list of items to be transformed.",
                        "code", "The code to transform a single item."),
                (node, functions) =>
                {
                    return new InstructionList(
                        new InPlace(Compiler.Compile(node.Children[2], functions)),
                        "PUSH_VARIABLE PEEK NEXT", "__list", //Store the list of items in scope             [L]
                        "LENGTH POP PUSH",                                                                //[L*]
                        "PUSH_VARIABLE POP NEXT", "__total", //Total elements in list                      []
                        "PUSH_VARIABLE NEXT NEXT", 0, "__counter", 
                        "EMPTY_LIST PUSH",                                                                //[R]
                        "PUSH_VARIABLE POP NEXT", "__result",                                             //[]
                        "BRANCH PUSH NEXT",       //LOOP BRANCH                                           //[M]
                            new InstructionList(
                                "LOOKUP NEXT PUSH", "__total",
                                "LOOKUP NEXT PUSH", "__counter",                                          //[M C]
                                "GREATER_EQUAL POP POP PUSH",                                             //[M B]
                                "IF_TRUE POP",  //If counter is 0, stop looping.                          //[M]
                                "BRANCH PUSH NEXT",                                                       //[M M]
                                    new InstructionList(
                                        "MOVE POP NONE",    //Remove inner most MARK from stack           //[M]
                                        "BREAK POP"),       //Break to LOOP BRANCH                        //[]
                                "LOOKUP NEXT PUSH", "__list",                                             //[M L]
                                "LOOKUP NEXT PUSH", "__counter",                                          //[M L C]
                                "INDEX POP POP PUSH",                                                     //[M O]
                                "PUSH_VARIABLE POP NEXT", node.Children[1].Token,                         //[M]
                                "LOOKUP NEXT PUSH", "__result",                                           //[M R]
                                new InPlace(Compiler.Compile(node.Children[3], functions)),               //[M R O]
                                "APPEND POP POP PUSH",                                                   //[M R]
                                "SET_VARIABLE POP NEXT", "__result",                                      //[M]
                                "POP_VARIABLE NEXT", node.Children[1].Token,                        
                                "LOOKUP NEXT PUSH", "__counter", //Increment the counter
                                "INCREMENT POP PUSH",
                                "SET_VARIABLE POP NEXT", "__counter",
                                "CONTINUE POP"),                                                          //[]
                        "LOOKUP NEXT PUSH", "__result",                                                   //[R]
                        "POP_VARIABLE NEXT", "__result",
                        "POP_VARIABLE NEXT", "__counter",
                        "POP_VARIABLE NEXT", "__list",
                        "POP_VARIABLE NEXT", "__total"
                        );
                });
        }
    }
}
