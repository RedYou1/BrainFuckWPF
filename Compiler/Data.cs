using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class Data
    {
        public short Address { get; }
        public short Size { get; }

        public Data(short address, short size)
        {
            Address = address;
            Size = size;
        }

        public short[] Array => Enumerable.Range(Address, Size).Cast<short>().ToArray();
    }
}
