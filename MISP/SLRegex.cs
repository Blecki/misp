using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public partial class Engine
    {
        private void SetupRegexFunctions()
        {
            AddFunction("rmatch", "True if regex matches parameter",
                (context, arguments) =>
                {
                    var regex = AutoBind.StringArgument(arguments[0]);
                    var value = AutoBind.StringArgument(arguments[1]);
                    var result = System.Text.RegularExpressions.Regex.Match(value, regex);
                    return result;
                }, Arguments.Arg("regex"), Arguments.Arg("value"));

        }
    }
}
