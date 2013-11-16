using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class ScriptObject
    {
        public Dictionary<String, Object> properties = new Dictionary<string, object>();

        public ScriptObject() { }

        public ScriptObject(ScriptObject cloneFrom)
        {
            foreach (var str in cloneFrom.ListProperties())
                SetProperty(str as String, cloneFrom.GetProperty(str as String));
        }

        public ScriptObject(params Object[] args)
        {
            if (args.Length % 2 != 0) throw new InvalidProgramException("Generic Script Object must be initialized with pairs");
            for (int i = 0; i < args.Length; i += 2)
                SetProperty(args[i].ToString(), args[i + 1]);
        }

        public object GetProperty(string name)
        {
            if (properties.ContainsKey(name)) return properties[name];
            else return null;
        }

        public void SetProperty(string Name, Object Value)
        {
            properties.Upsert(Name, Value);
        }

        public void DeleteProperty(String Name)
        {
            if (properties.ContainsKey(Name)) properties.Remove(Name);
        }

        public IEnumerable<String> ListProperties()
        {
            return properties.Select((p) => { return p.Key; });
        }

        public void ClearProperties()
        {
            properties.Clear();
        }

        public bool HasProperty(String name)
        {
            return properties.ContainsKey(name);
        }
    }
}
