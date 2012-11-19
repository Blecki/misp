using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private class ObjectRecord
        {
            internal ScriptObject obj;
            internal int referenceCount;
        }

        private static bool AddRef(ScriptObject obj, List<ObjectRecord> list)
        {
            var spot = list.FirstOrDefault((o) => { return Object.ReferenceEquals(o.obj, obj); });
            if (spot != null) { spot.referenceCount++; return false; }
            else
            {
                list.Add(new ObjectRecord { obj = obj, referenceCount = 1 });
                return true;
            }
        }

        private static void EnumerateObject(
            ScriptObject obj,
            List<ScriptObject> globalFunctions,
            List<ObjectRecord> objects,
            List<ObjectRecord> lambdas)
        {
            if (obj == null) return;

            if (Function.IsFunction(obj))
            {
                //Lambda function?
                if (globalFunctions.Contains(obj)) return;
                if (AddRef(obj, lambdas))
                    EnumerateObject(obj["declaration-scope"] as ScriptObject, globalFunctions, objects, lambdas);
            }
            else
            {
                foreach (var prop in obj.ListProperties())
                {
                    var value = obj.GetLocalProperty(prop as String);
                    if (value is ScriptObject)
                    {
                        if (AddRef(value as ScriptObject, objects))
                            EnumerateObject(value as ScriptObject, globalFunctions, objects, lambdas);
                    } 
                    else if (value is ScriptList)
                    {
                        foreach (var item in value as ScriptList)
                        {
                            if (item is ScriptObject)
                                if (AddRef(item as ScriptObject, objects))
                                    EnumerateObject(item as ScriptObject, globalFunctions, objects, lambdas);
                        }
                    }

                    
                }
            }
        }

        private static int IndexIn(ScriptObject obj, List<ObjectRecord> list)
        {
            for (int i = 0; i < list.Count; ++i)
                if (list[i].obj == obj) return i;
            return -1;
        }

        private static void EmitObjectProperty(
            System.IO.TextWriter to,
            Object value,
            List<ScriptObject> globalFunctions,
            List<ObjectRecord> objects,
            List<ObjectRecord> lambdas)
        {

            if (value == null) to.Write("null");
            else if (value is ScriptObject)
            {
                if (globalFunctions.Contains(value)) to.Write((value as ScriptObject).gsp("name"));
                else
                {
                    var index = IndexIn(value as ScriptObject, objects);
                    if (index != -1) to.Write("(index objects " + index + ")");
                    else
                    {
                        index = IndexIn(value as ScriptObject, lambdas);
                        if (index != -1) to.Write("(index lambdas " + index + ")");
                        else
                            EmitObject(to, value as ScriptObject, globalFunctions, objects, lambdas);
                    }
                }
            }
            else if (value is String)
                to.Write("\"" + value as String + "\"");
            else if (value is ScriptList)
            {
                to.Write("^(");
                foreach (var item in value as ScriptList)
                {
                    EmitObjectProperty(to, item, globalFunctions, objects, lambdas);
                    to.Write(" ");
                }
                to.Write(")");
            }
            else
                to.Write(value.ToString());
        }

        private static void EmitObjectProperties(
            System.IO.TextWriter to,
            ScriptObject obj,
            List<ScriptObject> globalFunctions,
            List<ObjectRecord> objects,
            List<ObjectRecord> lambdas)
        {
            foreach (var propertyName in obj.ListProperties())
            {
                to.Write("^(\"" + propertyName as String + "\" ");
                var value = obj.GetLocalProperty(propertyName as String);
                EmitObjectProperty(to, value, globalFunctions, objects, lambdas);
                to.Write(")\n");
            }
        }

        private static void EmitObject(
            System.IO.TextWriter to,
            ScriptObject obj,
            List<ScriptObject> globalFunctions,
            List<ObjectRecord> objects,
            List<ObjectRecord> lambdas)
        {
            to.WriteLine("(record ");
            EmitObjectProperties(to, obj, globalFunctions, objects, lambdas);
            to.Write(")\n");
        }

        private static void EmitObjectRoot(
            System.IO.TextWriter to,
            ScriptObject obj,
            List<ScriptObject> globalFunctions,
            List<ObjectRecord> objects,
            List<ObjectRecord> lambdas)
        {
            var index = IndexIn(obj, objects);
            to.WriteLine("(multi-set (index objects " + index + ") ^(");
            EmitObjectProperties(to, obj, globalFunctions, objects, lambdas);
            to.Write("))\n");
        }

        public void EmitFunction(ScriptObject func, string type, System.IO.TextWriter to)
        {
            to.Write("(" + type + " \"" + func.gsp("name") + "\" (");
            var arguments = func["@arguments"] as List<ArgumentInfo>;
            foreach (var arg in arguments)
            {
                to.Write("\"" + arg.type.Typename + " ");
                if (arg.optional) to.Write("?");
                if (arg.repeat) to.Write("+");
                to.Write(arg.name + "\" ");
            }
            to.Write(") ");
            Engine.SerializeCode(to, func["@function-body"] as ScriptObject);
            to.Write(" " + func.gsp("help") + ")\n");
        }

        public void SerializeEnvironment(System.IO.TextWriter to, ScriptObject @this)
        {
            var globalFunctions = new List<ScriptObject>();
            var objects = new List<ObjectRecord>();
            var lambdas = new List<ObjectRecord>();

            //Build list of global non-system functions
            foreach (var func in functions)
            {
                if (Function.IsSystemFunction(func.Value)) continue;
                globalFunctions.Add(func.Value);
            }

            //Filter objects into objects and lambda list
            foreach (var func in globalFunctions)
                EnumerateObject(func["declaration-scope"] as ScriptObject, globalFunctions, objects, lambdas);
            EnumerateObject(@this, globalFunctions, objects, lambdas);

            //Filter out objects with just a single reference
            objects = new List<ObjectRecord>(objects.Where((o) => { return o.referenceCount > 1; }));
            AddRef(@this, objects);

            to.WriteLine("(lastarg");

            //Emit global functions
            foreach (var func in globalFunctions)
                EmitFunction(func, "defun", to);

            //Create and emit lambda functions.
            to.WriteLine("(let ^(\n   ^(\"lambdas\" ^(");
            foreach (var func in lambdas)
            {
                to.Write("      ");
                EmitFunction(func.obj, "lambda", to);
            }
            to.WriteLine(")\n   )\n   ^(\"objects\" (array " + objects.Count + " (record)))\n)\n(lastarg\n");

            //Set function declaration scopes.
            foreach (var func in globalFunctions)
            {
                to.WriteLine("(set " + func.gsp("name") + " \"declaration-scope\" ");
                EmitObjectProperty(to, func["declaration-scope"], globalFunctions, objects, lambdas);
                to.WriteLine(")\n");
            }

            foreach (var func in lambdas)
            {
                to.WriteLine("(set " + func.obj.gsp("name") + " \"declaration-scope\" ");
                EmitObjectProperty(to, func.obj["declaration-scope"], globalFunctions, objects, lambdas);
                to.WriteLine(")\n");
            }
            //Emit remaining objects
            foreach (var obj in objects)
                EmitObjectRoot(to, obj.obj, globalFunctions, objects, lambdas);

            //Emit footer
            var thisIndex = IndexIn(@this, objects);
            to.Write("(index objects " + thisIndex + "))))");
        }

    }
}
