using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compiler
{
    public class String : Array<Char>
    {
        public String(short address, short elementSize, short amount)
            : base(address, elementSize, amount) { }

        public static Func<short, String> ConstructorOf(short amount)
            => (address) => new String(address, ValueType.Types[nameof(Char)].size, amount);

        public Char Get(short index)
            => new Char((short)(Address + index), ValueType.Types[nameof(Char)].size);

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
