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
    public class Char : ValueType
    {
        public Char(short address, short size) : base(address, Types[nameof(Char)].size)
        {
        }

        public static Char Constructor(short address) => new Char(address, Types[nameof(Char)].size);

        public override void Add(Compiler comp, CodeWriter codeWriter, string stringValue, bool needReset)
        {
            if (comp.Memory.ContainName(stringValue))
            {
                Data from = comp.Memory[stringValue];
                if (from.Size != Types[nameof(Char)].size)
                    throw new Exception("Char add not same size");

                comp.CopyData(codeWriter, from, this, false, needReset);
            }
            else
            {
                comp.Move(codeWriter, Address);
                char value = GetValue(stringValue);
                codeWriter.Write(
                    new string('+',
                        value), $"adding {value}");
            }
        }

        public override void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            comp.Move(codeWriter, Address);
            char value = GetValue(stringValue);
            codeWriter.Write(
                new string('-',
                    value), $"substracting {value}");
        }

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
