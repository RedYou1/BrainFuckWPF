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

        public Dictionary<string, Func<Data, Compiler, string[], bool, ReturnCode>> BuildInFunction { get; } = new();

        public Data(short address, short size)
        {
            Address = address;
            Size = size;
            AddressArray = Enumerable.Range(Address, Size).Select(x => (short)x).ToArray();
        }

        public readonly short[] AddressArray;

        public static Dictionary<string, Func<Compiler, string[], bool, ReturnCode>> Types =
        new(){
            { nameof(Bool), DefaultInit<Bool> },
            { nameof(Byte), DefaultInit<Byte> },
            { nameof(Char), DefaultInit<Char> },
            { nameof(Short), DefaultInit<Short> },
            { nameof(Int), DefaultInit<Int> },

            { nameof(Array), ArrayInit },
            { nameof(String), StringInit },
        };

        private static ReturnCode DefaultInit<T>(Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            if (comp.CodeWriter is null)
                return ReturnCode.WrongStart;
            if (args.Length < 2)
            {
                return ReturnCode.BadArgs;
            }
            T v = comp.Memory.Add<T>(comp.CodeWriter, args[1]);
            if (args.Length >= 3)
            {
                return v.BuildInFunction[ValueType.BuildInFunctions.Add](v, comp, new string[] { "", "", args[2] }, needReset);
            }
            return ReturnCode.OK;
        }

        public static ReturnCode ArrayInit(Compiler comp, string[] args, bool needReset)
        {
            if (comp.CodeWriter is null)
                return ReturnCode.WrongStart;
            if (args.Length < 4)
            {
                return ReturnCode.BadArgs;
            }
            var t = ValueType.Types[args[2]];
            short amount = short.Parse(args[3]);
            Array s = comp.Memory.Add<Array>(comp.CodeWriter, args[1], (short)(t.size * amount)
                , Array.ConstructorOf(t.size, amount, t.constructor));
            if (args.Length >= 4 + amount)
            {
                for (short i = 0; i < amount; i++)
                {
                    ValueType v = t.constructor((short)(s.Address + i));
                    ReturnCode r = v.BuildInFunction[ValueType.BuildInFunctions.Add](v, comp, new string[] { "", "", args[4 + i] }, needReset);
                    if (r != ReturnCode.OK)
                        return r;
                }
            }
            return ReturnCode.OK;
        }

        public static ReturnCode StringInit(Compiler comp, string[] args, bool needReset)
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
                Data to = comp.Memory.Add<String>(comp.CodeWriter, args[1], from.Size, String.ConstructorOf(from.Size));
                comp.CopyData(comp.CodeWriter, from, to, false, needReset);
            }
            else if (short.TryParse(args[2], out short value))
            {
                comp.Memory.Add<String>(comp.CodeWriter, args[1], value, String.ConstructorOf(value));
            }
            else
            {
                string stringValue = String.GetValue(args[2]);
                String s = comp.Memory.Add<String>(comp.CodeWriter, args[1], (short)stringValue.Length, String.ConstructorOf((short)stringValue.Length));
                for (int i = 0; i < stringValue.Length; i++)
                {
                    comp.Move(comp.CodeWriter, (short)(s.Address + i));
                    comp.CodeWriter.Write(new string('+',
                        stringValue[i]), $"adding {stringValue[i]}");
                }
            }
            return ReturnCode.OK;
        }

        public static ReturnCode StructInit(Compiler comp, string[] args, bool needReset)
        {
            if (comp.CodeWriter == null)
                return ReturnCode.WrongStart;
            if (args.Length < 2)
            {
                return ReturnCode.BadArgs;
            }

            var t = ValueType.Types[args[0]];
            Struct v = comp.Memory.Add<Struct>(comp.CodeWriter, args[1], t.size, t.constructor);

            if (args.Length < 2 + v.Datas.Length)
            {
                return ReturnCode.BadArgs;
            }

            for (int i = 0; i < v.Datas.Length; i++)
            {
                ReturnCode r = v.Datas[i].data.BuildInFunction[ValueType.BuildInFunctions.Set]
                    (v.Datas[i].data, comp, new string[] { "", "", args[2 + i] }, needReset);
                if (r != ReturnCode.OK)
                    return r;
            }
            return ReturnCode.OK;
        }
    }
}
