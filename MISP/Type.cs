using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Type
    {
        public virtual Object ProcessArgument(Context context, Object obj) { return obj; }
        public virtual Object CreateDefault() { return null; }
        public static Type Anything = new Type();
        public String Typename;

        public static void CheckArgumentType(Context context, Object obj, System.Type type)
        {
            if (obj == null)
            {
                context.RaiseNewError("Expecting argument of type " + type + ", got null. ", null);
                return;
            }
            if (obj.GetType() == type || obj.GetType().IsSubclassOf(type)) return;
            context.RaiseNewError("Function argument is the wrong type. Expected type "
                    + type + ", got " + obj.GetType() + ". ", null);
        }
    }

    public class TypePrimitive : Type
    {
        private System.Type type;
        private bool allowNull;
        public TypePrimitive(System.Type type, bool allowNull) { this.type = type; this.allowNull = allowNull; }

        public override object ProcessArgument(Context context, object obj)
        {
            if (allowNull && obj == null) return obj;
                Type.CheckArgumentType(context, obj, type);
            if (context.evaluationState == EvaluationState.UnwindingError)
            {
                context.evaluationState = EvaluationState.Normal;
                try
                {
                    return Convert.ChangeType(obj, type);
                }
                catch (Exception exp)
                {
                    context.evaluationState = EvaluationState.UnwindingError;
                }
            }
            return obj;
        }

        public override object CreateDefault()
        {
            return null;
        }

    }

    public class TypeString : Type
    {
        public override object ProcessArgument(Context context, object obj)
        {
            if (obj == null) return "";
            else return ScriptObject.AsString(obj);
        }

        public override object CreateDefault()
        {
            return "";
        }

    }

    public class TypeList : Type
    {
        public override object ProcessArgument(Context context, object obj)
        {
            if (obj == null) return new ScriptList();
            else if (!(obj is ScriptList)) return new ScriptList(obj);
            return obj;
        }

        public override object CreateDefault()
        {
            return new ScriptList();
        }

       
    }
}
