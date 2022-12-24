using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class Int : ValueType
    {
        public Int(short address, short size) : base(address, Types[nameof(Int)].size)
        {
        }

        public static Int Constructor(short address) => new Int(address, Types[nameof(Int)].size);

        public override void Add(Compiler comp, CodeWriter codeWriter, string stringValue, bool needReset)
        {
            if (comp.Memory.ContainName(stringValue))
            {
                Data from = comp.Memory[stringValue];
                if (from.Size != Types[nameof(Int)].size)
                    throw new Exception("Int add not same size");

                comp.CopyData(codeWriter, from, this, false, needReset);
            }
            else
            {
                comp.Move(codeWriter, (short)(Address + 3));
                int value = GetValue(stringValue);
                codeWriter.Write(
                    new StringBuilder("+[<+[<+[<+>]>]>]".Length * value).Insert(0, "+[<+[<+[<+>]>]>]", value).ToString()
                    , $"adding {value}");
            }
        }

        public override void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            comp.Move(codeWriter, (short)(Address + 3));
            int value = GetValue(stringValue);
            codeWriter.Write(
               new StringBuilder("-[<-[<-[<->]>]>]".Length * value).Insert(0, "-[<-[<-[<->]>]>]", value).ToString()
               , $"substracting {value}");
        }

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
