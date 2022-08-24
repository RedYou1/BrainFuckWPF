﻿using Compiler.BFFunctions;

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
                returnCode = Compile(getFileCommands(File.ReadAllText(sourcePath)), new Compiler(sw));
                sw.Close();
            }

            return returnCode;
        }

        public Dictionary<string, BFFunction> BFFunctions { get; } = new()
        {
            { "print", new BFFPrint() }
        };

        public Compiler(StreamWriter streamWriter)
        {
            this.StreamWriter = streamWriter;
        }


        public StreamWriter StreamWriter { get; }
        public Dictionary<string, short> Memory { get; } = new();
        public short nextMemory { get; protected set; } = 0;
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
                    stack.Push(currentChar);
                    currentValue += " ;";
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
                    !((currentChar == ' ' || currentChar == '\t') && stacktop == '{' && currentValue.Last() == ';'))
                {
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

        private static ReturnCode Compile(string[] commands, Compiler comp)
        {
            foreach (string line in commands)
            {
                string[] args = line.Split(' ');
                ReturnCode status = comp.compileLine(args);
                if (status != ReturnCode.OK)
                {
                    return status;
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

        private ReturnCode compileLine(string[] args)
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
                        Memory.Add(args[1], nextMemory);
                        nextMemory++;
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
                                byte.Parse(args[2])));
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
                                byte.Parse(args[2])));
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
                case "while":
                    {
                        if (args.Length < 3)
                        {
                            return ReturnCode.BadArgs;
                        }
                        short address = Memory[args[1]];
                        Move(address);
                        StreamWriter.Write('[');
                        ReturnCode r = Compile(getFileCommands(new string(args[2].Skip(1).ToArray()).Replace((char)1, ' ')), this);
                        if (r != ReturnCode.OK)
                        {
                            return r;
                        }
                        Move(address);
                        StreamWriter.Write(']');
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
                        func.Call(this, args.Skip(2).ToArray());
                        break;
                    }
                default:
                    return ReturnCode.BadCommand;
            }
            return ReturnCode.OK;
        }
    }
}