using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Compiler.Compiler;

namespace Compiler
{
    public class Data
    {
        public short Address { get; }
        public short Size { get; }

        public Dictionary<string, Action<Data, Compiler, string[], bool>> BuildInFunction { get; } = new();

        public Data(short address, short size)
        {
            Address = address;
            Size = size;
            AddressArray = Enumerable.Range(Address, Size).Select(x => (short)x).ToArray();
        }

        public readonly short[] AddressArray;

        public static void DefaultInit<T>(Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.NeedCodeWriter();
            CompileError.MinLength(args.Length, 2, $"Data.DefaultInit<{typeof(T).Name}> min length");

            T v = comp.Memory.Add<T>(comp, comp.CodeWriter!, args[1]);
            if (args.Length >= 3)
            {
                v.BuildInFunction[ValueType.BuildInFunctions.Add](v, comp, new string[] { "", "", args[2] }, needReset);
            }
        }

        public static void ArrayInit(Compiler comp, string[] args, bool needReset)
        {
            comp.NeedCodeWriter();
            CompileError.MinLength(args.Length, 4, $"Data.ArrayInit min length");

            var t = comp.ValueTypes[args[2]];
            short amount = short.Parse(args[3]);
            Array s = comp.Memory.Add<Array>(comp.CodeWriter!, args[1], (short)(t.size * amount)
                , Array.ConstructorOf(t.size, amount, t.constructor));
            if (args.Length >= 4 + amount)
            {
                for (short i = 0; i < amount; i++)
                {
                    ValueType v = t.constructor((short)(s.Address + i));
                    try
                    {
                        v.BuildInFunction[ValueType.BuildInFunctions.Add](v, comp, new string[] { "", "", args[4 + i] }, needReset);
                    }
                    catch (CompileError e)
                    {
                        e.AddMessage("Data.ArrayInit");
                        throw e;
                    }
                }
            }
        }

        public static void StringInit(Compiler comp, string[] args, bool needReset)
        {
            comp.NeedCodeWriter();
            CompileError.MinLength(args.Length, 3, $"Data.StringInit min length");

            if (comp.Memory.ContainName(args[2]))
            {
                Data from = comp.Memory[args[2]];
                Data to = comp.Memory.Add<String>(comp.CodeWriter!, args[1], from.Size, String.ConstructorOf(from.Size));
                comp.CopyData(comp.CodeWriter!, from, to, false, needReset);
            }
            else if (short.TryParse(args[2], out short value))
            {
                comp.Memory.Add<String>(comp.CodeWriter!, args[1], value, String.ConstructorOf(value));
            }
            else
            {
                string stringValue = String.GetValue(args[2]);
                String s = comp.Memory.Add<String>(comp.CodeWriter!, args[1], (short)stringValue.Length, String.ConstructorOf((short)stringValue.Length));
                for (int i = 0; i < stringValue.Length; i++)
                {
                    comp.Move(comp.CodeWriter!, (short)(s.Address + i));
                    comp.CodeWriter!.Write(new string('+',
                        stringValue[i]), $"adding {stringValue[i]}");
                }
            }
        }

        public static void StructInit(Compiler comp, string[] args, bool needReset)
        {
            comp.NeedCodeWriter();
            CompileError.MinLength(args.Length, 2, $"Data.StructInit min length");

            var t = comp.ValueTypes[args[0]];
            Struct v = comp.Memory.Add<Struct>(comp.CodeWriter!, args[1], t.size, t.constructor);

            CompileError.MinLength(args.Length, 2 + v.Datas.Length, $"Data.StructInit min length with args");

            for (int i = 0; i < v.Datas.Length; i++)
            {
                try
                {
                    v.Datas[i].data.BuildInFunction[ValueType.BuildInFunctions.Set]
                    (v.Datas[i].data, comp, new string[] { "", "", args[2 + i] }, needReset);
                }
                catch (CompileError e)
                {
                    e.AddMessage("Data.StructInit");
                    throw e;
                }
            }
        }
    }
}
