using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class CachedReflectionScriptObject : ScriptObject
    {
        private Dictionary<String, Object> thunkedFunctions = new Dictionary<string, Object>();

        override public object GetProperty(string name)
        {
            var field = this.GetType().GetField(name);
            if (field != null) return field.GetValue(this);
            if (thunkedFunctions.ContainsKey(name)) return thunkedFunctions[name];
            var func = this.GetType().GetMethod(name);

            var saveThis = this;

            if (func != null)
            {
                var impl = Function.MakeSystemFunction(name,
                    Arguments.Args(Arguments.Optional(Arguments.Repeat("arg"))),
                    "Auto-bound function",
                    (context, arguments) =>
                    {
                        return func.Invoke(saveThis, (arguments[0] as ScriptList).ToArray());
                    });
                thunkedFunctions.Upsert(name, impl);
                return impl;
            }
            return null;
        }

        public override object GetLocalProperty(string name)
        {
            return GetProperty(name);
        }

        override public void DeleteProperty(String name)
        {
            throw new ScriptError("Properties cannot be removed from objects of type " + this.GetType().Name + ".", null);
        }

        override public void SetProperty(string name, object value)
        {
            var field = this.GetType().GetField(name);
            if (field != null)
            {
                if (field.FieldType == typeof(bool))
                    field.SetValue(this, (value != null));
                else
                    field.SetValue(this, value);
            }
            else throw new ScriptError("Field does not exist on " + this.GetType().Name + ".", null);
        }

        override public ScriptList ListProperties()
        {
            return new ScriptList(this.GetType().GetFields().Select((info) => { return info.Name; }));
        }
    }
}
