﻿#define MultipleLine

namespace Compiler
{
    public class CodeWriter
    {
        private StreamWriter _writer;

        public CodeWriter(StreamWriter writer)
        {
            _writer = writer;
        }

        public void Write(string command, string description)
        {
#if MultipleLine
            _writer.WriteLine(command + " //" + description);
#else
            _writer.Write(command);
#endif
        }
    }
}
