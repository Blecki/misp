using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private Random random = new Random();

        private void SetupMathFunctions()
        {
            AddFunction("+", "Add values", (context, arguments) =>
                {
                    var values = AutoBind.ListArgument(arguments[0]);
                    if (values.Count == 0) return null;
                    if (values.Count == 1) return values[0];
                    var result = (dynamic)values[0];
                    for (int i = 1; i < values.Count; ++i)
                        result += (dynamic)values[i];
                    return result;
                },
                Arguments.Repeat("value"));

            AddFunction("-", "Subtract values", (context, arguments) =>
            {
                if (arguments[0] == null || arguments[1] == null) return null;
                return (dynamic)arguments[0] - (dynamic)arguments[1];
            },
                Arguments.Arg("A"),
                Arguments.Arg("B"));

            AddFunction("*", "Add values", (context, arguments) =>
            {
                var values = AutoBind.ListArgument(arguments[0]);
                if (values.Count == 0) return null;
                if (values.Count == 1) return values[0];
                var result = (dynamic)values[0];
                for (int i = 1; i < values.Count; ++i)
                    result *= (dynamic)values[i];
                return result;
            },
                Arguments.Repeat("value")); 
            
            AddFunction("/", "Divide values", (context, arguments) =>
            {
                if (arguments[0] == null || arguments[1] == null) return null;
                return (dynamic)arguments[0] / (dynamic)arguments[1];
            },
                Arguments.Arg("A"),
                Arguments.Arg("B"));

            AddFunction("%", "Modulus values", (context, arguments) =>
            {
                if (arguments[0] == null || arguments[1] == null) return null;
                return (dynamic)arguments[0] % (dynamic)arguments[1];
            },
                Arguments.Arg("A"),
                Arguments.Arg("B"));


            //functions.Add("random", Function.MakeSystemFunction("random",
            //    Arguments.ParseArguments(this, "integer A", "integer B"),
            //    "A B : return a random value in range (A,B).",
            //    (context, arguments) =>
            //    {
            //        var first = arguments[0] as int?;
            //        var second = arguments[1] as int?;
            //        if (first == null || second == null || !first.HasValue || !second.HasValue) return null;
            //        return random.Next(first.Value, second.Value);
            //    }));

        }
    }
}
