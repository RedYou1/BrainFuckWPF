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
    public class Bool : ValueType
    {
        public Bool(short address, short size) : base(address, Types[nameof(Bool)].size)
        {
        }

        public static Bool Constructor(short address) => new Bool(address, Types[nameof(Bool)].size);

        public override void Add(Compiler comp, CodeWriter codeWriter, string stringValue, bool needReset)
        {
            if (comp.Memory.ContainName(stringValue))
            {
                Data from = comp.Memory[stringValue];
                if (from.Size != Types[nameof(Bool)].size)
                    throw new Exception("Bool add not same size");

                comp.CopyData(codeWriter, from, this, false, needReset);
            }
            else
            {
                bool value = GetValue(stringValue);
                if (value)
                {
                    comp.Move(codeWriter, Address);
                    codeWriter.Write("+", $"adding {value}");
                }
            }
        }

        public override void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            bool value = GetValue(stringValue);
            if (value)
            {
                comp.Move(codeWriter, Address);
                codeWriter.Write("-", $"substracting {value}");
            }
        }

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
