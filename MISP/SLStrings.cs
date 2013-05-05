using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupStringFunctions()
        {
            AddFunction("split", "Split a string into pieces", (context, arguments) =>
                {
                    var pieces = AutoBind.StringArgument(arguments[0]).Split(
                        new String[] { AutoBind.StringArgument(arguments[1]) }, 
                        Int32.MaxValue, StringSplitOptions.RemoveEmptyEntries);
                    var r = new ScriptList(pieces);
                    return r;
                }, Arguments.Arg("string"), Arguments.Arg("split-chars"));

            AddFunction("strlen", "string : Returns length of string.",
                (context, arguments) =>
                {
                    return ScriptObject.AsString(arguments[0]).Length;
                }, Arguments.Arg("string"));

            AddFunction("strind",
                "string n : Returns nth element in string.",
                (context, arguments) =>
                {
                    var index = arguments[1] as int?;
                    if (index == null || !index.HasValue) return null;
                    if (index.Value < 0) return null;
                    var str = ScriptObject.AsString(arguments[0]);
                        if (index.Value >= str.Length) return null;
                        return str[index.Value];
                },
                Arguments.Arg("string"),
                Arguments.Arg("n"));

            AddFunction("substr", "Returns a portion of the input string.",
                (context, arguments) =>
                {
                    var str = ScriptObject.AsString(arguments[0]);
                    var start = AutoBind.IntArgument(arguments[1]);
                    if (arguments[2] == null) return str.Substring(start);
                    else return str.Substring(start, AutoBind.IntArgument(arguments[2]));
                },
                    Arguments.Arg("string"),
                    Arguments.Arg("start"),
                    Arguments.Optional("length"));

            AddFunction("strcat", "Concatenate many strings into one.",
                (context, arguments) =>
                    {
                        var r = new StringBuilder();
                        foreach (var obj in AutoBind.ListArgument(arguments[0]))
                            r.Append(ScriptObject.AsString(obj));
                        return r.ToString();
                    },
                    Arguments.Repeat("item"));

            AddFunction("itoa", "Change a number to the string representation.",
                (context, arguments) =>
                {
                    return arguments[0].ToString();
                }, 
                Arguments.Arg("i"));

            AddFunction("atoi", "",
                (context, arguments) =>
                {
                    return Convert.ToInt32(arguments[0]);
                }, 
                Arguments.Arg("i"));

            AddFunction("unescape", "", (context, arguments) =>
            {
                return Console.UnescapeString(ScriptObject.AsString(arguments[0]));
            },
            Arguments.Arg("string"));

            AddFunction("format", "Format a string.",
                (context, arguments) =>
                {
                    return String.Format(AutoBind.StringArgument(arguments[0]),
                        AutoBind.ListArgument(arguments[1]).ToArray());
                },
                Arguments.Arg("format-string"),
                Arguments.Optional(Arguments.Repeat("value")));
        }
    }
}
