using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class Short : ValueType
    {
        public Short(short address, short size) : base(address, Types[nameof(Short)].size)
        {
        }

        public static Short Constructor(short address) => new Short(address, Types[nameof(Short)].size);

        public override void Add(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            comp.Move(codeWriter, (short)(Address + 1));
            short value = GetValue(stringValue);
            codeWriter.Write(
                new StringBuilder("+[<+>]".Length * value).Insert(0, "+[<+>]", value).ToString()
                , $"adding {value}");
        }

        public override void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            comp.Move(codeWriter, (short)(Address + 1));
            short value = GetValue(stringValue);
            codeWriter.Write(
               new StringBuilder("-[<->]".Length * value).Insert(0, "-[<->]", value).ToString()
               , $"substracting {value}");
        }

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
