using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Compiler.ValueTypes
{
    public class Byte : ValueType
    {
        public Byte(short address, short size) : base(address, Types[nameof(Byte)].size)
        {
        }

        public static Byte Constructor(short address) => new Byte(address, Types[nameof(Byte)].size);

        public override void Add(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            comp.Move(codeWriter, Address);
            byte value = comp.GetValue(stringValue);
            codeWriter.Write(
                new string('+',
                    value), $"adding {value}");
        }

        public override void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            comp.Move(codeWriter, Address);
            byte value = comp.GetValue(stringValue);
            codeWriter.Write(
                new string('-',
                    value), $"substracting {value}");
        }
    }
}
