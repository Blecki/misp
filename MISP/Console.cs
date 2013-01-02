using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MISP;

namespace MISP
{
    public class Environment
    {
        public Engine engine;
        public Context context;
    }

    public class Console
    {
        public Action<String> Write = (s) => { };
        public Dictionary<String, Environment> environments = new Dictionary<string, Environment>();
        public Environment environment;

        public void AddEnvironment(String name, Engine engine, Context context)
        {
            environments.Upsert(name, new Environment { engine = engine, context = context });
            SetupStandardConsoleFunctions(engine);
        }

        public void RemoveEnvironment(String name)
        {
            environments.Remove(name);
        }

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
            if (AddEnvironmentFunctions) SetupEnvironmentFunctions(environment.engine);
        }

        public void Setup()
        {
            var mispEngine = new Engine();
            var mispContext = new Context();
            AddEnvironment("console", mispEngine, mispContext);
            this.environment = environments["console"];
            mispContext.limitExecutionTime = false;
 
            Write("MISP Console 1.0\n");

            mispEngine.AddFunction("run-file", "Load and run a file.",
                (context, arguments) =>
                {
                    var text = System.IO.File.ReadAllText(ScriptObject.AsString(arguments[0]));
                    return mispEngine.EvaluateString(context, text, ScriptObject.AsString(arguments[0]), false);
                },
                Arguments.Arg("name"));
        }

        public void SetupStandardConsoleFunctions(Engine mispEngine)
        {
            mispEngine.AddFunction("print", "Print something.",
                (context, arguments) =>
                {
                    foreach (var item in arguments[0] as ScriptList)
                        System.Console.Write(MISP.Console.PrettyPrint2(item, 0));
                    return null;
                }, Arguments.Optional(Arguments.Repeat("value")));

            mispEngine.AddFunction("emitf", "Emit a function",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    var obj = arguments[0] as ScriptObject;
                    stream.Write("Name: ");
                    stream.Write(obj.gsp("@name") + "\nHelp: " + obj.gsp("@help") + "\nArguments: \n");
                    foreach (var arg_ in obj["@arguments"] as ScriptList)
                    {
                        stream.Write("   ");
                        var arg = arg_ as ScriptObject;
                        //stream.Write((arg["@type"] as Type).Typename + " ");
                        if (arg["@optional"] != null) stream.Write("?");
                        if (arg["@repeat"] != null) stream.Write("+"); 
                        if (arg["@lazy"] != null) stream.Write("*");
                        stream.Write(arg["@name"] + "  ");
                        if (arg["@mutator"] != null) Engine.SerializeCode(stream, arg["@mutator"] as ScriptObject);
                        stream.Write("\n");
                    }
                    stream.Write("\nBody: ");
                    if (obj["@function-body"] is ScriptObject)
                        Engine.SerializeCode(stream, obj["@function-body"] as ScriptObject);
                    else
                        stream.Write("System");
                    stream.Write("\n");
                    return stream.ToString();
                },
                Arguments.Arg("func"));

            mispEngine.AddFunction("emitfl", "Emit a list of functions",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    foreach (var item in mispEngine.functions)
                        stream.Write(item.Value.gsp("@name") + new String(' ', 16 - item.Value.gsp("@name").Length) 
                            + ": " + item.Value.gsp("@help") + "\n");
                    return stream.ToString();
                });

            mispEngine.AddFunction("reflect", "Examine an object using .net reflection.",
                (context, arguments) =>
                {
                    var stream = new System.IO.StringWriter();
                    if (arguments[0] == null) stream.Write("null\n");
                    else
                    {
                        stream.Write(arguments[0].GetType().Name + "\n");
                        foreach (var field in arguments[0].GetType().GetFields())
                            stream.Write("field: " + field.Name + " " + field.FieldType.Name + "\n");
                        foreach (var method in arguments[0].GetType().GetMethods())
                            stream.Write("method: " + method.Name + " " + method.ReturnType.Name + "\n");
                    }
                    return stream.ToString();
                }, Arguments.Arg("object"));
        }

        private void SetupEnvironmentFunctions(Engine mispEngine)
        {
            mispEngine.AddFunction("save-environment", "", (context, arguments) =>
            {
                var file = new System.IO.StreamWriter(arguments[0].ToString());
                mispEngine.SerializeEnvironment(file, context.Scope);
                file.Close();
                return true;
            }, Arguments.Arg("file"));

            mispEngine.AddFunction("load-environment", "", (context, arguments) =>
            {
                var file = System.IO.File.ReadAllText(arguments[0].ToString());
                var newConsole = new MISP.Console((s) => { System.Console.Write(s); }, false);
                newConsole.environment.context.ResetTimer();
                newConsole.environment.context.evaluationState = EvaluationState.Normal;
                var result = newConsole.environment.engine.EvaluateString(newConsole.environment.context, file, arguments[0].ToString());
                var scope = new Scope();
                foreach (var memberName in (result as ScriptObject).ListProperties())
                {
                    var value = (result as ScriptObject).GetLocalProperty(memberName as String);
                    scope.PushVariable(memberName as String, value);
                }
                if (newConsole.environment.context.evaluationState == EvaluationState.Normal)
                {
                    environment.context = newConsole.environment.context;
                    environment.context.ReplaceScope(scope);
                    mispEngine = newConsole.environment.engine;
                    SetupEnvironmentFunctions(environment.engine);
                    Write("Loaded.\n");
                    return true;
                }
                else
                {
                    Write("Error:\n");
                    Write(MISP.Console.PrettyPrint2(newConsole.environment.context.errorObject, 0));
                    return false;
                }
            }, Arguments.Arg("file"));
        }

        public void Execute(String str)
        {
            try
            {
                environment.context.ResetTimer();
                environment.context.evaluationState = EvaluationState.Normal;
                var result = environment.engine.EvaluateString(environment.context, str, "");

                if (environment.context.evaluationState == EvaluationState.Normal)
                {
                    Write(MISP.Console.PrettyPrint2(result, 0) + "\n");
                }
                else
                {
                    Write("Error:\n");
                    Write(MISP.Console.PrettyPrint2(environment.context.errorObject, 0));
                }

                if (!environment.context.CheckScope())
                {
                    Write("Error: Scopes not properly cleaned.\n");
                }
            }
            catch (Exception e)
            {
                Write(e.Message + "\n");
            }
        }

    }
}
