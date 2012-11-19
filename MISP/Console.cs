using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MISP;

namespace MISP
{
    public class Console
    {
        public Action<String> Write = (s) => { };
        public Engine mispEngine;
        public Context mispContext;
        public ScriptObject mispObject;

        public void PrettyPrint(Object what, int depth)
        {
            Write(PrettyPrint2(what, depth));
        }

        public static String PrettyPrint2(Object what, int depth)
        {
            var r = "";
            Action<String> Write = (s) => { r += s; };

            if (depth == 3)
            {
                Write(what == null ? "null" : what.ToString());
            }
            else
            {
                if (what == null)
                    Write("null");
                else if (what is ScriptList)
                {
                    var l = what as ScriptList;
                    if (l.Count > 0)
                    {
                        Write("list [" + l.Count + "] (\n");
                        foreach (var item in l)
                        {
                            Write(new String('.', depth * 3 + 1));
                            Write(PrettyPrint2(item, depth + 1));
                            Write("\n");
                        }
                        Write(new String('.', depth * 3) + ")\n");
                    }
                    else
                        Write("list [0] ()\n");
                }
                else if (what is ScriptObject)
                {
                    var o = what as ScriptObject;
                    Write("object (\n");
                    foreach (var item in o.ListProperties())
                    {
                        Write(new String('.', depth * 3 + 1) + item + ": ");
                        Write(PrettyPrint2(o.GetLocalProperty(item as String), depth + 1));
                        Write("\n");
                    }
                    Write(new String('.', depth * 3) + ")\n");
                }
                else Write(what.ToString());
            }

            return r;
        }


        public Console(Action<String> Write, bool AddEnvironmentFunctions = true)
        {
            this.Write = Write;
            Setup();
            if (AddEnvironmentFunctions) SetupEnvironmentFunctions();
        }

        public void Setup()
        {
            mispEngine = new Engine();
            mispContext = new Context();
            mispObject = new GenericScriptObject();
            mispContext.limitExecutionTime = false;
            mispContext.Scope.PushVariable("this", mispObject);

            Write("MISP Console 1.0\n");

            mispEngine.AddFunction("run-file", "Load and run a file.",
                (context, arguments) =>
                {
                    var text = System.IO.File.ReadAllText(ScriptObject.AsString(arguments[0]));
                    return mispEngine.EvaluateString(context, text, ScriptObject.AsString(arguments[0]), false);
                },
            "string name");

            mispEngine.AddFunction("print", "Print something.",
                (context, arguments) =>
                {
                    foreach (var item in arguments[0] as ScriptList)
                        System.Console.Write(MISP.Console.PrettyPrint2(item, 0));
                    return null;
                }, "?+item");

            mispEngine.AddFunction("emitf", "Emit a function",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    var obj = arguments[0] as ScriptObject;
                    stream.Write("Name: ");
                    stream.Write(obj.gsp("@name") + "\nHelp: " + obj.gsp("@help") + "\nArguments: ");
                    foreach (var arg in obj["@arguments"] as List<ArgumentInfo>)
                    {
                        stream.Write(arg.type.Typename + " ");
                        if (arg.optional) stream.Write("?");
                        if (arg.repeat) stream.Write("+");
                        stream.Write(arg.name + ", ");
                    }
                    stream.Write("\nBody: ");
                    if (obj["@function-body"] is ScriptObject)
                        Engine.SerializeCode(stream, obj["@function-body"] as ScriptObject);
                    else
                        stream.Write("System");
                    stream.Write("\n");
                    return stream.ToString();
                }, "function func");

            mispEngine.AddFunction("emitfl", "Emit a list of functions",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    foreach (var item in mispEngine.functions)
                        stream.Write(item.Value.gsp("@name") + new String(' ', 16 - item.Value.gsp("@name").Length) 
                            + ": " + item.Value.gsp("@help") + "\n");
                    return stream.ToString();
                });
        }

        private void SetupEnvironmentFunctions()
        {
            mispEngine.AddFunction("save-environment", "", (context, arguments) =>
            {
                var file = new System.IO.StreamWriter(arguments[0].ToString());
                mispEngine.SerializeEnvironment(file, mispObject);
                file.Close();
                return true;
            }, "string file");

            mispEngine.AddFunction("load-environment", "", (context, arguments) =>
            {
                var file = System.IO.File.ReadAllText(arguments[0].ToString());
                var newConsole = new MISP.Console((s) => { System.Console.Write(s); }, false);
                newConsole.mispContext.ResetTimer();
                newConsole.mispContext.evaluationState = EvaluationState.Normal;
                var result = newConsole.mispEngine.EvaluateString(newConsole.mispContext, file, arguments[0].ToString());
                newConsole.mispObject = result as ScriptObject;
                if (newConsole.mispContext.evaluationState == EvaluationState.Normal)
                {
                    mispContext = newConsole.mispContext;
                    mispEngine = newConsole.mispEngine;
                    mispObject = newConsole.mispObject;
                    SetupEnvironmentFunctions();
                    Write("Loaded.\n");
                    return true;
                }
                else
                {
                    Write("Error:\n");
                    Write(MISP.Console.PrettyPrint2(newConsole.mispContext.errorObject, 0));
                    return false;
                }
            }, "string file");
        }

        public void Execute(String str)
        {
            try
            {
                mispContext.ResetTimer();
                mispContext.evaluationState = EvaluationState.Normal;
                var result = mispEngine.EvaluateString(mispContext, str, "");
                if (mispContext.evaluationState == EvaluationState.Normal)
                {
                    Write(MISP.Console.PrettyPrint2(result, 0) + "\n");
                }
                else
                {
                    Write("Error:\n");
                    Write(MISP.Console.PrettyPrint2(mispContext.errorObject, 0));
                }
            }
            catch (Exception e)
            {
                Write(e.Message + "\n");
            }
        }

    }
}
