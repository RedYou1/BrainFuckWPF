using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static Compiler.Compiler;

namespace Compiler
{
    public class Int : ValueType
    {

        public Int(short address, short size) : base(address, Types[nameof(Int)].size)
        {
            BuildInFunction.Add(BuildInFunctions.Add, Add);
            BuildInFunction.Add(BuildInFunctions.Sub, Sub);
        }

        public static Int Constructor(short address) => new Int(address, Types[nameof(Int)].size);

        public static ReturnCode Add(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Int>('+', (s) => GetValue(s), self, comp, args, needReset);

        public static ReturnCode Sub(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Int>('-', (s) => GetValue(s), self, comp, args, needReset);

        public static int GetValue(string value)
        {
            if (int.TryParse(value, out int result))
            {
                return result;
            }
            string s = String.GetValue(value);
            if (s.Length == 4)
                return (s[3] << 0x1000000) + (s[2] << 0x10000) + (s[1] << 0x100) + s[0];
            if (s.Length == 3)
                return (s[2] << 0x10000) + (s[1] << 0x100) + s[0];
            if (s.Length == 2)
                return (s[1] << 0x100) + s[0];
            if (s.Length == 1)
                return s[0];
            if (s.Length == 0)
                return 0;
            throw new ArgumentException();
        }
    }
}
