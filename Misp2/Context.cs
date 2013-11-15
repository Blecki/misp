﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public struct CodeContext
    {
        public InstructionList Code;
        public int InstructionPointer;

        public CodeContext(InstructionList Code, int InstructionPointer)
        {
            this.Code = Code;
            this.InstructionPointer = InstructionPointer;
        }
    }

    public class Context
    {
        private List<Scope> scopeStack = new List<Scope>();
        internal Stack<Object> Stack = new Stack<Object>();
        internal CodeContext CodeContext;
        internal CodeContext OriginalCodeContext;

        public void Reset()
        {
            scopeStack.Clear();
            Stack.Clear();
            PushScope(new Scope());
            CodeContext = OriginalCodeContext;
        }

        public Context(CodeContext start)
        {
            this.OriginalCodeContext = start;
            Reset();
        }

        public int ScopeStackDepth { get { return scopeStack.Count; } }
        public void TrimScopes(int Depth) { scopeStack = new List<Scope>(scopeStack.GetRange(0, Depth)); }
        public Scope Scope { get { return scopeStack.Count > 0 ? scopeStack[scopeStack.Count - 1] : null; } }
        public void PushScope(Scope scope) { scope.parentScope = Scope; scopeStack.Add(scope); }
        public void PopScope() { Scope.parentScope = null; scopeStack.RemoveAt(scopeStack.Count - 1); }
    }
}
