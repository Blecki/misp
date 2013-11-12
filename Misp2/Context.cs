using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Context
    {
        private List<Scope> scopeStack = new List<Scope>();
        private List<Object> Stack = new List<Object>();
        private List<Object> Code = new List<Object>();
        private UInt32 CodePointer = 0;

        public void Reset()
        {
            scopeStack.Clear();
            Stack.Clear();
            PushScope(new Scope());
            CodePointer = 0;
        }

        public Context(List<Object> Code)
        {
            this.Code = Code;
            Reset();
        }

        public Scope Scope { get { return scopeStack.Count > 0 ? scopeStack[scopeStack.Count - 1] : null; } }

        public void PushScope(Scope scope) { scope.parentScope = Scope; scopeStack.Add(scope); }
        public void PopScope() { Scope.parentScope = null; scopeStack.RemoveAt(scopeStack.Count - 1); }
    }
}
