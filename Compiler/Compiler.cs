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
                    comp.CodeWriter = new(sw, comp);
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
                CodeWriter = new(streamWriter, this);
        }

        public void IsMainFile()
        {
            CompileError.NotNull(CodeWriter, "Tried to do an action without being in the starting file .f");
        }

        public string Path { get; }

        public CodeWriter? CodeWriter { get; private set; }
        public Memory? Memory => CodeWriter?.Memory;

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
                    IsMainFile();
                    CodeWriter!.Write("", string.Join(' ', args.Skip(1).ToArray()));
                    break;
                case "input":
                    {
                        IsMainFile();
                        CompileError.MinLength(args.Length, 2, "input args: {name} {type or length default Char}");

                        if (args.Length == 2)
                        {
                            if (Memory!.ContainName(args[1]))
                            {
                                Data d = Memory![args[1]];
                                CodeWriter!.Move(d.Address);
                                CodeWriter!.Write(new string(',', d.Size), $"input for variable {args[1]}");
                            }
                            else
                            {
                                Char v = Memory!.Add<Char>(args[1]);
                                CodeWriter!.Move(v.Address);
                                CodeWriter!.Write(",", "input");
                            }
                        }
                        else if (short.TryParse(args[2], out short amount))
                        {
                            String s = Memory!.Add<String>(args[1], amount, String.ConstructorOf(amount));
                            CodeWriter!.Move(s.Address);
                            CodeWriter!.Write(new string(',', amount), "input");
                        }
                        else if (ValueTypes.ContainsKey(args[2]))
                        {
                            var t = ValueTypes[args[2]];
                            Data d = Memory!.Add(args[1], t.size, t.constructor);
                            CodeWriter!.Move(d.Address);
                            CodeWriter!.Write(new string(',', t.size), "input");
                        }
                        else
                        {
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "input args: {name} {type or length default Char}");
                        }
                        break;
                    }
                case "unsafe":
                    {
                        IsMainFile();
                        CompileError.MinLength(args.Length, 3, "structure: unsafe {name or ptr}\n{\n}");

                        short address;
                        if (Memory!.ContainName(args[1]))
                            address = Memory![args[1]].Address;
                        else if (!short.TryParse(args[1], out address))
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "structure: unsafe {name or ptr}\n{\n}");

                        CodeWriter!.Move(address);
                        CodeWriter!.Write(string.Join("", args[2].Skip(1)),
                            $"unsafe {address}");
                        break;
                    }
                case "while":
                    {
                        IsMainFile();
                        CompileError.MinLength(args.Length, 3, "structure: while {name}\n{\n}");

                        CodeWriter!.While(Memory![args[1]].Address, () =>
                        {
                            try
                            {
                                Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)2, ' ')), this, true);
                            }
                            catch (CompileError e)
                            {
                                e.AddMessage($"while {args[1]}");
                                throw e;
                            }
                        });
                        break;
                    }
                case "foreach":
                    {
                        IsMainFile();
                        CompileError.MinLength(args.Length, 4, "structure: foreach {arrayName} {elementName}\n{\n}");
                        if (!Memory!.ContainName(args[1]) || Memory![args[1]] is not Array arr)
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"foreach arrayName ({args[1]}) doesn't exists or isn't an array");

                        string[] commands = getFileCommands(new string(args[3].Skip(1).ToArray()).Replace((char)2, ' '));

                        for (short i = 0; i < arr.Amount; i++)
                        {
                            Data element = arr.Get(i);
                            Memory!.PushStack();
                            Memory!.AddToCurrent(args[2], element, true);
                            try
                            {
                                Compile(commands, this, true);
                            }
                            catch (CompileError e)
                            {
                                e.AddMessage($"foreach {args[1]} {args[2]}");
                                throw e;
                            }
                            Memory!.PopStack(i + 1 == arr.Amount ? needReset : true);
                        }
                        break;
                    }
                case "if":
                    {
                        IsMainFile();
                        CompileError.MinLength(args.Length, 3, "structure: if {name}\n{\n}");

                        CodeWriter!.IfKeep(Memory![args[1]].Address, () =>
                        {
                            try
                            {
                                Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)2, ' ')), this, needReset);
                            }
                            catch (CompileError e)
                            {
                                e.AddMessage($"if {args[1]}");
                                throw e;
                            }
                        }, $"check {args[1]}", $"end of {args[1]}", needReset);
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
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "structure: func {name} ({type} {name})\n{\n}");

                        string[] to = new string[(args.Length - 3) / 2];
                        for (int i = 3; i < args.Length - 1; i += 2)
                        {
                            to[(i - 3) / 2] = args[i];
                        }
                        BFFunctions.Add(args[1], new BFFunction(args.Length / 2 - 1,
                            (CodeWriter codeWriter, string[] args2, bool needReset) =>
                        {
                            codeWriter.Memory.PushFunc(args2, to);
                            try
                            {
                                Compile(getFileCommands(new string(args[args.Length - 1].Skip(1).ToArray()).Replace((char)2, ' ')), this, needReset);
                            }
                            catch (CompileError e)
                            {
                                e.AddMessage($"func {args[1]}");
                                throw e;
                            }
                            codeWriter.Memory.PopFunc(needReset);
                        }));
                        break;
                    }
                case "call":
                    {
                        IsMainFile();
                        CompileError.MinLength(args.Length, 2, "structure: call {name} {args}");
                        if (!BFFunctions.ContainsKey(args[1]))
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"function {args[1]} doesn't exists.");

                        BFFunction func = BFFunctions[args[1]];
                        if (args.Length < 2 + func.NumberArgs)
                            throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, $"bad amount of args for the function {args[1]}.");

                        try
                        {
                            func.Action(CodeWriter!, args.Skip(2).ToArray(), needReset);
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
                    if (CodeWriter is not null && Memory!.ContainName(args[1]))
                    {
                        Data d = Memory![args[1]];
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