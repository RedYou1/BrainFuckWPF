namespace Compiler
{
    public class Compiler
    {
        public enum ReturnCode
        {
            OK,
            SourceDontExists,
            ResultAlreadyExists,
            BadCommand,
            BadArgs
        }
        public static ReturnCode Compile(string sourcePath, string resultPath)
        {
            if (!File.Exists(sourcePath))
            {
                return ReturnCode.SourceDontExists;
            }

            ReturnCode returnCode = ReturnCode.OK;
            using (StreamWriter sw = File.CreateText(resultPath))
            {
                returnCode = Compile(getFileCommands(File.ReadAllText(sourcePath)), new Compiler(sw), false);
                sw.Close();
            }

            return returnCode;
        }

        public Dictionary<string, BFFunction> BFFunctions { get; } = new();

        public Compiler(StreamWriter streamWriter)
        {
            StreamWriter = streamWriter;
            Memory = new(this);
            Memory.PushStack();
        }

        public StreamWriter StreamWriter { get; }
        public Memory Memory { get; }
        public short actualPtr { get; protected set; } = 0;

        public bool MultipleLine = true;
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
                    while (currentValue.Last() == ' ')
                    {
                        currentValue = currentValue.Substring(0, currentValue.Length - 1);
                    }
                    if (stack.Count == 0)
                    {
                        currentValue += " ;";
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
                    if (currentChar == ' ' && currentValue.Last() == ' ')
                    {
                        continue;
                    }
                    if (stacktop == '{' && currentChar == ' ')
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
                string[] args = line.Split(' ');
                ReturnCode status = comp.compileLine(args, garbage);
                if (status != ReturnCode.OK)
                {
                    return status;
                }
                if (comp.MultipleLine)
                {
                    comp.StreamWriter.Write('\n');
                }
            }
            return ReturnCode.OK;
        }

        public void Move(short moveTo)
        {
            if (moveTo != actualPtr)
            {
                StreamWriter.Write(
                    new string(moveTo > actualPtr ? '>' : '<',
                    Math.Abs(moveTo - actualPtr)));
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
            else if (value.Length == 1)
            {
                return (byte)value[0];
            }
            throw new ArgumentException();
        }

        private ReturnCode compileLine(string[] args, bool garbage)
        {
            switch (args[0])
            {
                case "bool":
                case "byte":
                case "char":
                    {
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Memory.Add(args[1], this);
                        if (args.Length >= 3)
                        {
                            Move(Memory[args[1]]);
                            StreamWriter.Write(
                                new string('+',
                                    GetValue(args[2])));
                        }
                        break;
                    }
                case "add":
                    {
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Move(Memory[args[1]]);
                        StreamWriter.Write(
                            new string('+',
                                GetValue(args[2])));
                        break;
                    }
                case "sub":
                    {
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Move(Memory[args[1]]);
                        StreamWriter.Write(
                            new string('-',
                                GetValue(args[2])));
                        break;
                    }
                case "print":
                    {
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        foreach (string arg in args.Skip(1))
                        {
                            Move(Memory[arg]);
                            StreamWriter.Write('.');
                        }
                        break;
                    }
                case "input":
                    {
                        if (args.Length < 2)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Memory.Add(args[1], this);
                        Move(Memory[args[1]]);
                        StreamWriter.Write(',');
                        break;
                    }
                case "while":
                    {
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        short address = Memory[args[1]];
                        Move(address);
                        StreamWriter.Write('[');
                        Memory.PushStack();
                        ReturnCode r = Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)1, ' ')), this, true);
                        Memory.PopStack(true);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        Move(address);
                        StreamWriter.Write(']');
                        break;
                    }
                case "if":
                    {
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        Move(Memory[args[1]]);
                        StreamWriter.Write('[');
                        Memory.PushStack();
                        ReturnCode r = Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)1, ' ')), this, garbage);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        Memory.Add(" if ", this);
                        Move(Memory[" if "]);
                        Memory.PopStack(garbage);
                        StreamWriter.Write(']');
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
                            (Compiler comp, string[] args2, bool garbage) =>
                        {
                            comp.Memory.PushFunc(args2, to);
                            ReturnCode r = Compile(getFileCommands(new string(args[args.Length - 1].Skip(1).ToArray()).Replace((char)1, ' ')), this, garbage);
                            comp.Memory.PopFunc(garbage);
                            return r;
                        }));
                        break;
                    }
                case "call":
                    {
                        if (args.Length < 2 || !BFFunctions.ContainsKey(args[1]))
                        {
                            return ReturnCode.BadArgs;
                        }
                        BFFunction func = BFFunctions[args[1]];
                        if (args.Length < 2 + func.NumberArgs)
                        {
                            return ReturnCode.BadArgs;
                        }
                        ReturnCode r = func.Action(this, args.Skip(2).ToArray(), garbage);
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