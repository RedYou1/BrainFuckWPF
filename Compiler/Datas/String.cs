using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Compiler.Compiler;

namespace Compiler
{
    public class String : Array
    {
        public String(short address, short elementSize, short amount)
            : base(address, elementSize, amount, Char.Constructor) { }

        public static Func<short, String> ConstructorOf(short amount)
            => (address) => new String(address, Char.BytesSize, amount);


        public static void PrintString(Data data, Compiler comp, string[] args, bool needReset)
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 2, "Need something to print");

            comp.Memory!.PushStack();

            Bool end = comp.Memory!.Add<Bool>(" endPrints ");
            Bool tempEnd = comp.Memory!.Add<Bool>(" tempEndPrints ");
            Byte temp = comp.Memory!.Add<Byte>(" tempPrints ");

            foreach (string arg in args.Skip(1))
            {
                Data v = comp.Memory![arg];
                comp.CodeWriter!.Set(end.Address, 1, "check toPrint");
                for (short i = v.Address; i < v.Address + v.Size; i++)
                {
                    comp.CodeWriter!.IfKeep(end.Address, () =>
                    {
                        comp.CodeWriter!.IfKeep(i,
                            () =>
                            {
                                comp.CodeWriter!.Move(i);
                                comp.CodeWriter!.Write(".", "print");
                                comp.CodeWriter!.Add(end.Address, 1, "end toPrint");
                            }, $"check {i}", $"end check {i}", temp.Address);
                        comp.CodeWriter!.Add(end.Address, -1, "end toPrint");
                    }, "check toPrint", "end check toPrint", tempEnd.Address);
                }
            }

            comp.Memory!.PopStack(needReset);
        }

        public static void Set(Data data, Compiler comp, string[] args, bool needReset)
        {
            comp.IsMainFile();

            for (short i = data.Address; i < data.Address + data.Size; i++)
            {
                comp.CodeWriter!.Move(i);
                comp.CodeWriter!.Write("[-]", "reset Set");
            }
            if (comp.Memory!.ContainName(args[2]))
            {
                Data from = comp.Memory![args[2]];
                comp.CodeWriter!.CopyData(from, data, true, needReset);
            }
            else
            {
                string value = GetValue(args[2]);
                if (value.Length > data.Size)
                    throw new Exception($"value {args[2]} dont fit in string of {data.Size}");
                for (short i = 0; i < value.Length; i++)
                {
                    comp.CodeWriter!.Move((short)(data.Address + i));
                    comp.CodeWriter!.Write(new string('+', (byte)value[i]), $"set string {i} to {value[i]}");
                }
            }
        }

        public static string GetValue(string value)
        {
            if (Regex.Match(value, @"^'\\{0,1}.'$").Success || Regex.Match(value, "^\"\\\\{0,1}.+\"$").Success)
            {
                value = value.Substring(1, value.Length - 2);
                string result = "";
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i] == '\\')
                    {
                        i++;
                        switch (value[i])
                        {
                            case 'a':
                                result += '\a';
                                break;
                            case 'b':
                                result += '\b';
                                break;
                            case 'f':
                                result += '\f';
                                break;
                            case 'n':
                                result += '\n';
                                break;
                            case 'r':
                                result += '\r';
                                break;
                            case 't':
                                result += '\t';
                                break;
                            case 'v':
                                result += '\v';
                                break;
                            default:
                                result += value[i];
                                break;
                        }
                        continue;
                    }
                    result += value[i];
                }
                return result;
            }
            throw new ArgumentException();
        }
    }
}
