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

        public Bool(short address, short size) : base(address, Types[nameof(Bool)].size)
        {
            BuildInFunction.Add(BuildInFunctions.Add, Add);
            BuildInFunction.Add(BuildInFunctions.Sub, Sub);
        }

        public static Bool Constructor(short address) => new Bool(address, Types[nameof(Bool)].size);


        public static ReturnCode Add(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Bool>('+', (s) => GetValue(s) ? 1 : 0, self, comp, args, needReset);

        public static ReturnCode Sub(Data self, Compiler comp, string[] args, bool needReset)
            => BaseAdd<Bool>('+', (s) => GetValue(s) ? 1 : 0, self, comp, args, needReset);

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
