using Byte = Compiler.ValueTypes.Byte;
using System.IO;
using System.Text.RegularExpressions;

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

        private static ReturnCode Compile(string[] commands, Compiler comp, bool garbage)
        {
            foreach (string line in commands)
            {
                string[] args = line.Split((char)1);
                ReturnCode status = comp.compileLine(args, garbage);
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

        public byte GetValue(string value)
        {
            byte result = 0;
            if (byte.TryParse(value, out result))
            {
                return result;
            }
            else if (Regex.Match(value, @"^'\\{0,1}.'$").Success)
            {
                value = value.Substring(1, value.Length - 2);
                if (value.Length == 1)
                {
                    return (byte)value[0];
                }
                else
                {
                    switch (value[1])
                    {
                        case 'a':
                            return (byte)'\a';
                        case 'b':
                            return (byte)'\b';
                        case 'f':
                            return (byte)'\f';
                        case 'n':
                            return (byte)'\n';
                        case 'r':
                            return (byte)'\r';
                        case 't':
                            return (byte)'\t';
                        case 'v':
                            return (byte)'\v';
                        default:
                            return (byte)value[1];
                    }
                }
            }
            throw new ArgumentException();
        }

        private ReturnCode compileLine(string[] args, bool garbage)
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
                case "bool":
                case "byte":
                case "char":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Byte v = Memory.Add<Byte>(CodeWriter, args[1]);
                        if (args.Length >= 3)
                        {
                            Move(CodeWriter, v.Address);
                            byte value = GetValue(args[2]);
                            CodeWriter.Write(
                                new string('+',
                                    value), $"adding {value}");
                        }
                        break;
                    }
                case "add":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Memory[args[1]].Add(this, CodeWriter, args[2]);
                        break;
                    }
                case "sub":
                    {
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Memory[args[1]].Sub(this, CodeWriter, args[2]);
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
                            ValueType v = Memory[arg];
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
                        if (CodeWriter == null)
                            return ReturnCode.WrongStart;
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Byte v = Memory.Add<Byte>(CodeWriter, args[1]);
                        Move(CodeWriter, v.Address);
                        CodeWriter.Write(",", "input");
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
                        ReturnCode r = Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)2, ' ')), this, garbage);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        Byte v = Memory.Add<Byte>(CodeWriter, " if ");
                        Move(CodeWriter, v.Address);
                        Memory.PopStack(CodeWriter, garbage);
                        CodeWriter.Write("]", $"end of {address}");
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
                            (Compiler comp, CodeWriter codeWriter, string[] args2, bool garbage) =>
                        {
                            comp.Memory.PushFunc(args2, to);
                            ReturnCode r = Compile(getFileCommands(new string(args[args.Length - 1].Skip(1).ToArray()).Replace((char)2, ' ')), this, garbage);
                            comp.Memory.PopFunc(codeWriter, garbage);
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
                        ReturnCode r = func.Action(this, CodeWriter, args.Skip(2).ToArray(), garbage);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        break;
                    }
                default:
                    return ReturnCode.BadCommand;
            }
            return ReturnCode.OK;
        }
    }
}