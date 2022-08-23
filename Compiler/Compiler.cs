using Compiler.BFFunctions;

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
                returnCode = Compile(sourcePath, new Compiler(sw));
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

        private static ReturnCode Compile(string sourcePath, Compiler comp)
        {
            foreach (string line in File.ReadAllLines(sourcePath))
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
                case "char":
                case "byte":
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