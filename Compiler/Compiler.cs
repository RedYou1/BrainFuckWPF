using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Compiler
{
    public class Compiler
    {
        public static bool Debug = true;

        public static CompileError? Compile(string sourcePath, string startingFile, string resultPath = "", Compiler? comp = null)
        {
            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
            {
                return new CompileError(CompileError.ReturnCodeEnum.SourceDontExists, "The sourcePath doesn't exists.");
            }

            if (comp == null)
                comp = new Compiler(sourcePath, null);

            if (File.Exists(startingFile + ".b"))
            {
                try
                {
                    Compile(getFileCommands(File.ReadAllText(startingFile + ".b")), comp, false);
                }
                catch (CompileError e)
                {
                    return e;
                }
            }

            if (File.Exists(startingFile + ".f"))
            {
                StreamWriter? sw = null;
                if (resultPath != "")
                {
                    sw = File.CreateText(resultPath);
                    comp.CodeWriter = new CodeWriter(sw);
                }
                try
                {
                    Compile(getFileCommands(File.ReadAllText(startingFile + ".f")), comp, false);
                }
                catch (CompileError e)
                {
                    return e;
                }
                finally
                {
                    sw?.Close();
                }
            }

            return null;
        }

        public Dictionary<string, BFFunction> BFFunctions { get; } = new();

        public Dictionary<string, Action<Compiler, string[], bool>> DataTypes =
        new(){
            { nameof(Bool), Data.DefaultInit<Bool> },
            { nameof(Byte), Data.DefaultInit<Byte> },
            { nameof(Char), Data.DefaultInit<Char> },
            { nameof(Short), Data.DefaultInit<Short> },
            { nameof(Int), Data.DefaultInit<Int> },

            { nameof(Array), Data.ArrayInit },
            { nameof(String), Data.StringInit },
        };

        public Dictionary<string, (short size, Func<short, ValueType> constructor)> ValueTypes =
        new(){
            { nameof(Bool),(Bool.BytesSize,Bool.Constructor) },
            { nameof(Byte),(Byte.BytesSize,Byte.Constructor) },
            { nameof(Char),(Char.BytesSize,Char.Constructor) },
            { nameof(Short),(Short.BytesSize,Short.Constructor) },
            { nameof(Int),(Int.BytesSize,Int.Constructor) }
        };

        public Compiler(string path, StreamWriter? streamWriter)
        {
            Path = path;
            if (streamWriter != null)
                CodeWriter = new(streamWriter);
            Memory = new(this);
            Memory.PushStack();
        }

        public void NeedCodeWriter()
        {
            CompileError.NotNull(CodeWriter, "Tried to do an action without being in the starting file .f");
        }

        public string Path { get; }

        public CodeWriter? CodeWriter { get; private set; }
        public Memory Memory { get; }
        public short actualPtr { get; protected set; } = 0;

        private static string[] getFileCommands(string file)
        {
            List<string> values = new();
            Stack<char> stack = new();
            string currentValue = "";
            for (int i = 0; i < file.Length; i++)
            {
                char currentChar = file[i];
                if (currentChar == '\\')
                {
                    i++;
                    currentValue += currentChar;
                    currentValue += file[i];
                    continue;
                }


                char stacktop = ' ';
                if (stack.TryPeek(out stacktop))
                {
                    if (stacktop == '\'' || stacktop == '"')
                    {
                        if (currentChar == stacktop)
                        {
                            stack.Pop();
                            currentValue += currentChar;
                        }
                        else
                        {
                            currentValue += currentChar;
                        }
                        continue;
                    }
                }

                if (currentChar == '{')
                {
                    while (currentValue.Last() == (char)1)
                    {
                        currentValue = currentValue.Substring(0, currentValue.Length - 1);
                    }
                    if (stack.Count == 0)
                    {
                        currentValue += (char)1;
                        currentValue += ";";
                    }
                    else
                    {
                        currentValue += "{";
                    }
                    stack.Push(currentChar);
                    continue;
                }

                if (currentChar == '}')
                {
                    if (stacktop == '{')
                    {
                        stack.Pop();
                        if (stack.Count == 0)
                        {
                            values.Add(currentValue);
                            currentValue = "";
                        }
                        else
                        {
                            currentValue += "}";
                        }
                        continue;
                    }
                    throw new FileLoadException("Content Exception");
                }

                if (currentChar == '\'' || stacktop == '"')
                {
                    if (stacktop == currentChar)
                    {
                        stack.Pop();
                    }
                    else
                    {
                        stack.Push(currentChar);
                    }
                }

                if (currentChar == ';' && stacktop != '{')
                {
                    values.Add(currentValue);
                    currentValue = "";
                }
                else if (currentChar > (char)31 &&
                    !((currentChar == ' ' || currentChar == '\t') && stacktop == '{' &&
                        (
                        currentValue.Last() == ';' ||
                        currentValue.Last() == '{' ||
                        currentValue.Last() == '}'
                        )))
                {
                    if (currentChar == ' ' && currentValue.Last() == (char)1)
                    {
                        continue;
                    }
                    if (stacktop == '{' && currentChar == ' ')
                    {
                        currentValue += (char)2;
                    }
                    else if (stacktop != '\'' && stacktop != '"' && currentChar == ' ')
                    {
                        currentValue += (char)1;
                    }
                    else
                    {
                        currentValue += currentChar;
                    }
                }
            }
            return values.ToArray();
        }

        private static void Compile(string[] commands, Compiler comp, bool needReset)
        {
            foreach (string line in commands)
            {
                string[] args = line.Split((char)1);
                comp.compileLine(args, needReset);
            }
        }

        public void Move(CodeWriter codeWriter, short moveTo)
        {
            if (moveTo != actualPtr)
            {
                codeWriter.Write(
                    new string(moveTo > actualPtr ? '>' : '<',
                    Math.Abs(moveTo - actualPtr)), $"move to {moveTo}");
                actualPtr = moveTo;
            }
        }

        public void CopyData(CodeWriter codeWriter, Data from, Data to, bool set2ToZero, bool needReset)
        {
            if (from.Address == to.Address)
                return;
            if (from.Size != to.Size)
                throw new Exception("CopyData dont have the same size");

            for (int i = 0; i < from.Size; i++)
            {
                CopyData(codeWriter, from.AddressArray[i], to.AddressArray[i], set2ToZero, needReset);
            }
        }

        public void CopyData(CodeWriter codeWriter, short from, short to, bool set2ToZero, bool needReset)
        {
            if (from == to)
                return;
            if (set2ToZero)
            {
                if (to != actualPtr)
                    Move(codeWriter, to);
                codeWriter.Write("[-]", "reset to 0");
            }
            if (from != actualPtr)
                Move(codeWriter, from);

            Memory.PushStack();

            Byte temp = Memory.Add<Byte>(this, codeWriter, " copyData ");

            short moveAmountA = (short)Math.Abs(to - from);
            bool dirA = to > from;
            short moveAmountB = (short)Math.Abs(temp.Address - to);
            bool dirB = temp.Address > to;
            short moveAmountC = (short)Math.Abs(from - temp.Address);
            bool dirC = from > temp.Address;
            codeWriter.Write(
                $"""
                 [-{new string(dirA ? '>' : '<', moveAmountA)}
                  +{new string(dirB ? '>' : '<', moveAmountB)}
                  +{new string(dirC ? '>' : '<', moveAmountC)}]
                 """, $"Duplicate data from {from} to {from} and {temp.Address}");
            MoveData(codeWriter, temp.Address, from, false);

            Memory.PopStack(codeWriter, needReset);
        }

        public void MoveData(CodeWriter codeWriter, Data from, Data to, bool set2ToZero)
        {
            if (from.Address == to.Address)
                return;
            if (from.Size != to.Size)
                throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "MoveData dont have the same size");

            for (int i = 0; i < from.Size; i++)
            {
                MoveData(codeWriter, from.AddressArray[i], to.AddressArray[i], set2ToZero);
            }
        }

        public void MoveData(CodeWriter codeWriter, short from, short to, bool set2ToZero)
        {
            if (from == to)
                return;
            if (set2ToZero)
            {
                if (to != actualPtr)
                    Move(codeWriter, to);
                codeWriter.Write("[-]", "reset to 0");
            }
            if (from != actualPtr)
                Move(codeWriter, from);
            short moveAmount = (short)Math.Abs(to - from);
            bool dir = to > from;
            codeWriter.Write($"[-{new string(dir ? '>' : '<', moveAmount)}+{new string(dir ? '<' : '>', moveAmount)}]", $"Add between {from} and {to}");
        }

        private void compileLine(string[] args, bool needReset)
        {
            switch (args[0])
            {
                case "include":
                    {
                        CompileError.MinLength(args.Length, 2, "Need sometging to include");
                        CompileError? c = Compile(Path, Path + args[1], "", this);
                        if (c is not null)
                        {
                            c.AddMessage($"include {args[1]}");
                            throw c;
                        }
                        break;
                    }
                case "//":
                    NeedCodeWriter();
                    CodeWriter!.Write("", string.Join(' ', args.Skip(1).ToArray()));
                    break;
                case "input":
                    {
                        NeedCodeWriter();
                        CompileError.MinLength(args.Length, 2, "input args: {name} {type or length default Char}");

                        if (args.Length == 2)
                        {
                            Char v = Memory.Add<Char>(this, CodeWriter!, args[1]);
                            Move(CodeWriter!, v.Address);
                            CodeWriter!.Write(",", "input");
                        }
                        else if (short.TryParse(args[2], out short amount))
                        {
                            String s = Memory.Add<String>(CodeWriter!, args[1], amount, String.ConstructorOf(amount));
                            Move(CodeWriter!, s.Address);
                            CodeWriter!.Write(new string(',', amount), "input");
                        }
                        else if (ValueTypes.ContainsKey(args[2]))
                        {
                            var t = ValueTypes[args[2]];
                            Data d = Memory.Add(CodeWriter!, args[1], t.size, t.constructor);
                            Move(CodeWriter!, d.Address);
                            CodeWriter!.Write(new string(',', t.size), "input");
                        }
                        else
                        {
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "input args: {name} {type or length default Char}");
                        }
                        break;
                    }
                case "while":
                    {
                        NeedCodeWriter();
                        CompileError.MinLength(args.Length, 3, "structure: while {name}\n{\n}");

                        short address = Memory[args[1]].Address;
                        Move(CodeWriter!, address);
                        CodeWriter!.Write("[", $"check {address}");
                        Memory.PushStack();
                        try
                        {
                            Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)2, ' ')), this, true);
                        }
                        catch (CompileError e)
                        {
                            e.AddMessage($"while {args[1]}");
                            throw e;
                        }
                        Memory.PopStack(CodeWriter, true);
                        Move(CodeWriter, address);
                        CodeWriter.Write("]", $"end of {address}");
                        break;
                    }
                case "foreach":
                    {
                        NeedCodeWriter();
                        CompileError.MinLength(args.Length, 4, "structure: foreach {arrayName} {elementName}\n{\n}");
                        if (!Memory.ContainName(args[1]) || Memory[args[1]] is not Array arr)
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"foreach arrayName ({args[1]}) doesn't exists or isn't an array");

                        string[] commands = getFileCommands(new string(args[3].Skip(1).ToArray()).Replace((char)2, ' '));

                        for (short i = 0; i < arr.Amount; i++)
                        {
                            Data element = arr.Get(i);
                            Memory.PushStack();
                            Memory.AddToCurrent(args[2], element, true);
                            try
                            {
                                Compile(commands, this, true);
                            }
                            catch (CompileError e)
                            {
                                e.AddMessage($"foreach {args[1]} {args[2]}");
                                throw e;
                            }
                            Memory.PopStack(CodeWriter!, i + 1 == arr.Amount ? needReset : true);
                        }
                        break;
                    }
                case "if":
                    {
                        NeedCodeWriter();
                        CompileError.MinLength(args.Length, 3, "structure: if {name}\n{\n}");

                        short address = Memory[args[1]].Address;
                        Move(CodeWriter!, address);
                        CodeWriter!.Write("[", $"check {address}");
                        Memory.PushStack();

                        try
                        {
                            Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)2, ' ')), this, needReset);
                        }
                        catch (CompileError e)
                        {
                            e.AddMessage($"if {args[1]}");
                            throw e;
                        }

                        Byte v = Memory.Add<Byte>(this, CodeWriter, " if ");
                        Move(CodeWriter, v.Address);
                        Memory.PopStack(CodeWriter, needReset);
                        CodeWriter.Write("]", $"end of {address}");
                        break;
                    }
                case "struct":
                    {
                        CompileError.MinLength(args.Length, 4, "structure: struct {name} ({type} {name}) at least one");
                        if (args.Length % 2 != 0)
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "structure: struct {name} ({type} {name}) at least one");
                        if (DataTypes.ContainsKey(args[1]))
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"The struct name ({args[1]}) already exists.");

                        short size = 0;// -1 -> not ValueType
                        Dictionary<string, Func<short, Data>> datas = new();
                        for (int i = 2; i < args.Length; i += 2)
                        {
                            if (datas.ContainsKey(args[i + 1]))
                                throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"The name ({args[i + 1]}) already exists in struct.");

                            Func<short, Data> constructor;
                            if (ValueTypes.ContainsKey(args[i]))
                            {
                                var type = ValueTypes[args[i]];
                                if (size != -1)
                                    size += type.size;
                                constructor = type.constructor;
                            }
                            else if (DataTypes.ContainsKey(args[i]))
                            {
                                size = -1;
                                throw new NotImplementedException();
                            }
                            else
                            {
                                throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"The type ({args[i]}) doesn't exists.");
                            }

                            datas.Add(args[i + 1], constructor);
                        }
                        DataTypes.Add(args[1], Data.StructInit);

                        if (size != -1)
                        {
                            ValueTypes.Add(args[1], (size, (address) =>
                            {
                                short i = 0;
                                List<(string, Data)> sdatas = new();
                                foreach (var e in datas)
                                {
                                    Data d = e.Value((short)(address + i));
                                    sdatas.Add((e.Key, d));
                                    i += d.Size;
                                }
                                return new Struct(address, sdatas.ToArray());
                            }
                            ));
                        }
                        break;
                    }
                case "func":
                    {
                        CompileError.MinLength(args.Length, 3, "structure: func {name} ({type} {name})\n{\n}");
                        if (args.Length % 2 == 0)
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "structure: func {name} ({type} {name}) at least one\n{\n}");

                        string[] to = new string[(args.Length - 3) / 2];
                        for (int i = 3; i < args.Length - 1; i += 2)
                        {
                            to[(i - 3) / 2] = args[i];
                        }
                        BFFunctions.Add(args[1], new BFFunction(args.Length / 2 - 1,
                            (Compiler comp, CodeWriter codeWriter, string[] args2, bool needReset) =>
                        {
                            comp.Memory.PushFunc(args2, to);
                            try
                            {
                                Compile(getFileCommands(new string(args[args.Length - 1].Skip(1).ToArray()).Replace((char)2, ' ')), this, needReset);
                            }
                            catch (CompileError e)
                            {
                                e.AddMessage($"func {args[1]}");
                                throw e;
                            }
                            comp.Memory.PopFunc(codeWriter, needReset);
                        }));
                        break;
                    }
                case "call":
                    {
                        NeedCodeWriter();
                        CompileError.MinLength(args.Length, 2, "structure: call {name} {args}");
                        if (!BFFunctions.ContainsKey(args[1]))
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"function {args[1]} doesn't exists.");

                        BFFunction func = BFFunctions[args[1]];
                        if (args.Length < 2 + func.NumberArgs)
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"bad amount of args for the function {args[1]}.");

                        try
                        {
                            func.Action(this, CodeWriter!, args.Skip(2).ToArray(), needReset);
                        }
                        catch (CompileError e)
                        {
                            e.AddMessage(string.Join(' ', args));
                            throw e;
                        }
                        break;
                    }
                default:
                    if (DataTypes.ContainsKey(args[0]))
                    {
                        DataTypes[args[0]].Invoke(this, args, needReset);
                        return;
                    }
                    if (Memory.ContainName(args[1]))
                    {
                        Data d = Memory[args[1]];
                        if (d.BuildInFunction.ContainsKey(args[0]))
                        {
                            d.BuildInFunction[args[0]].Invoke(d, this, args, needReset);
                            return;
                        }
                    }
                    throw new CompileError(CompileError.ReturnCodeEnum.BadCommand, $"this command doesn't exists {string.Join(' ', args)}");
            }
        }
    }
}