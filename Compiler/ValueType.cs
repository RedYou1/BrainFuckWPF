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
                data.BuildInFunction[BuildInFunctions.Init](data, comp, args, needReset);
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
                byte[] value = amount(args[2]).Take(data.Size).ToArray();

                for (int i = 0; i < value.Length; i++)
                {
                    comp.Move(comp.CodeWriter!, (short)(data.Address + i));
                    comp.CodeWriter!.Write(new string('+', value[i]), $"set {value[i]}");
                }
            }
        }


        public static void BaseAdd<T>(Func<string, byte[]> amount, Data data, Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.NeedCodeWriter();
            CompileError.MinLength(args.Length, 3, $"ValueType.BaseAdd min length");

            if (comp.Memory.ContainName(args[2]))
            {
                Data from = comp.Memory[args[2]];
                if (from.Size != comp.ValueTypes[typeof(T).Name].size)
                    throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "ValueType.BaseAdd data size");

                comp.CopyData(comp.CodeWriter!, from, data, false, needReset);//TODO overflow
            }
            else
            {
                comp.Memory.PushStack();

                Bool overflow = comp.Memory.Add<Bool>(comp, comp.CodeWriter!, " overflow ");
                Byte temp = comp.Memory.Add<Byte>(comp, comp.CodeWriter!, " temp ");

                void Add(short address, int remainingRecursive)
                {
                    comp.Move(comp.CodeWriter!, address);
                    comp.CodeWriter!.Write("+", "add 1");

                    if (remainingRecursive > 0)
                    {
                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("[-]+", "set to 1");

                        comp.Move(comp.CodeWriter!, address);
                        comp.CodeWriter!.Write("[", "does not overflow");
                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("-", "set to 0");

                        comp.MoveData(comp.CodeWriter!, address, temp.Address, false);

                        comp.Move(comp.CodeWriter!, address);
                        comp.CodeWriter!.Write("]", "end not overflow");

                        comp.MoveData(comp.CodeWriter!, temp.Address, address, false);

                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("[", "if overflow");
                        comp.CodeWriter!.Write("-", "set to 0");
                        Add((short)(address - 1), remainingRecursive - 1);
                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("]", "end overflow");
                    }
                }

                byte[] value = amount(args[2]).Take(data.Size).ToArray();

                for (int i = 0; i < value.Length; i++)
                {
                    for (int y = 0; y < value[i]; y++)
                        Add((short)(data.Address + data.Size - 1 - i), value.Length - 1 - i);
                }

                comp.Memory.PopStack(comp.CodeWriter!, needReset);
            }
        }

        public static void BaseSub<T>(Func<string, byte[]> amount, Data data, Compiler comp, string[] args, bool needReset)
            where T : ValueType
        {
            comp.NeedCodeWriter();
            CompileError.MinLength(args.Length, 3, $"ValueType.BaseSub min length");

            if (comp.Memory.ContainName(args[2]))
            {
                Data from = comp.Memory[args[2]];
                if (from.Size != comp.ValueTypes[typeof(T).Name].size)
                    throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "ValueType.BaseSub data size");

                comp.CopyData(comp.CodeWriter!, from, data, false, needReset);//TODO underflow
            }
            else
            {
                comp.Memory.PushStack();

                Bool overflow = comp.Memory.Add<Bool>(comp, comp.CodeWriter!, " underflow ");
                Byte temp = comp.Memory.Add<Byte>(comp, comp.CodeWriter!, " temp ");

                void Sub(short address, int remainingRecursive)
                {
                    if (remainingRecursive > 0)
                    {
                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("[-]+", "set to 1");

                        comp.Move(comp.CodeWriter!, address);
                        comp.CodeWriter!.Write("[", "does not overflow");
                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("-", "set to 0");

                        comp.MoveData(comp.CodeWriter!, address, temp.Address, false);

                        comp.Move(comp.CodeWriter!, address);
                        comp.CodeWriter!.Write("]", "end not overflow");

                        comp.MoveData(comp.CodeWriter!, temp.Address, address, false);

                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("[", "if overflow");
                        comp.CodeWriter!.Write("-", "set to 0");
                        Sub((short)(address - 1), remainingRecursive - 1);
                        comp.Move(comp.CodeWriter!, overflow.Address);
                        comp.CodeWriter!.Write("]", "end overflow");
                    }

                    comp.Move(comp.CodeWriter!, address);
                    comp.CodeWriter!.Write("-", "sub 1");
                }

                byte[] value = amount(args[2]).Take(data.Size).ToArray();

                for (int i = 0; i < value.Length; i++)
                {
                    for (int y = 0; y < value[i]; y++)
                        Sub((short)(data.Address + data.Size - 1 - i), value.Length - 1 - i);
                }

                comp.Memory.PopStack(comp.CodeWriter!, needReset);
            }
        }
    }
}
