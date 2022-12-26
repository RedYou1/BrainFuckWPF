using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Compiler.Compiler;

namespace Compiler
{
    public class Byte : ValueType
    {
        public const short BytesSize = 1;
        public Byte(short address, short size) : base(address, BytesSize)
        {
            BuildInFunction.Add(BuildInFunctions.Add, Add);
            BuildInFunction.Add(BuildInFunctions.Sub, Sub);
        }

        public static Byte Constructor(short address) => new Byte(address, BytesSize);

        public static void Add(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Byte>('+', (s) => GetValue(s), self, comp, args, needReset);

        public static void Sub(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Byte>('-', (s) => GetValue(s), self, comp, args, needReset);

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
