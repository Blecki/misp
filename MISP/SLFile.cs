using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupFileFunctions()
        {
            var file_functions = new GenericScriptObject();

            file_functions.SetProperty("open", Function.MakeSystemFunction("open",
                Arguments.Args("file-name", "mode"), "Opens a file",
                (context, arguments) =>
                {
                    var mode = AutoBind.StringArgument(arguments[1]).ToUpperInvariant();
                    if (mode == "READ")
                        return System.IO.File.OpenText(AutoBind.StringArgument(arguments[0]));
                    else if (mode == "WRITE")
                        return System.IO.File.CreateText(AutoBind.StringArgument(arguments[0]));
                    else if (mode == "APPEND")
                        return System.IO.File.AppendText(AutoBind.StringArgument(arguments[0]));
                    else
                        context.RaiseNewError("Invalid mode specifier", context.currentNode);
                    return null;
                }));

            file_functions.SetProperty("close", Function.MakeSystemFunction("close",
                Arguments.Args("file"), "Close a file.",
                (context, arguments) =>
                {
                    if (arguments[0] is System.IO.StreamReader) (arguments[0] as System.IO.StreamReader).Close();
                    else if (arguments[0] is System.IO.StreamWriter) (arguments[0] as System.IO.StreamWriter).Close();
                    else context.RaiseNewError("Argument is not a file.", context.currentNode);
                    return null;
                }));

            file_functions.SetProperty("read-line", Function.MakeSystemFunction("read-line",
                Arguments.Args("file"), "Read a line from a file.",
                (context, arguments) =>
                {
                    var file = arguments[0] as System.IO.StreamReader;
                    if (file == null)
                    {
                        context.RaiseNewError("Argument is not a read file.", context.currentNode);
                        return null;
                    }
                    return file.ReadLine();
                }));

            file_functions.SetProperty("read-all", Function.MakeSystemFunction("read-all",
                Arguments.Args("file"), "Read all of a file.",
                (context, arguments) =>
                {
                    var file = arguments[0] as System.IO.StreamReader;
                    if (file == null)
                    {
                        context.RaiseNewError("Argument is not a read file.", context.currentNode);
                        return null;
                    }
                    return file.ReadToEnd();
                }));

            file_functions.SetProperty("write", Function.MakeSystemFunction("write",
                Arguments.Args("file", "text"), "Write to a file.",
                (context, arguments) =>
                {
                    var file = arguments[0] as System.IO.StreamWriter;
                    if (file == null)
                    {
                        context.RaiseNewError("Argument is not a write file.", context.currentNode);
                        return null;
                    }
                    file.Write(ScriptObject.AsString(arguments[1]));
                    return null;
                }));

            file_functions.SetProperty("more", Function.MakeSystemFunction("more",
                Arguments.Args("file"), "Is there more to read in this file?",
                (context, arguments) =>
                {
                    var file = arguments[0] as System.IO.StreamReader;
                    if (file == null)
                    {
                        context.RaiseNewError("Argument is not a read file.", context.currentNode);
                        return null;
                    }
                    if (file.EndOfStream) return null;
                    return true;
                }));

            AddGlobalVariable("file", c => file_functions);

        }
    }
}
