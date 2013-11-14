using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class StandardLibrary
    {
        public static void MapFunctions(FunctionSet set)
        {
            set.AddBuiltin("map", (node, functions) =>
                {
                    return new InstructionList(
                        new InPlace(Compiler.Compile(node.Children[2], functions)),
                        "PUSH_VARIABLE PEEK NEXT", "__list",
                        "LENGTH POP PUSH",
                        "PUSH_VARIABLE POP NEXT", "__counter",
                        "EMPTY_LIST PUSH",
                        "PUSH_VARIABLE POP NEXT", "__result",
                        "BEGIN_LOOP PUSH",
                            "LOOKUP NEXT PUSH", "__counter",
                            "DECREMENT POP PUSH",
                            "SET_VARIABLE PEEK NEXT", "__counter",
                            "LESS POP NEXT PUSH", 0,
                            "IF_TRUE POP",
                                "BRANCH PUSH NEXT",
                                    new InstructionList(
                                        "CLEANUP NEXT", 1,
                                        "BREAK POP"),
                            "LOOKUP NEXT PUSH", "__list",
                            "LOOKUP NEXT PUSH", "__counter",
                            "INDEX POP POP PUSH",
                            "PUSH_VARIABLE POP NEXT", node.Children[1].Token,
                            "LOOKUP NEXT PUSH", "__result",
                            new InPlace(Compiler.Compile(node.Children[3], functions)),
                            "PREPEND POP POP PUSH",
                            "SET_VARIABLE POP NEXT", "__result",
                            "POP_VARIABLE NEXT", node.Children[1].Token,
                            "CONTINUE POP",
                        "LOOKUP NEXT PUSH", "__result",
                        "POP_VARIABLE NEXT", "__result",
                        "POP_VARIABLE NEXT", "__counter",
                        "POP_VARIABLE NEXT", "__list"
                        );
                });
        }
    }
}
