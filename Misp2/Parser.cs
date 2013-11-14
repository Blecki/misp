using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ParseError : ScriptError
    {
        public int line;
        public ParseError(string msg, int line) : base(msg)
        {
            this.line = line;
        }
    }

    public class SuperBrace
    { }

    public class Parser
    {
        public static String delimeters = " \t\r\n:.)]}";
        public static bool IsWhitespace(char c)
        {
            return " \t\r\n".Contains(c);
        }

        public static bool IsHex(char c)
        {
            return "0123456789abcdefABCDEF".Contains(c);
        }

        public static void DevourWhitespace(ParseState state)
        {
            while (!state.AtEnd() && " \t\r\n".Contains(state.Next())) state.Advance();
        }

        public static ParseNode ParseToken(ParseState state)
        {
            var result = new ParseNode(NodeTypes.Token);
            var start = state.start;
            while (!state.AtEnd() && !delimeters.Contains(state.Next())) state.Advance();
            result.Token = state.source.Substring(start, state.start - start);
            if (String.IsNullOrEmpty(result.Token)) throw new ParseError("Empty token", state.currentLine);
            return result;
        }

        public static ParseNode ParseChar(ParseState state)
        {
            var result = new ParseNode(NodeTypes.Character);
            var token = "";

            state.Advance(); //skip opening '
            while (!state.AtEnd())
            {
                if (state.Next() == '\\')
                {
                    state.Advance();
                    if (state.Next() == 'n') token += "\n";
                    if (state.Next() == 't') token += "\t";
                    if (state.Next() == 'r') token += "\r";
                    else token += state.Next();
                }
                else if (state.Next() == '\'')
                {
                    result.Token = token;
                    state.Advance();
                    return result;
                }
                else
                {
                    token += state.Next();
                    state.Advance();
                }
            }

            result.Token = token;
            return result;
        }

        public static ParseNode ParseNumber(ParseState state)
        {
            var result = new ParseNode(NodeTypes.Number);
            bool foundDot = false;
            var start = state.start;
            var numbertype = 0;

            if (state.MatchNext("0x"))
            {
                state.Advance(2);
                numbertype = 1;
            }
            else if (state.MatchNext("0b"))
            {
                state.Advance(2);
                numbertype = 2;
            }
            else if (state.Next() == '-')
            {
                state.Advance();
            }

            while (!state.AtEnd())
            {
                if (numbertype == 0 && state.Next() >= '0' && state.Next() <= '9')
                {
                    state.Advance();
                    continue;
                }
                else if (numbertype == 1 && IsHex(state.Next()))
                {
                    state.Advance();
                    continue;
                }
                else if (numbertype == 2 && (state.Next() == '0' || state.Next() == '1'))
                {
                    state.Advance();
                    continue;
                }
                else if (numbertype == 0 && state.Next() == '.')
                {
                    if (foundDot) break;
                    foundDot = true;
                    state.Advance();
                    continue;
                }
                break;
            }

            result.Token = state.source.Substring(start, state.start - start);
            return result;
        }

        private class AccessChainNode
        {
            public ParseNode node;
            public String token;
        }

        public static ParseNode ReorderMemberAccessNode(ParseNode node)
        {
            //Convert (A (B (C D))) to (((A B) C) D)

            //Create an (A B C D) list.
            var nodeList = new LinkedList<AccessChainNode>();
            for (var n = node; n != null; n = (n.Type == NodeTypes.MemberAccess) ? n.Children[1] : null)
            {
                if (n.Type == NodeTypes.MemberAccess)
                    nodeList.AddLast(new AccessChainNode { node = n.Children[0], token = n.Token });
                else
                    nodeList.AddLast(new AccessChainNode { node = n, token = "" });
            }

            //Each iteration, take the first two nodes and combine them into a new member access node.
            //(A B C D) becomes ((A B) C D), etc.
            while (nodeList.Count > 1)
            {
                var lhs = nodeList.First();
                nodeList.RemoveFirst();
                var rhs = nodeList.First();
                nodeList.RemoveFirst();

                var newNode = new ParseNode(NodeTypes.MemberAccess);
                newNode.Token = lhs.token;
                newNode.Children.AddMany(lhs.node, rhs.node);

                nodeList.AddFirst(new AccessChainNode
                {
                    node = newNode,
                    token = rhs.token
                });
            }

            return nodeList.First().node;
        }

        public static Prefixes ParsePrefix(ParseState state)
        {
            var next = state.Next();
            var r = Prefixes.None;

            if (next == '*') r = Prefixes.AsLiteral;
            else if (next == '$') r = Prefixes.ExpandInPlace;
            else if (next == '^') r = Prefixes.AsList;
            else if (next == '.') r = Prefixes.Lookup;
            else if (next == ':') r = Prefixes.Evaluate;

            if (r != Prefixes.None) state.Advance();
            return r;
        }

        public static ParseNode ParseExpression(ParseState state)
        {
            ParseNode result = null;

            var savePrefix = state.Next();
            var prefix = ParsePrefix(state);

            if (state.Next() == '"')
                result = ParseBasicString(state);
            else if (state.Next() == '(')
                result = ParseNode(state);
            else if (state.Next() == '\'')
                result = ParseChar(state);
            else if ("-0123456789".Contains(state.Next()))
            {
                result = ParseNumber(state);
                //A lone - sign is not a valid number. Interpret it as a token.
                if (result.Token == "-") result.Type = NodeTypes.Token;
            }
            else
            {
                if (delimeters.Contains(state.Next()) && prefix != Prefixes.None)
                {
                    //The prefix is a token.
                    result = new ParseNode(NodeTypes.Token);
                    result.Token = new String(savePrefix, 1);
                    prefix = Prefixes.None;
                }
                else
                    result = ParseToken(state);
            }
             
            if (!state.AtEnd() && (state.Next() == '.' || state.Next() == ':'))
            {
                var final_result = new ParseNode(NodeTypes.MemberAccess);
                final_result.Children.Add(result);
                final_result.Token = new String(state.Next(), 1);;
                state.Advance();
                final_result.Children.Add(ParseExpression(state));
                result = final_result;
            }

            result.Prefix = prefix;
            if (!PrefixCheck.CheckPrefix(result)) 
                throw new ParseError("Illegal prefix on expression of type " + result.Type, state.currentLine);
            return result;
        }
                        


        public static ParseNode ParseNode(ParseState state, String start = "(", String end = ")")
        {
            var result = new ParseNode(NodeTypes.Node);
            if (!state.MatchNext(start)) throw new ParseError("Expected " + start, state.currentLine);
            state.Advance(start.Length);
            while (!state.AtEnd() && !state.MatchNext(end))
            {
                DevourWhitespace(state);
                if (state.Next() == '}') //Super-brace
                    return result; 
                if (!state.AtEnd() && !state.MatchNext(end))
                {
                    var expression = ParseExpression(state);
                    if (expression.Type == NodeTypes.MemberAccess) expression = ReorderMemberAccessNode(expression);
                    result.Children.Add(expression);
                }
                DevourWhitespace(state);
            }
            if (end != null) state.Advance(end.Length);
            return result;
        }

        public static ParseNode ParseBasicString(ParseState state)
        {
            var result = new ParseNode(NodeTypes.String);
            state.Advance(); //skip quote
            var piece_start = state.start;
            while (!state.AtEnd())
            {
                if (state.Next() == '\\')
                {
                    state.Advance(); //skip the slash.
                    state.Advance();
                }
                else if (state.Next() == '"')
                {
                    result.Token = state.source.Substring(piece_start, state.start - piece_start);
                    state.Advance();
                    return result;
                }
                else
                {
                    state.Advance();
                }
            }

            throw new ParseError("Unexpected end of script inside string.", state.currentLine);
        }

        public static int ParseComment(ParseState state)
        {
            var start = state.start;
            state.Advance(2);
            while (!state.AtEnd() && !state.MatchNext("*/")) state.Advance();
            state.Advance(2);
            return state.start - start;
        }

        public static ParseNode ParseRoot(String script, String filename)
        {
            var commentFree = new StringBuilder();
            var state = new ParseState { start = 0, end = script.Length, source = script, filename = filename };
            while (!state.AtEnd())
            {
                if (state.MatchNext("\\/*"))
                {
                    commentFree.Append("\\/*");
                    state.Advance(3);
                }
                else if (state.MatchNext("/*")) 
                    commentFree.Append(new String(' ',ParseComment(state)));
                else
                {
                    commentFree.Append(state.Next());
                    state.Advance();
                }
            }

            var r = ParseNode(
                new ParseState { start = 0, end = commentFree.Length, source = commentFree.ToString(), filename = filename },
                "", null);
            if (r.Children.Count == 1) return r.Children[0];
            return r;
        }
                
    }
}
