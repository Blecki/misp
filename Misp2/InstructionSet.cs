using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public enum InstructionSet
    {
        YIELD = 0,          //Yield execution back to the system
        MEMBER_LOOKUP = 1,  //Pop the name of a member off the stack; Pop an object off the stack; lookup the member and push to th stack

    }
}
