﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MISP
{
    public class Debug
    {
        public static void DumpCompiledCode(Context context, System.IO.StreamWriter to)
        {
            DumpOpcode(context.CodeContext.Code, to, 0);
        }

        private static void DumpOpcode(List<Object> opcode, System.IO.StreamWriter to, int indent)
        {
            foreach (var item in opcode)
            {
                to.Write(new String(' ', indent * 4));
                if (item == null) to.Write("NULL\n");
                else if (item is List<String>)
                {
                    to.Write("[ ");
                    foreach (var entry in item as List<String>) to.Write(entry + " ");
                    to.Write("]\n");
                }
                else if (item is List<Object>)
                {
                    to.Write("--- Embedded instruction stream\n");
                    DumpOpcode(item as List<Object>, to, indent + 1);
                    to.Write(new String(' ', indent * 4));
                    to.Write("--- End embedded stream\n");
                }
                else if (item is String)
                    to.Write("\"" + item.ToString() + "\"\n");
                else to.Write(item.ToString() + "\n");
            }
        }
    }
}
