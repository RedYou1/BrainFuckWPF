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
        public ValueType(short address, short size) : base(address, size) { }

        public static void Set(Data data, Compiler comp, string[] args, bool needReset)
        {
            comp.IsMainFile();

            for (short i = data.Address; i < data.Address + data.Size; i++)
            {
                comp.CodeWriter!.Set(i, 0, "reset Set");
            }
            try
            {
                comp.DataTypes[data.Name].Functions[BuildInFunctions.Init](data, comp, args, needReset);
            }
            catch (CompileError e)
            {
                e.AddMessage("ValueType.Set");
                throw e;
            }
        }

        public static void BaseInit<T>(Func<string, byte[]> amount, Data data, Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 3, $"ValueType.BaseAdd min length");

            if (comp.Memory!.ContainName(args[2]))
            {
                Data from = comp.Memory![args[2]];
                if (from.Size != comp.ValueTypes[typeof(T).Name].size)
                    throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "ValueType.BaseAdd data size");

                comp.CodeWriter!.CopyData(from, data, false, needReset);
            }
            else
            {
                byte[] value = amount(args[2]).Take(data.Size).ToArray();

                for (int i = 0; i < value.Length; i++)
                {
                    comp.CodeWriter!.Add((short)(data.Address + i), value[i], $"set {value[i]}");
                }
            }
        }


        public static void BaseAdd<T>(Func<string, byte[]> amount, Data data, Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 3, $"ValueType.BaseAdd min length");

            if (comp.Memory!.ContainName(args[2]))
            {
                Data from = comp.Memory![args[2]];
                if (from.Size != comp.ValueTypes[typeof(T).Name].size)
                    throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "ValueType.BaseAdd data size");

                comp.CodeWriter!.CopyData(from, data, false, needReset);//TODO overflow
            }
            else
            {
                byte[] value = amount(args[2]).Take(data.Size).ToArray();

                short address = (short)(data.Address + data.Size - 1);

                if (value.Length == 1)
                {
                    comp.CodeWriter!.Add(address, value[0], $"add {value[0]}");
                    return;
                }


                comp.Memory!.PushStack();

                Bool overflow = comp.Memory!.Add<Bool>(" overflow ");
                Byte temp = comp.Memory!.Add<Byte>(" temp ");

                void Add(short address, int remainingRecursive)
                {
                    comp.CodeWriter!.Add(address, 1, "add 1");

                    if (remainingRecursive > 0)
                    {
                        comp.CodeWriter!.Set(overflow.Address, 1, "set to 1");

                        comp.CodeWriter!.IfKeep(address,
                            () => comp.CodeWriter!.Add(overflow.Address, -1, "set to 0"),
                            "does not overflow", "end not overflow", temp.Address);

                        comp.CodeWriter!.IfToZero(overflow.Address,
                            () => Add((short)(address - 1), remainingRecursive - 1),
                            "if overflow", "end overflow");
                    }
                }

                int remainingRecursive = value.Length - 1;

                for (int i = 0; i < value.Length; i++, remainingRecursive--, address--)
                {
                    for (int y = 0; y < value[i]; y++)
                    {
                        Add(address, remainingRecursive);
                    }
                }

                comp.Memory!.PopStack(needReset);
            }
        }

        public static void BaseSub<T>(Func<string, byte[]> amount, Data data, Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.IsMainFile();
            CompileError.MinLength(args.Length, 3, $"ValueType.BaseSub min length");

            if (comp.Memory!.ContainName(args[2]))
            {
                Data from = comp.Memory![args[2]];
                if (from.Size != comp.ValueTypes[typeof(T).Name].size)
                    throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "ValueType.BaseSub data size");

                comp.CodeWriter!.CopyData(from, data, false, needReset);//TODO underflow
            }
            else
            {
                byte[] value = amount(args[2]).Take(data.Size).ToArray();

                short address = (short)(data.Address + data.Size - 1);

                if (value.Length == 1)
                {
                    comp.CodeWriter!.Add(address, -value[0], $"sub {value[0]}");
                    return;
                }

                comp.Memory!.PushStack();

                Bool overflow = comp.Memory!.Add<Bool>(" underflow ");
                Byte temp = comp.Memory!.Add<Byte>(" temp ");

                void Sub(short address, int remainingRecursive)
                {
                    if (remainingRecursive > 0)
                    {
                        comp.CodeWriter!.Set(overflow.Address, 1, "set to 1");

                        comp.CodeWriter!.IfKeep(address,
                            () => comp.CodeWriter!.Add(overflow.Address, -1, "set to 0"),
                            "does not overflow", "end not overflow", temp.Address);

                        comp.CodeWriter!.IfToZero(overflow.Address,
                            () => Sub((short)(address - 1), remainingRecursive - 1),
                            "if overflow", "end overflow");
                    }

                    comp.CodeWriter!.Add(address, -1, "sub 1");
                }

                int remainingRecursive = value.Length - 1;

                for (int i = 0; i < value.Length; i++, remainingRecursive--, address--)
                {
                    for (int y = 0; y < value[i]; y++)
                        Sub(address, remainingRecursive);
                }

                comp.Memory!.PopStack(needReset);
            }
        }
    }
}
