using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NativeGenerator
{
    /*  
        IDAScriptWriterBase class (C) 2016 Mark Ludwig
    */
    public abstract class IDAScriptWriterBase
    {
        private StringBuilder m_stringBuilder;

        private int m_lineNumber = 0;
        private int m_lastIndent = 0;

        protected abstract string FileExtension { get; }

        protected virtual char IndentChar
        {
            get { return '\t'; }
        }

        protected StringBuilder Writer
        {
            get
            {
                if (m_stringBuilder == null)
                    m_stringBuilder = new StringBuilder();

                return m_stringBuilder;
            }
        }

        protected int IndentLevel { get; set; }

        public void Indent()
        {
            m_lastIndent = IndentLevel++;
        }

        public void Dedent()
        {
            if (IndentLevel > 0)
                m_lastIndent = IndentLevel--;
        }

        public void Undent()
        {
            IndentLevel = 0;
        }

        public int LineNumber
        {
            get { return m_lineNumber; }
        }

        protected void IndentBlock()
        {
            if ((IndentLevel != m_lastIndent) && (IndentLevel != 0))
            {
                for (int i = 0; i < IndentLevel; i++)
                    Writer.Append(IndentChar);
            }

            m_lastIndent = IndentLevel;
        }

        public void Write(string text)
        {
            foreach (var c in text)
            {
                if (c == '\n')
                    m_lineNumber++;
            }

            IndentBlock();
            Writer.Append(text);
        }

        public void Write(string format, params object[] args)
        {
            Write(String.Format(format, args));
        }

        public void WriteLine()
        {
            IndentBlock();
            Writer.AppendLine();

            m_lineNumber++;
            m_lastIndent = 0;
        }

        public void WriteLine(string text)
        {
            Write(text);
            WriteLine();
        }

        public void WriteLine(string format, params object[] args)
        {
            WriteLine(String.Format(format, args));
        }

        public abstract void OpenMainBlock();
        public abstract void CloseMainBlock();

        public abstract void WritePreamble(string header = "");

        public abstract void WriteComment(string comment);
        
        public abstract void WriteMethodCall(string methodName);
        public abstract void WriteMethodCall(string methodName, params object[] arguments);

        public virtual void SaveFile(string directory, string name)
        {
            File.WriteAllText(Path.Combine(directory, String.Format("{0}.{1}", name, FileExtension)), Writer.ToString());
        }

        public override string ToString()
        {
            return Writer.ToString();
        }
    }
}
