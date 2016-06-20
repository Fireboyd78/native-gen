using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NativeGenerator
{
    public class IDAPythonWriter : IDAScriptWriterBase
    {
        protected override string FileExtension
        {
            get { return "py"; }
        }

        public override void CloseMainBlock()
        {
            WriteLine("#---------------------------------------------------------------------");
        }

        public override void OpenMainBlock()
        {
            WriteLine("#---------------------------------------------------------------------");
        }

        public override void WritePreamble(string header = "")
        {
            WriteLine("#!/usr/bin/env python");

            if (!String.IsNullOrEmpty(header))
            {
                WriteLine("#---------------------------------------------------------------------");
                WriteLine("# {0}", header);
                WriteLine("#---------------------------------------------------------------------");
            }

            WriteLine();

            WriteLine("import idaapi");
            WriteLine("from idc import *");
            WriteLine();
        }

        public override void WriteComment(string comment)
        {
            Write("# {0} ", comment);
        }

        public override void WriteMethodCall(string methodName)
        {
            Write("{0}() ", methodName);
        }

        public override void WriteMethodCall(string methodName, params object[] arguments)
        {
            Write("{0}({1}) ", methodName, String.Join(", ", arguments));
        }
    }
}
