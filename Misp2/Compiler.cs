using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Compiler
    {
        public static InstructionList CompileFunction(ParseNode node, FunctionSet builtIns)
        {
            var arguments = new List<String>();
            foreach (var argument in node.Children[1].Children)
                arguments.Add(argument.Token);

            return new InstructionList(
                "LAMBDA_PREAMBLE POP NEXT", arguments,
                new InPlace(Compile(node.Children[2], builtIns)),
                "BREAK POP");
        }

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

        public static void DumpOpcode(List<Object> opcode, System.IO.StreamWriter to, int indent = 0)
        {
            foreach (var item in opcode)
            {
                to.Write(new String(' ', indent * 4));
                if (item == null) to.Write("NULL\n");
                else if (item is List<String>)
                {
                    to.Write("[ ");
                    foreach (var entry in item as List<String>) to.Write(entry + " ");
                    to.Write("]\n");
                }
                else if (item is List<Object>)
                {
                    to.Write("--- Embedded instruction stream\n");
                    DumpOpcode(item as List<Object>, to, indent + 1);
                    to.Write(new String(' ', indent * 4));
                    to.Write("--- End embedded stream\n");
                }
                else if (item is String)
                    to.Write("\"" + item.ToString() + "\"\n");
                else to.Write(item.ToString() + "\n");
            }
        }
    }
}
