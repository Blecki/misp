using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ParseError : ScriptError
    {
        public int line;
        public ParseError(string msg, int line)
            : base(msg, null)
        {
            this.line = line;
        }
    }

    public class SuperBrace
    { }

    public class Parser
    {
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

        public static int asInt(Object obj)
        {
            var r = obj as int?;
            if (r.HasValue) return r.Value;
            return 0;
        }

        public static Object index(Object obj, int i)
        {
            if (obj is ScriptList) return (obj as ScriptList)[i];
            return null;
        }

        public static ScriptObject ParseToken(ParseState state)
        {
            var result = new GenericScriptObject("@type", "token", "@start", state.start, "@source", state);
            while (!state.AtEnd() && !(" \t\r\n:.)]}".Contains(state.Next()))) state.Advance();
            result["@end"] = state.start;
            result["@token"] = state.source.Substring(asInt(result["@start"]),
                asInt(result["@end"]) - asInt(result["@start"]));
            if (String.IsNullOrEmpty(result.gsp("@token"))) throw new ParseError("Empty token", state.currentLine);
            return result;
        }

        public static ScriptObject ParseChar(ParseState state)
        {
            var result = new GenericScriptObject("@type", "char", "@start", state.start, "@source", state);
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
                    result["@end"] = state.start;
                    result["@token"] = token;
                    state.Advance();
                    return result;
                }
                else
                {
                    token += state.Next();
                    state.Advance();
                }
            }

            result["@end"] = state.start;
            result["@token"] = token;
            return result;
        }
        public static ScriptObject ParseNumber(ParseState state)
        {
            var result = new GenericScriptObject("@type", "number", "@start", state.start, "@source", state);
            bool foundDot = false;
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
            
            result["@end"] = state.start;
            result["@token"] = state.source.Substring(asInt(result["@start"]), asInt(result["@end"]) - asInt(result["@start"]));
            return result;
        }

        private class AccessChainNode
        {
            public Object node;
            public Object token;
        }

        private static bool isType(ScriptObject obj, String type)
        {
            return obj.gsp("@type") == type;
        }

        private static Object child(ScriptObject obj, int index)
        {
            return (obj["@children"] as ScriptList)[index];
        }

        private static ScriptList children(ScriptObject obj)
        {
            var list = obj["@children"];
            if (list == null)
            {
                list = new ScriptList();
                obj["@children"] = list;
            }

            return list as ScriptList;
        }

        public static ScriptObject ReorderMemberAccessNode(ScriptObject node)
        {
            //Convert (A (B (C D))) to (((A B) C) D)

            //Create an (A B C D) list.
            var nodeList = new LinkedList<AccessChainNode>();
            for (var n = node; n != null; n = (isType(n, "memberaccess") ? child(n,1) as ScriptObject : null))
            {
                if (isType(n, "memberaccess"))
                    nodeList.AddLast(new AccessChainNode { node = child(n,0), token = n["@token"] });
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

                var newNode = new GenericScriptObject("@type", "memberaccess", "@start", (lhs.node as ScriptObject)["@start"],
                    "@source", (lhs.node as ScriptObject)["@source"]);
                newNode["@token"] = lhs.token;
                children(newNode).Add(lhs.node);
                children(newNode).Add(rhs.node);

                nodeList.AddFirst(new AccessChainNode
                {
                    node = newNode,
                    token = rhs.token
                });
            }

            return nodeList.First().node as ScriptObject;
        }

        public static String ParsePrefix(ParseState state)
        {
            var next = state.Next();
            if (next == '*' || next == '^' || next == '$' || next == '.' || next == ':')
            {
                state.Advance();
                return new String(next, 1);
            }
            return "";
        }

        public static ScriptObject ParseExpression(ParseState state)
        {
            ScriptObject result = null;
            var prefix = ParsePrefix(state);
            if (state.Next() == '"')
            {
                if (prefix == "$")
                {
                    prefix = "";
                    result = ParseBasicString(state);
                }
                else
                    result = ParseStringExpression(state);
            }
            else if (state.Next() == '(')
            {
                result = ParseNode(state);
            }
            else if (state.Next() == '\'')
            {
                result = ParseChar(state);
            }
            else if ("-0123456789".Contains(state.Next()))
            {
                result = ParseNumber(state);
                if (result["@token"].ToString() == "-") //A lone - sign is not a valid number. Interpret it as a token.
                    result["@type"] = "token";
            }
            else
            {
                if (" \t\r\n:.)}".Contains(state.Next())
                    && !String.IsNullOrEmpty(prefix))
                {
                    //The prefix is a token.
                    result = new GenericScriptObject("@type", "token", "@start", state.start - 1, "@source", state);
                    result["@end"] = state.start;
                    result["@token"] = prefix;
                    prefix = "";
                }
                else
                    result = ParseToken(state);
            }
             
            if (!state.AtEnd() && (state.Next() == '.' || state.Next() == ':'))
            {
                var final_result = new GenericScriptObject("@type", "memberaccess", "@start", result["@start"],
                    "@source", state);
                children(final_result).Add(result);
                final_result["@token"] = new String(state.Next(), 1);
                state.Advance();
                children(final_result).Add(ParseExpression(state));
                result = final_result;
            }

            result["@prefix"] = prefix;
            if (!PrefixCheck.CheckPrefix(result)) 
                throw new ParseError("Illegal prefix on expression of type " + result["@type"], state.currentLine);
            return result;
        }
                        


        public static ScriptObject ParseNode(ParseState state, String start = "(", String end = ")")
        {
            var result = new GenericScriptObject("@type", "node", "@start", state.start, "@source", state);
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
                    if (isType(expression, "memberaccess")) expression = ReorderMemberAccessNode(expression);
                    children(result).Add(expression);
                }
                DevourWhitespace(state);
            }
            if (end != null) state.Advance(end.Length);
            return result;
        }

        public static ScriptObject ParseBasicString(ParseState state)
        {
            var result = new GenericScriptObject("@type", "string", "@start", state.start, "@source", state);
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
                    result["@token"] = state.source.Substring(piece_start, state.start - piece_start);
                    state.Advance();
                    result["@end"] = state.start;
                    return result;
                }
                else
                {
                    state.Advance();
                }
            }

            throw new ParseError("Unexpected end of script inside string expression.", state.currentLine);
        }

        public static ScriptObject ParseStringExpression(ParseState state, bool isRoot = false)
        {
            var result = new GenericScriptObject("@type", "stringexpression", "@start", state.start, "@source", state);
            if (!isRoot) state.Advance(); //Skip opening quote
            string piece = "";
            int piece_start = state.start;
            while (!state.AtEnd())
            {
                if (state.Next() == '}' && piece.Length == 0)
                {
                    state.Advance(1);
                }
                else if (state.Next() == '(') 
                {
                    if (piece.Length > 0) children(result).Add(
                        new GenericScriptObject("@type", "string", "@start", piece_start, "@source", state,
                            "@token", state.source.Substring(piece_start, state.start - piece_start)));
                    children(result).Add(ParseNode(state));
                    piece = "";
                }
                else if (state.Next() == '\\')
                {
                    if (piece.Length == 0) piece_start = state.start;
                    state.Advance(); //skip the slash.
                    piece += state.Next();
                    state.Advance();
                }
                else if (!isRoot && state.Next() == '"') 
                {
                    if (piece.Length > 0) children(result).Add(
                        new GenericScriptObject("@type", "string", "@start", piece_start, "@source", state,
                            "@token", state.source.Substring(piece_start, state.start - piece_start)));
                    state.Advance();
                    result["@end"] = state.start;
                    if (children(result).Count == 1 && isType(children(result)[0] as ScriptObject, "string"))
                        return child(result, 0) as ScriptObject;
                    return result;
                }
                else
                {
                    if (piece.Length == 0) piece_start = state.start;
                    piece += state.Next();
                    state.Advance();
                }

            }

            if (isRoot)
            {
                if (piece.Length > 0) children(result).Add(new GenericScriptObject("@type", "string", "@start", piece_start,
                    "@source", state, "@token", state.source.Substring(piece_start, state.start - piece_start)));
                if (children(result).Count == 1) return child(result, 0) as ScriptObject;
                return result;
            }
            
            throw new ParseError("Unexpected end of script inside string expression.", state.currentLine);
        }

        public static int ParseComment(ParseState state)
        {
            var start = state.start;
            state.Advance(2);
            while (!state.AtEnd() && !state.MatchNext("*/")) state.Advance();
            state.Advance(2);
            return state.start - start;
        }

        public static ScriptObject ParseRoot(String script, String filename)
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
            if (r._children.Count == 1) return r._child(0) as ScriptObject;
            r.SetProperty("@type", "root");
            return r;
        }
                
    }
}
