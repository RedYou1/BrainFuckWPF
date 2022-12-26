﻿using System;
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
            : base(address, elementSize, amount, ValueType.Types[nameof(Char)].constructor)
        {
            BuildInFunction.Add(ValueType.BuildInFunctions.Set, Set);
        }

        public static Func<short, String> ConstructorOf(short amount)
            => (address) => new String(address, ValueType.Types[nameof(Char)].size, amount);


        public ReturnCode Set(Data data, Compiler comp, string[] args, bool needReset)
        {
            if (comp.CodeWriter is null)
                return ReturnCode.WrongStart;

            for (short i = Address; i < Address + Size; i++)
            {
                comp.Move(comp.CodeWriter, i);
                comp.CodeWriter.Write("[-]", "reset Set");
            }
            if (comp.Memory.ContainName(args[2]))
            {
                Data from = comp.Memory[args[2]];
                comp.CopyData(comp.CodeWriter, from, this, true, needReset);
            }
            else
            {
                string value = GetValue(args[2]);
                if (value.Length > Size)
                    throw new Exception($"value {args[2]} dont fit in string of {Size}");
                for (short i = 0; i < value.Length; i++)
                {
                    comp.Move(comp.CodeWriter, (short)(Address + i));
                    comp.CodeWriter.Write(new string('+', (byte)value[i]), $"set string {i} to {value[i]}");
                }
            }
            return ReturnCode.OK;
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
