using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Compiler
    {
        public static InstructionList Compile(ParseNode node, CompileContext coreFunctions)
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
                    if (coreFunctions.CompileTimeConstants.ContainsKey(node.Token))
                        r.AddInstruction("MOVE NEXT PUSH", coreFunctions.CompileTimeConstants[node.Token].Value);
                    else
                        r.AddInstruction("LOOKUP NEXT PUSH", node.Token);
                    break;
                case NodeTypes.MemberAccess:
                    r.AddRange(Compile(node.Children[0], coreFunctions));
                    if (node.Children[1].Type == NodeTypes.Token)
                        r.AddInstruction("MOVE NEXT PUSH", node.Children[1].Token);
                    else
                        r.AddRange(Compile(node.Children[1], coreFunctions));
                    r.AddInstruction("LOOKUP_MEMBER POP POP PUSH");
                    //TODO: And evaluate
                    break;
                case NodeTypes.Node:
                    if (node.Prefix == Prefixes.AsList)
                    {
                        r.AddInstruction("EMPTY_LIST PUSH");
                        foreach (var child in node.Children)
                        {
                            r.AddRange(Compile(child, coreFunctions));
                            if (child.Prefix == Prefixes.ExpandInPlace)
                                r.AddInstruction("APPEND_RANGE POP PEEK");
                            else
                                r.AddInstruction("APPEND POP PEEK");
                        }
                    }
                    else if (node.Prefix == Prefixes.AsLiteral)
                    {
                        node.Prefix = Prefixes.None;
                        r.AddInstruction(
                            "MOVE NEXT PUSH",
                            Compile(node, coreFunctions));
                        node.Prefix = Prefixes.AsLiteral;
                    }
                    else
                    {
                        if (node.Children.Count == 0) break;
                        if (node.Children[0].Type == NodeTypes.Token)
                        {
                            if (coreFunctions.CoreFunctions.ContainsKey(node.Children[0].Token))
                            {
                                r.AddRange(coreFunctions.CoreFunctions[node.Children[0].Token].EmitOpcode(node, coreFunctions));
                                break;
                            }
                        }
                        r.AddInstruction("EMPTY_LIST PUSH");
                        foreach (var child in node.Children)
                        {
                            r.AddRange(Compile(child, coreFunctions));
                            if (child.Prefix == Prefixes.ExpandInPlace)
                                r.AddInstruction("APPEND_RANGE POP PEEK");
                            else
                                r.AddInstruction("APPEND POP PEEK");
                        }
                        r.AddInstruction("INVOKE POP");
                    }
                    break;
                case NodeTypes.StringExpression:
                    r.AddInstruction("EMPTY_STRING PUSH");
                    foreach (var child in node.Children)
                    {
                        r.AddRange(Compile(child, coreFunctions));
                        r.AddInstruction("APPEND_STRING POP POP PUSH");
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
