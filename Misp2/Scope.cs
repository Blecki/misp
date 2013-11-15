using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Scope
    {
        internal Scope parentScope = null;
        internal Dictionary<String, ScriptList> variables = new Dictionary<String, ScriptList>();

        public bool HasVariable(String name)
        {
            return variables.ContainsKey(name);
        }

        public void PushVariable(String name, Object value)
        {
            if (!HasVariable(name)) variables.Add(name, new ScriptList());
            variables[name].Add(value);
        }

        public void PopVariable(String name)
        {
            if (!HasVariable(name)) return;
            var list = variables[name];
            list.RemoveAt(list.Count - 1);
            if (list.Count == 0)
                variables.Remove(name);
        }

        public Object GetVariable(String name)
        {
            if (name == "@parent") return parentScope;
            if (!HasVariable(name)) return null;
            var list = variables[name];
            return list[list.Count - 1];
        }

        public void ChangeVariable(String name, Object newValue)
        {
            if (!variables.ContainsKey(name)) 
                throw new ScriptError("Variable '" + name + "' does not exist.");
            var list = variables[name];
            list.RemoveAt(list.Count - 1);
            list.Add(newValue);
        }

        public Scope Capture()
        {
            var r = new Scope();
            foreach (var variable in variables)
                r.PushVariable(variable.Key, variable.Value.Last());
            return r;
        }
    }
}
