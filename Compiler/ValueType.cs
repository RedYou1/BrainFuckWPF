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
        public ValueType(short address, short size) : base(address, size)
        {
            BuildInFunction.Add(BuildInFunctions.Set, Set);
        }

        public static void Set(Data data, Compiler comp, string[] args, bool needReset)
        {
            comp.NeedCodeWriter();

            for (short i = data.Address; i < data.Address + data.Size; i++)
            {
                comp.Move(comp.CodeWriter!, i);
                comp.CodeWriter!.Write("[-]", "reset Set");
            }
            try
            {
                data.BuildInFunction[BuildInFunctions.Add](data, comp, args, needReset);
            }
            catch (CompileError e)
            {
                e.AddMessage("ValueType.Set");
                throw e;
            }
        }


        public static void BaseAdd<T>(char op, Func<string, int> amount, Data data, Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.NeedCodeWriter();
            CompileError.MinLength(args.Length, 3, $"ValueType.BaseAdd min length");

            if (comp.Memory.ContainName(args[2]))
            {
                Data from = comp.Memory[args[2]];
                if (from.Size != comp.ValueTypes[typeof(T).Name].size)
                    throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "ValueType.BaseAdd data size");

                comp.CopyData(comp.CodeWriter!, from, data, false, needReset);
            }
            else
            {
                comp.Move(comp.CodeWriter!, (short)(data.Address + comp.ValueTypes[typeof(T).Name].size - 1));
                int value = amount(args[2]);

                string s = $"{op}";

                StringBuilder sb = new StringBuilder(1 + (5 * (comp.ValueTypes[typeof(T).Name].size - 1))).Insert(0, s);
                for (int i = 0; i < comp.ValueTypes[typeof(T).Name].size - 1; i++)
                {
                    sb.Insert(1 + 3 * i, $"[<{op}>]");
                }
                string adder = sb.ToString();

                comp.CodeWriter!.Write(
                    new StringBuilder(adder.Length * value).Insert(0, adder, value).ToString()
                    , $"adding {value}");
            }
        }
    }
}
