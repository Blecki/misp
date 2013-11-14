using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    //Lower three bits of an instruction determine operand behavior.
    public enum Operand
    {
        NEXT = 0,
        POP = 1,
        PEEK = 2,
        PUSH = 3,
        NONE = 4
    }

    public enum InstructionSet
    {
        YIELD = 0,      //Yield execution back to the system

        MOVE,
        LOOKUP,
        MEMBER_LOOKUP,

        LAMBDA_PREAMBLE,
        BEGIN_LOOP,
        BREAK,
        BRANCH,
        CONTINUE,
        CLEANUP,
        
        EMPTY_LIST,
        APPEND_RANGE,
        APPEND,
        LENGTH,
        INDEX,
        PREPEND,
        PREPEND_RANGE,

        INVOKE,

        PUSH_VARIABLE,
        SET_VARIABLE,
        POP_VARIABLE,

        DECREMENT,
        LESS,
        IF_TRUE,

    }

    public class InstructionHelper
    {
        public static UInt16 MakeInstruction(InstructionSet Instruction, Operand Operand)
        {
            return (UInt16)((((int)Instruction) << 8) + ((int)Operand));
        }
    }
    
}
