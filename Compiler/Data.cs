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

        public virtual string Name => GetType().Name;

        public Data(short address, short size)
        {
            Address = address;
            Size = size;
            AddressArray = Enumerable.Range(Address, Size).Select(x => (short)x).ToArray();
        }

        public readonly short[] AddressArray;

        public static void Print(Data data, Compiler comp, string[] args, bool needReset)
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 2, "Need something to print");

            foreach (string arg in args.Skip(1))
            {
                Data v = comp.Memory![arg];
                for (short i = v.Address; i < v.Address + v.Size; i++)
                {
                    comp.CodeWriter!.Move(i);
                    comp.CodeWriter!.Write(".", "print");
                }
            }
        }

        public static void Move(Data data, Compiler comp, string[] args, bool needReset)
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 3, "move {to} {from}");

            Data from = comp.Memory![args[2]];
            Data to = comp.Memory![args[1]];

            if (from.Size != to.Size)
                throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"move {args[1]} {args[2]} not same size");

            comp.CodeWriter!.MoveData(from, to, false);
        }

        public static void DefaultInit<T>(Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 2, $"Data.DefaultInit<{typeof(T).Name}> min length");

            T v = comp.Memory!.Add<T>(args[1]);
            if (args.Length >= 3)
            {
                comp.DataTypes[v.Name].Functions[BuildInFunctions.Init](v, comp, new string[] { "", "", args[2] }, needReset);
            }
        }

        public static void ArrayInit(Compiler comp, string[] args, bool needReset)
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 4, $"Data.ArrayInit min length");

            var t = comp.ValueTypes[args[2]];
            short amount = short.Parse(args[3]);
            Array s = comp.Memory!.Add<Array>(args[1], (short)(t.size * amount)
                , Array.ConstructorOf(t.size, amount, t.constructor));
            if (args.Length >= 4 + amount)
            {
                for (short i = 0; i < amount; i++)
                {
                    ValueType v = t.constructor((short)(s.Address + i));
                    try
                    {
                        comp.DataTypes[v.Name].Functions[BuildInFunctions.Add](v, comp, new string[] { "", "", args[4 + i] }, needReset);
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
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 3, $"Data.StringInit min length");

            if (comp.Memory!.ContainName(args[2]))
            {
                Data from = comp.Memory![args[2]];
                Data to = comp.Memory!.Add<String>(args[1], from.Size, String.ConstructorOf(from.Size));
                comp.CodeWriter!.CopyData(from, to, false, needReset);
            }
            else if (short.TryParse(args[2], out short value))
            {
                comp.Memory!.Add<String>(args[1], value, String.ConstructorOf(value));
            }
            else
            {
                string stringValue = String.GetValue(args[2]);
                String s = comp.Memory!.Add<String>(args[1], (short)stringValue.Length, String.ConstructorOf((short)stringValue.Length));
                for (int i = 0; i < stringValue.Length; i++)
                {
                    comp.CodeWriter!.Add((short)(s.Address + i), stringValue[i], $"adding {stringValue[i]}");
                }
            }
        }

        public static void StructInit(Compiler comp, string[] args, bool needReset)
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 2, $"Data.StructInit min length");

            var t = comp.ValueTypes[args[0]];
            Struct v = comp.Memory!.Add<Struct>(args[1], t.size, t.constructor);
            v.name = args[0];

            CompileError.MinLength(args.Length, 2 + v.Datas.Length, $"Data.StructInit min length with args");

            for (int i = 0; i < v.Datas.Length; i++)
            {
                try
                {
                    comp.DataTypes[v.Datas[i].data.Name].Functions[BuildInFunctions.Set]
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
