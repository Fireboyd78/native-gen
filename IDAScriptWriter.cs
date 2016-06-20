using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NativeGenerator
{
    /*  
        IDAScriptWriter class (C) 2016 Mark Ludwig
    */
    public class IDAScriptWriter : IDAScriptWriterBase
    {
        protected override string FileExtension
        {
            get { return "idc"; }
        }

        public override void CloseMainBlock()
        {
            Dedent();
            WriteLine("}");
        }

        public override void OpenMainBlock()
        {
            WriteLine("static main() {");
            Indent();
        }

        public override void WritePreamble(string header = "")
        {
            if (!String.IsNullOrEmpty(header))
                WriteLine("/*\n\t{0}\n*/\n", header);

            WriteLine("#include <ida.idc>\n");
        }

        public override void WriteComment(string comment)
        {
            Write("// {0} ", comment);
        }
        
        public override void WriteMethodCall(string methodName)
        {
            Write("{0}(); ", methodName);
        }

        public override void WriteMethodCall(string methodName, params object[] arguments)
        {
            Write("{0}({1}); ", methodName, String.Join(", ", arguments));
        }
    }
}
