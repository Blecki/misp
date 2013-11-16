using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Compiler
    {
        public static InstructionList Compile(ParseNode node, FunctionSet builtIns)
        {
            var r = new InstructionList();

            switch (node.Type)
            {
                case NodeTypes.String:
                    r.AddInstruction("MOVE NEXT PUSH", node.Token);
                    break;
                case NodeTypes.Number:
                    r.AddInstruction("MOVE NEXT PUSH", ParseNumber(node.Token));
                    break;
                case NodeTypes.Character:
                    r.AddInstruction("MOVE NEXT PUSH", node.Token[0]);
                    break;
                case NodeTypes.Token:
                    r.AddInstruction("LOOKUP NEXT PUSH", node.Token);
                    break;
                case NodeTypes.MemberAccess:
                    r.AddRange(Compile(node.Children[0], builtIns));
                    r.AddRange(Compile(node.Children[1], builtIns));
                    r.AddInstruction("MEMBER_LOOKUP POP POP PUSH");
                    //TODO: And evaluate
                    break;
                case NodeTypes.Node:
                    if (node.Prefix == Prefixes.AsList)
                    {
                        r.AddInstruction("EMPTY_LIST PUSH");
                        foreach (var child in node.Children)
                        {
                            r.AddRange(Compile(child, builtIns));
                            if (child.Prefix == Prefixes.ExpandInPlace)
                                r.AddInstruction("APPEND_RANGE POP PEEK");
                            else
                                r.AddInstruction("APPEND POP PEEK");
                        }
                    }
                        //Todo: Implement AsLiteral prefix
                    else
                    {
                        if (node.Children.Count == 0) break;
                        if (node.Children[0].Type == NodeTypes.Token)
                        {
                            if (builtIns.ContainsKey(node.Children[0].Token))
                            {
                                r.AddRange(builtIns[node.Children[0].Token].EmitOpcode(node, builtIns));
                                break;
                            }
                        }
                        r.AddInstruction("EMPTY_LIST PUSH");
                        foreach (var child in node.Children)
                        {
                            r.AddRange(Compile(child, builtIns));
                            if (child.Prefix == Prefixes.ExpandInPlace)
                                r.AddInstruction("APPEND_RANGE POP PEEK");
                            else
                                r.AddInstruction("APPEND POP PEEK");
                        }
                        r.AddInstruction("INVOKE POP");
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (node.Prefix == Prefixes.Lookup)
                r.AddInstruction("LOOKUP POP PUSH");

            //todo: Evaluate prefix

            return r;
        }

        private static Object ParseNumber(String str)
        {
            if (str.Contains('.')) return Convert.ToSingle(str);
            else
            {
                if (str.StartsWith("0x"))
                    return Convert.ToInt32(str.Substring(2), 16);
                else if (str.StartsWith("0b"))
                {
                    var accumulator = 0;
                    foreach (var c in str.Substring(2))
                    {
                        accumulator <<= 1;
                        if (c == '1') accumulator += 1;
                    }
                    return accumulator;
                }
                else
                    return Int32.Parse(str);
            }
        }
    }
}
