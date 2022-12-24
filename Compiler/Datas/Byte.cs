using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compiler
{
    public class Byte : ValueType
    {
        public Byte(short address, short size) : base(address, Types[nameof(Byte)].size)
        {
        }

        public static Byte Constructor(short address) => new Byte(address, Types[nameof(Byte)].size);

        public override void Add(Compiler comp, CodeWriter codeWriter, string stringValue, bool needReset)
        {
            if (comp.Memory.ContainName(stringValue))
            {
                Data from = comp.Memory[stringValue];
                if (from.Size != Types[nameof(Byte)].size)
                    throw new Exception("Byte add not same size");

                comp.CopyData(codeWriter, from, this, false, needReset);
            }
            else
            {
                comp.Move(codeWriter, Address);
                byte value = GetValue(stringValue);
                codeWriter.Write(
                    new string('+',
                        value), $"adding {value}");
            }
        }

        public override void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            comp.Move(codeWriter, Address);
            byte value = GetValue(stringValue);
            codeWriter.Write(
                new string('-',
                    value), $"substracting {value}");
        }

        public static byte GetValue(string value)
        {
            if (byte.TryParse(value, out byte result))
            {
                return result;
            }
            return (byte)Char.GetValue(value);
        }
    }
}
