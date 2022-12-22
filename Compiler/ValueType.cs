using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Byte = Compiler.ValueTypes.Byte;

namespace Compiler
{
    public class ValueType
    {
        public short Address { get; }
        public short Size { get; }

        public ValueType(short address, short size)
        {
            Address = address;
            Size = size;
        }

        public virtual void Add(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            throw new Exception("Can't add a ValueType class");
        }

        public virtual void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            throw new Exception("Can't sub a ValueType class");
        }


        public static Dictionary<string, (short size, Func<short, ValueType> constructor)> Types =
        new(){
            { nameof(Byte),(1,Byte.Constructor) }
        };

        public short[] Array => Enumerable.Range(Address, Size).Cast<short>().ToArray();
    }
}
