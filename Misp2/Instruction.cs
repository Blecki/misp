using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public struct Instruction
    {
        public InstructionSet Opcode;
        public Operand FirstOperand;
        public Operand SecondOperand;
        public Operand ThirdOperand;

        public static Instruction? TryParse(String parseFrom)
        {
            try
            {
                return Parse(parseFrom);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Instruction Parse(String parseFrom)
        {
            var r = new Instruction();
            var parts = parseFrom.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            r.Opcode = (Enum.Parse(typeof(InstructionSet), parts[0]) as InstructionSet?).Value;
            r.FirstOperand = r.SecondOperand = r.ThirdOperand = Operand.NONE;
            if (parts.Length >= 2) r.FirstOperand = (Enum.Parse(typeof(Operand), parts[1]) as Operand?).Value;
            if (parts.Length >= 3) r.SecondOperand = (Enum.Parse(typeof(Operand), parts[2]) as Operand?).Value;
            if (parts.Length >= 4) r.ThirdOperand = (Enum.Parse(typeof(Operand), parts[3]) as Operand?).Value;
            return r;
        }

        public override string ToString()
        {
            return Opcode.ToString() + " " + FirstOperand + " " + SecondOperand + " " + ThirdOperand;
        }
    }
}
