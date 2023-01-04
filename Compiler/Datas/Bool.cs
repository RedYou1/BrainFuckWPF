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
    public class Bool : ValueType
    {
        public const short BytesSize = 1;
        public Bool(short address, short size = BytesSize) : base(address, BytesSize) { }

        public static Bool Constructor(short address) => new Bool(address);

        public static void Init(Data self, Compiler comp, string[] args, bool needReset)
            => BaseInit<Bool>(GetValuesByte, self, comp, args, needReset);

        public static void Add(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Bool>(GetValuesByte, self, comp, args, needReset);

        public static void Sub(Data self, Compiler comp, string[] args, bool needReset)
            => BaseSub<Bool>(GetValuesByte, self, comp, args, needReset);

        public static byte[] GetValuesByte(string value)
            => GetValue(value) ? new byte[] { 1 } : new byte[] { 0 };

        public static bool GetValue(string value)
        {
            if (bool.TryParse(value, out bool result))
            {
                return result;
            }
            throw new ArgumentException();
        }
    }
}
