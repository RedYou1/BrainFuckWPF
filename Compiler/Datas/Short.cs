using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static Compiler.Compiler;

namespace Compiler
{
    public class Short : ValueType
    {
        public const short BytesSize = 2;
        public Short(short address, short size = BytesSize) : base(address, BytesSize) { }

        public static Short Constructor(short address) => new Short(address);

        public static void Init(Data self, Compiler comp, string[] args, bool needReset)
           => BaseInit<Short>((s) => BitConverter.GetBytes(GetValue(s)), self, comp, args, needReset);

        public static void Add(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Short>((s) => BitConverter.GetBytes(GetValue(s)), self, comp, args, needReset);

        public static void Sub(Data self, Compiler comp, string[] args, bool needReset)
            => BaseSub<Short>((s) => BitConverter.GetBytes(GetValue(s)), self, comp, args, needReset);

        public static short GetValue(string value)
        {
            if (short.TryParse(value, out short result))
            {
                return result;
            }
            string s = String.GetValue(value);
            if (s.Length == 2)
                return (short)((s[1] << 0x100) + s[0]);
            if (s.Length == 1)
                return (short)s[0];
            if (s.Length == 0)
                return 0;
            throw new ArgumentException();
        }
    }
}
