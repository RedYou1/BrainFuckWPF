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
            AddressArray = Enumerable.Range(Address, Size).Select(x => (short)x).ToArray();
        }

        public virtual void Set(Compiler comp, CodeWriter codeWriter, string stringValue, bool needReset)
        {
            throw new Exception("Can't set a Data class");
        }

        public readonly short[] AddressArray;

        public static List<string> Types =
        new(){
            nameof(Bool),
            nameof(Byte),
            nameof(Char),
            nameof(Short),
            nameof(Int),

            nameof(Array),
            nameof(String),
        };
    }
}
