using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class ValueType
    {
        public short Address { get; init; }
        public short Size { get; init; }

        public short[] Array => Enumerable.Range(Address, Size).Cast<short>().ToArray();
    }
}
