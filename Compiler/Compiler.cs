using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Compiler
{
    public class Compiler
    {
        public static bool Debug = true;

        public enum ReturnCode
        {
            OK,
            SourceDontExists,
            ResultAlreadyExists,
            BadCommand,
            BadArgs,
            WrongStart
        }

        public static ReturnCode Compile(string sourcePath, string startingFile, string resultPath = "", Compiler? comp = null)
        {
            if (!Directory.Exists(sourcePath) && !File.Exists(sourcePath))
            {
                return ReturnCode.SourceDontExists;
            }

            if (comp == null)
                comp = new Compiler(sourcePath, null);

            if (!File.Exists(startingFile + ".b"))
            {
                return ReturnCode.SourceDontExists;
            }
            ReturnCode returnCode = Compile(getFileCommands(File.ReadAllText(startingFile + ".b")), comp, false);
            if (returnCode != ReturnCode.OK)
                return returnCode;

            if (File.Exists(startingFile + ".f"))
            {
                StreamWriter? sw = null;
                if (resultPath != "")
                {
                    sw = File.CreateText(resultPath);
                    comp.CodeWriter = new CodeWriter(sw);
                }
                returnCode = Compile(getFileCommands(File.ReadAllText(startingFile + ".f")), comp, false);
                sw?.Close();
            }

            return returnCode;
        }

        public Dictionary<string, BFFunction> BFFunctions { get; } = new();

        public Compiler(string path, StreamWriter? streamWriter)
        {
            Path = path;
            if (streamWriter != null)
                CodeWriter = new(streamWriter);
            Memory = new(this);
            Memory.PushStack();
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

        private static ReturnCode Compile(string[] commands, Compiler comp, bool needReset)
        {
            foreach (string line in commands)
            {
                string[] args = line.Split((char)1);
                ReturnCode status = comp.compileLine(args, needReset);
                if (status != ReturnCode.OK)
                {
                    return status;
                }
            }
            return ReturnCode.OK;
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

            Byte temp = Memory.Add<Byte>(codeWriter, " copyData ");

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
                throw new Exception("MoveData dont have the same size");

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

        private ReturnCode compileLine(string[] args, bool needReset)
        {
            switch (args[0])
            {
                case "include":
                    {
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Compile(Path, Path + args[1], "", this);
                        break;
                    }
                case "print":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        foreach (string arg in args.Skip(1))
                        {
                            Data v = Memory[arg];
                            for (short i = v.Address; i < v.Address + v.Size; i++)
                            {
                                Move(CodeWriter, i);
                                CodeWriter.Write(".", "print");
                            }
                        }
                        break;
                    }
                case "input":
                    {
                        if (CodeWriter is null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        if (args.Length == 2)
                        {
                            Char v = Memory.Add<Char>(CodeWriter, args[1]);
                            Move(CodeWriter, v.Address);
                            CodeWriter.Write(",", "input");
                        }
                        else if (short.TryParse(args[2], out short amount))
                        {
                            String s = Memory.Add<String>(CodeWriter, args[1], amount, String.ConstructorOf(amount));
                            Move(CodeWriter, s.Address);
                            CodeWriter.Write(new string(',', amount), "input");
                        }
                        else if (ValueType.Types.ContainsKey(args[2]))
                        {
                            var t = ValueType.Types[args[2]];
                            Data d = Memory.Add(CodeWriter, args[1], t.size, t.constructor);
                            Move(CodeWriter, d.Address);
                            CodeWriter.Write(new string(',', t.size), "input");
                        }
                        else
                        {
                            return ReturnCode.BadArgs;
                        }
                        break;
                    }
                case "while":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        short address = Memory[args[1]].Address;
                        Move(CodeWriter, address);
                        CodeWriter.Write("[", $"check {address}");
                        Memory.PushStack();
                        ReturnCode r = Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)2, ' ')), this, true);
                        Memory.PopStack(CodeWriter, true);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        Move(CodeWriter, address);
                        CodeWriter.Write("]", $"end of {address}");
                        break;
                    }
                case "foreach":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 4 || !Memory.ContainName(args[1]))
                            return ReturnCode.BadArgs;
                        if (Memory[args[1]] is not Array arr)
                            return ReturnCode.BadArgs;

                        string[] commands = getFileCommands(new string(args[3].Skip(1).ToArray()).Replace((char)2, ' '));

                        for (short i = 0; i < arr.Amount; i++)
                        {
                            Data element = arr.Get(i);
                            Memory.PushStack();
                            Memory.AddToCurrent(args[2], element, true);
                            ReturnCode r = Compile(commands, this, true);
                            Memory.PopStack(CodeWriter, true);
                            if (r != ReturnCode.OK)
                                return r;
                        }
                        break;
                    }
                case "if":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        short address = Memory[args[1]].Address;
                        Move(CodeWriter, address);
                        CodeWriter.Write("[", $"check {address}");
                        Memory.PushStack();
                        ReturnCode r = Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)2, ' ')), this, needReset);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        Byte v = Memory.Add<Byte>(CodeWriter, " if ");
                        Move(CodeWriter, v.Address);
                        Memory.PopStack(CodeWriter, needReset);
                        CodeWriter.Write("]", $"end of {address}");
                        break;
                    }
                case "struct":
                    {
                        if (args.Length < 4 || args.Length % 2 != 0
                            || Data.Types.ContainsKey(args[1]))
                        {
                            return ReturnCode.BadArgs;
                        }

                        short size = 0;// -1 -> not ValueType
                        Dictionary<string, Func<short, Data>> datas = new();
                        for (int i = 2; i < args.Length; i += 2)
                        {
                            if (datas.ContainsKey(args[i]))
                                return ReturnCode.BadArgs;

                            Func<short, Data> constructor;
                            if (ValueType.Types.ContainsKey(args[i]))
                            {
                                var type = ValueType.Types[args[i]];
                                if (size != -1)
                                    size += type.size;
                                constructor = type.constructor;
                            }
                            else if (Data.Types.ContainsKey(args[i]))
                            {
                                size = -1;
                                throw new NotImplementedException();
                            }
                            else
                            {
                                return ReturnCode.BadArgs;
                            }

                            datas.Add(args[i + 1], constructor);
                        }
                        Data.Types.Add(args[1], Data.StructInit);

                        if (size != -1)
                        {
                            ValueType.Types.Add(args[1], (size, (address) =>
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
                        if (args.Length < 3 || args.Length % 2 == 0)
                        {
                            return ReturnCode.BadArgs;
                        }
                        string[] to = new string[(args.Length - 3) / 2];
                        for (int i = 3; i < args.Length - 1; i += 2)
                        {
                            to[(i - 3) / 2] = args[i];
                        }
                        BFFunctions.Add(args[1], new BFFunction(args.Length / 2 - 1,
                            (Compiler comp, CodeWriter codeWriter, string[] args2, bool needReset) =>
                        {
                            comp.Memory.PushFunc(args2, to);
                            ReturnCode r = Compile(getFileCommands(new string(args[args.Length - 1].Skip(1).ToArray()).Replace((char)2, ' ')), this, needReset);
                            comp.Memory.PopFunc(codeWriter, needReset);
                            return r;
                        }));
                        break;
                    }
                case "call":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 2 || !BFFunctions.ContainsKey(args[1]))
                        {
                            return ReturnCode.BadArgs;
                        }
                        BFFunction func = BFFunctions[args[1]];
                        if (args.Length < 2 + func.NumberArgs)
                        {
                            return ReturnCode.BadArgs;
                        }
                        ReturnCode r = func.Action(this, CodeWriter, args.Skip(2).ToArray(), needReset);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        break;
                    }
                default:
                    if (Data.Types.ContainsKey(args[0]))
                        return Data.Types[args[0]].Invoke(this, args, needReset);
                    if (Memory.ContainName(args[1]))
                    {
                        Data d = Memory[args[1]];
                        if (d.BuildInFunction.ContainsKey(args[0]))
                        {
                            return d.BuildInFunction[args[0]].Invoke(d, this, args, needReset);
                        }
                    }
                    return ReturnCode.BadCommand;
            }
            return ReturnCode.OK;
        }
    }
}