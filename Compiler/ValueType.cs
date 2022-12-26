using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static Compiler.Compiler;

namespace Compiler
{
    public class ValueType : Data
    {
        public class BuildInFunctions
        {
            public const string Set = "set";
            public const string Add = "add";
            public const string Sub = "sub";
        }

        public ValueType(short address, short size) : base(address, size)
        {
            BuildInFunction.Add(BuildInFunctions.Set, Set);
        }

        public static ReturnCode Set(Data data, Compiler comp, string[] args, bool needReset)
        {
            if (comp.CodeWriter is null)
                return ReturnCode.WrongStart;
            for (short i = data.Address; i < data.Address + data.Size; i++)
            {
                comp.Move(comp.CodeWriter, i);
                comp.CodeWriter.Write("[-]", "reset Set");
            }
            return data.BuildInFunction[BuildInFunctions.Add](data, comp, args, needReset);
        }


        public static Dictionary<string, (short size, Func<short, ValueType> constructor)> Types =
        new(){
            { nameof(Bool),(1,Bool.Constructor) },
            { nameof(Byte),(1,Byte.Constructor) },
            { nameof(Char),(1,Char.Constructor) },
            { nameof(Short),(2,Short.Constructor) },
            { nameof(Int),(4,Int.Constructor) }
        };


        public static ReturnCode BaseAdd<T>(char op, Func<string, int> amount, Data data, Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            if (comp.CodeWriter == null)
                return ReturnCode.WrongStart;
            if (args.Length < 3)
            {
                return ReturnCode.BadArgs;
            }

            if (comp.Memory.ContainName(args[2]))
            {
                Data from = comp.Memory[args[2]];
                if (from.Size != Types[typeof(T).Name].size)
                    return ReturnCode.BadArgs;

                comp.CopyData(comp.CodeWriter, from, data, false, needReset);
            }
            else
            {
                comp.Move(comp.CodeWriter, (short)(data.Address + Types[typeof(T).Name].size - 1));
                int value = amount(args[2]);

                string s = $"{op}";

                StringBuilder sb = new StringBuilder(1 + (5 * (Types[typeof(T).Name].size - 1))).Insert(0, s);
                for (int i = 0; i < Types[typeof(T).Name].size - 1; i++)
                {
                    sb.Insert(1 + 3 * i, $"[<{op}>]");
                }
                string adder = sb.ToString();

                comp.CodeWriter.Write(
                    new StringBuilder(adder.Length * value).Insert(0, adder, value).ToString()
                    , $"adding {value}");
            }
            return ReturnCode.OK;
        }
    }
}
