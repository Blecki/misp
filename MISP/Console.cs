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
        public String name;
    }

    public class Console
    {
        public Action<String> Write = (s) => { };
        public Dictionary<String, Environment> environments = new Dictionary<string, Environment>();
        public Environment environment;
        public Action<Engine> SetupHost;
        public bool noEcho = false;

        public void PrepareEnvironment(Engine engine)
        {
            SetupStandardConsoleFunctions(engine);
            if (SetupHost != null) SetupHost(engine);
            engine.AddFunction("run-file", "Load and run a file.",
                (_context, arguments) =>
                {
                    var text = System.IO.File.ReadAllText(ScriptObject.AsString(arguments[0]));
                    return engine.EvaluateString(_context, text, ScriptObject.AsString(arguments[0]), false);
                },
                Arguments.Arg("name"));
        }


        public Environment AddEnvironment(String name, Engine engine, Context context)
        {
            environments.Upsert(name, new Environment { engine = engine, context = context, name = name });
            return environments[name];
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
                        for (int i = 0; i < l.Count && i < 20; ++i)
                        {
                            Write(new String('.', depth * 3 + 1));
                            Write(PrettyPrint2(l[i], depth + 1));
                            Write("\n");
                        }
                        if (l.Count >= 20) Write("...and additional items.\n");
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


        public Console(Action<String> Write, Action<Engine> SetupHost)
        {
            this.Write = Write;
            this.SetupHost = SetupHost;
            SetupNewEnvironment("console");
            environment = environments["console"];
            SetupEnvironmentFunctions(environment.engine);
          
        }

        public void SetupNewEnvironment(String name)
        {
            var mispEngine = new Engine();
            var mispContext = new Context();
            PrepareEnvironment(mispEngine);
            var environment = AddEnvironment(name, mispEngine, mispContext);
            mispContext.limitExecutionTime = false;
 
            
        }

        public static String UnescapeString(String s)
        {
            var place = 0;
            var r = "";
            while (place < s.Length)
            {
                if (s[place] == '\\')
                {
                    if (place < s.Length - 1)
                    {
                        if (s[place + 1] == 'n')
                            r += '\n';
                        else if (s[place + 1] == 't')
                            r += '\t';
                        else if (s[place + 1] == 'r')
                            r += '\r';
                        else r += s[place + 1];
                    }                    
                    place += 2;
                }
                else
                {
                    r += s[place];
                    ++place;
                }
            }
            return r;
        }

        public void SetupStandardConsoleFunctions(Engine mispEngine)
        {
            mispEngine.AddFunction("print", "Print something.",
                (context, arguments) =>
                {
                    noEcho = true;
                    foreach (var item in arguments[0] as ScriptList)
                        Write(UnescapeString(MISP.Console.PrettyPrint2(item, 0)));
                    return null;
                }, Arguments.Optional(Arguments.Repeat("value")));

            mispEngine.AddFunction("printf", "Print a string with formatting.",
                (context, arguments) =>
                {
                    noEcho = true;
                    var s = String.Format(AutoBind.StringArgument(arguments[0]),
                        AutoBind.ListArgument(arguments[1]).ToArray());
                    Write(UnescapeString(s));
                    return null;
                },
                Arguments.Arg("format-string"),
                Arguments.Optional(Arguments.Repeat("value")));


            mispEngine.AddFunction("emitfl", "Emit a list of functions",
                (context, arguments) =>
                {
                    noEcho = true;
                    if (arguments[0] == null) foreach (var item in mispEngine.functions) EmitFunctionListItem(item.Value);
                    else if (arguments[0] is String)
                    {
                        var regex = new System.Text.RegularExpressions.Regex(arguments[0] as String);
                        foreach (var item in mispEngine.functions)
                            if (regex.IsMatch(item.Value.gsp("@name"))) EmitFunctionListItem(item.Value);
                    }
                    else
                    {
                        foreach (var item in AutoBind.ListArgument(arguments[0]))
                            if (item is ScriptObject) EmitFunctionListItem(item as ScriptObject);
                    }
                    return null;
                }, Arguments.Optional("list"));

            mispEngine.AddFunction("module", "Load a module from disc",
                (context, arguments) =>
                {
                    return NetModule.LoadModule(mispEngine, AutoBind.StringArgument(arguments[0]),
                        AutoBind.StringArgument(arguments[1]));
                }, Arguments.Arg("file"), Arguments.Arg("module"));

        }

        private void EmitFunctionListItem(ScriptObject item)
        {
            Write(item.gsp("@name"));
            if (item.gsp("@name").Length < 16)
                Write(new String(' ', 16 - item.gsp("@name").Length));
            Write(item.gsp("@help") + "\n");
        }

        private void SetupEnvironmentFunctions(Engine mispEngine)
        {
            mispEngine.AddFunction("save-environment", "", (context, arguments) =>
            {
                Engine.ReportSerializationError = (str) => Write(str + "\n");
                var file = new System.IO.StreamWriter(arguments[0].ToString());
                mispEngine.SerializeEnvironment(file, context.Scope);
                file.Close();
                return true;
            }, Arguments.Arg("file"));

            mispEngine.AddFunction("load-environment", "", (context, arguments) =>
            {
                var newContext = new Context();
                var newEngine = new Engine();

                PrepareEnvironment(newEngine);

                if (arguments[2] != null)
                {
                    newEngine.Evaluate(newContext, arguments[2], true, true);
                    if (newContext.evaluationState == EvaluationState.UnwindingError)
                    {
                        Write("Prime error:\n");
                        Write(MISP.Console.PrettyPrint2(newContext.errorObject, 0));
                        return false;
                    }
                }

                var result = newEngine.EvaluateString(newContext,
                    System.IO.File.ReadAllText(arguments[0].ToString()), arguments[0].ToString()) as ScriptObject;


                if (newContext.evaluationState != EvaluationState.UnwindingError)
                {
                    var environment = AddEnvironment(arguments[1].ToString(), newEngine, newContext);
                    if (environment.name == this.environment.name)
                        this.environment = environment;
                    SetupEnvironmentFunctions(newEngine);
                    var scope = new Scope();
                    foreach (var prop in result.ListProperties())
                        scope.PushVariable(prop as String, result.GetLocalProperty(prop as String));
                    newContext.ReplaceScope(scope);

                    Write("Loaded.\n");
                    return true;
                }
                else
                {
                    Write("Error:\n");
                    Write(MISP.Console.PrettyPrint2(newContext.errorObject, 0));
                    return false;
                }
            }, Arguments.Arg("file"), Arguments.Arg("name"), Arguments.Optional(Arguments.Lazy("prime")));
        }

        public void ExecuteCommand(String str)
        {
            try
            {
                if (str.StartsWith("\\"))
                {
                    if (str.StartsWith("\\limit"))
                    {
                            var time = Convert.ToSingle(str.Substring(7));
                            environment.context.allowedExecutionTime = TimeSpan.FromSeconds(time);
                            environment.context.limitExecutionTime = time > 0;

                            if (environment.context.limitExecutionTime)
                                Write("Execution time limit set to " + environment.context.allowedExecutionTime + "\n");
                            else
                                Write("Execution time limit disabled.\n");
                    }
                    else if (str.StartsWith("\\trace"))
                    {
                        if (environment.context.trace == null) environment.context.trace = Write;
                        else environment.context.trace = null;
                    }
                    else if (str.StartsWith("\\environments"))
                    {
                        Write("Environments:\n");
                        foreach (var environment in environments)
                            Write(".." + environment.Key + "\n");
                    }
                    else if (str.StartsWith("\\environment"))
                    {
                        var environmentName = str.Substring(13);
                        if (environments.ContainsKey(environmentName))
                        {
                            environment = environments[environmentName];
                            Write("Environment changed to " + environmentName + ".\n");
                        }
                        else
                            Write("No environment with name " + environmentName + ".\n");
                    }
                    else Write("I don't understand.\n");
                }
                else
                {
                    noEcho = false;
                    environment.context.ResetTimer();
                    environment.context.evaluationState = EvaluationState.Normal;
                    var result = environment.engine.EvaluateString(environment.context, "(" + str + ")", "");

                    if (environment.context.evaluationState != EvaluationState.UnwindingError)
                    {
                        if (!noEcho) Write(MISP.Console.PrettyPrint2(result, 0) + "\n");
                    }
                    else
                    {
                        Write("Error:\n");
                        Write(MISP.Console.PrettyPrint2(environment.context.errorObject, 0));
                    }

                    if (!environment.context.CheckScope())
                        Write("Error: Scopes not properly cleaned.\n");


                    var prompt = environment.context.Scope.GetVariable("@prompt");
                    if (prompt != null && Function.IsFunction(prompt as ScriptObject))
                        Function.Invoke(prompt as ScriptObject, environment.engine, environment.context, new ScriptList());
                    
                }
            }
            catch (Exception e)
            {
                Write(e.Message + "\n");
            }
        }

        public void ExecuteCode(ScriptObject obj)
        {
            try
            {
                environment.context.ResetTimer();
                environment.context.evaluationState = EvaluationState.Normal;
                var result = environment.engine.Evaluate(environment.context, obj, true, false);

                if (environment.context.evaluationState != EvaluationState.UnwindingError)
                {
                    if (!noEcho) Write(MISP.Console.PrettyPrint2(result, 0) + "\n");
                }
                else
                {
                    Write("Error:\n");
                    Write(MISP.Console.PrettyPrint2(environment.context.errorObject, 0));
                }

                if (!environment.context.CheckScope())
                    Write("Error: Scopes not properly cleaned.\n");

                noEcho = false;
            }
            catch (Exception e)
            {
                Write(e.Message + "\n");
            }
        }

    }
}
