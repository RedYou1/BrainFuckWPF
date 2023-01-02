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
    public class Char : ValueType
    {
        public const short BytesSize = 1;
        public Char(short address, short size) : base(address, BytesSize)
        {
            BuildInFunction.Add(BuildInFunctions.Init, Init);
            BuildInFunction.Add(BuildInFunctions.Add, Add);
            BuildInFunction.Add(BuildInFunctions.Sub, Sub);
        }

        public static Char Constructor(short address) => new Char(address, BytesSize);

        public static void Init(Data self, Compiler comp, string[] args, bool needReset)
            => BaseInit<Char>((s) => new byte[] { Byte.GetValue(s) }, self, comp, args, needReset);

        public static void Add(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Char>((s) => new byte[] { Byte.GetValue(s) }, self, comp, args, needReset);

        public static void Sub(Data self, Compiler comp, string[] args, bool needReset)
            => BaseSub<Char>((s) => new byte[] { Byte.GetValue(s) }, self, comp, args, needReset);

        public static char GetValue(string value)
        {
            if (Regex.Match(value, @"^'\\{0,1}.'$").Success || Regex.Match(value, "^\"\\\\{0,1}.\"$").Success)
            {
                value = value.Substring(1, value.Length - 2);
                if (value.Length == 1)
                {
                    return value[0];
                }
                else
                {
                    switch (value[1])
                    {
                        case 'a':
                            return '\a';
                        case 'b':
                            return '\b';
                        case 'f':
                            return '\f';
                        case 'n':
                            return '\n';
                        case 'r':
                            return '\r';
                        case 't':
                            return '\t';
                        case 'v':
                            return '\v';
                        default:
                            return value[1];
                    }
                }
            }
            throw new ArgumentException();
        }
    }
}
