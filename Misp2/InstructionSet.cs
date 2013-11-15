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

        MOVE,           // SOURCE       DESTINATION     UNUSED
        LOOKUP,         // NAME         DESTINATION     UNUSED
        MEMBER_LOOKUP,  // NAME         OBJECT          DESTINATION

        BEGIN_LOOP,     // DESTINATION  UNUSED          UNUSED      --Places the current execution point in DESTINATION.
        BREAK,          // SOURCE       UNUSED          UNUSED      --Moves execution to the point in SOURCE, skipping 1 instruction.
        BRANCH,         // CODE         DESTINATION     UNUSED      --BEGIN_LOOP; then move execution into embedded code CODE.
        CONTINUE,       // SOURCE       UNUSED          UNUSED      --Moves execution to the point in SOURCE, without advancement.
        CLEANUP,        // SOURCE       UNUSED          UNUSED      --Remove SOURCE items from top of stack.
        SWAP_TOP,       // UNUSED       UNUSED          UNUSED      --Swap the two top object on stack.
        
        EMPTY_LIST,     // DESTINATION  UNUSED          UNUSED      --Create an empty list and store in DESTINATION.
        APPEND_RANGE,   // LIST-A       LIST-B          DESTINATION --Append A to B, store in DESTINATION.
        APPEND,         // VALUE        LIST            DESTINATION --Append VALUE to LIST, store in DESTINATION.
        LENGTH,         // LIST         DESTINATION     UNUSED      --Place the length of LIST in DESTINATION.
        INDEX,          // INDEX        LIST            DESTINATION --Place LIST[INDEX] in DESTINATION.
        PREPEND,
        PREPEND_RANGE,

        INVOKE,
        LAMBDA,
        PUSH_SCOPE,
        POP_SCOPE,
        PEEK_SCOPE,

        PUSH_VARIABLE,
        SET_VARIABLE,
        POP_VARIABLE,

        DECREMENT,
        LESS,
        IF_TRUE,


        THROW,
        CATCH,
    }
}
