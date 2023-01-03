namespace Compiler
{
    public class CodeWriter
    {
        private StreamWriter _writer;
        public readonly Compiler Compiler;
        public short actualPtr { get; protected set; } = 0;

        public readonly Memory Memory;

        public CodeWriter(StreamWriter writer, Compiler compiler)
        {
            _writer = writer;
            Compiler = compiler;
            Memory = new(this);
            Memory.PushStack();
        }

        public void While(short address, Action action)
        {
            Move(address);
            Write("[", $"check {address}");
            Memory!.PushStack();

            action.Invoke();

            Memory!.PopStack(true);
            Move(address);
            Write("]", $"end of {address}");
        }

        public void IfKeep(short address, Action action, string description1, string description2, bool needReset)
        {
            Memory.PushStack();
            short temp = Memory.Add<Byte>(" tempIf ").Address;

            Move(address);
            Write("[", description1);
            action.Invoke();

            MoveData(address, temp, false);

            Move(address);
            Write("]", description2);

            MoveData(temp, address, false);

            Memory.PopStack(needReset);
        }

        public void IfKeep(short address, Action action, string description1, string description2, short temp)
        {
            Move(address);
            Write("[", description1);
            action.Invoke();

            MoveData(address, temp, false);

            Move(address);
            Write("]", description2);

            MoveData(temp, address, false);
        }

        public void IfToZero(short address, Action action, string description1, string description2)
        {
            Move(address);
            Write("[", description1);

            action.Invoke();

            Move(address);
            Write("[-]]", description2);
        }

        public void Set(short address, int amount, string description)
        {
            Move(address);
            Write("[-]" + new string(amount > 0 ? '+' : '-', Math.Abs(amount) % (byte.MaxValue + 1)), description);
        }

        public void Add(short address, int amount, string description)
        {
            if (amount == 0)
                return;
            Move(address);
            Write(new string(amount > 0 ? '+' : '-', Math.Abs(amount) % (byte.MaxValue + 1)), description);
        }

        public void Write(string command, string description)
        {
            if (Compiler.Debug)
                _writer.WriteLine(command + " //" + description);
            else
                _writer.Write(command);
        }

        public void Move(short moveTo)
        {
            if (moveTo != actualPtr)
            {
                Write(
                    new string(moveTo > actualPtr ? '>' : '<',
                    Math.Abs(moveTo - actualPtr)), $"move to {moveTo}");
                actualPtr = moveTo;
            }
        }

        public void CopyData(Data from, Data to, bool set2ToZero, bool needReset)
        {
            if (from.Address == to.Address)
                return;
            if (from.Size != to.Size)
                throw new Exception("CopyData dont have the same size");

            for (int i = 0; i < from.Size; i++)
            {
                CopyData(from.AddressArray[i], to.AddressArray[i], set2ToZero, needReset);
            }
        }

        public void CopyData(short from, short to, bool set2ToZero, bool needReset)
        {
            if (from == to)
                return;
            if (set2ToZero)
            {
                if (to != actualPtr)
                    Move(to);
                Write("[-]", "reset to 0");
            }
            if (from != actualPtr)
                Move(from);

            Memory.PushStack();

            Byte temp = Memory.Add<Byte>(" copyData ");

            short moveAmountA = (short)Math.Abs(to - from);
            bool dirA = to > from;
            short moveAmountB = (short)Math.Abs(temp.Address - to);
            bool dirB = temp.Address > to;
            short moveAmountC = (short)Math.Abs(from - temp.Address);
            bool dirC = from > temp.Address;
            Write(
                $"""
                 [-{new string(dirA ? '>' : '<', moveAmountA)}
                  +{new string(dirB ? '>' : '<', moveAmountB)}
                  +{new string(dirC ? '>' : '<', moveAmountC)}]
                 """, $"Duplicate data from {from} to {from} and {temp.Address}");
            MoveData(temp.Address, from, false);

            Memory.PopStack(needReset);
        }

        public void MoveData(Data from, Data to, bool set2ToZero)
        {
            if (from.Address == to.Address)
                return;
            if (from.Size != to.Size)
                throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "MoveData dont have the same size");

            for (int i = 0; i < from.Size; i++)
            {
                MoveData(from.AddressArray[i], to.AddressArray[i], set2ToZero);
            }
        }

        public void MoveData(short from, short to, bool set2ToZero)
        {
            if (from == to)
                return;
            if (set2ToZero)
            {
                if (to != actualPtr)
                    Move(to);
                Write("[-]", "reset to 0");
            }
            if (from != actualPtr)
                Move(from);
            short moveAmount = (short)Math.Abs(to - from);
            bool dir = to > from;
            Write($"[-{new string(dir ? '>' : '<', moveAmount)}+{new string(dir ? '<' : '>', moveAmount)}]", $"Add between {from} and {to}");
        }
    }
}
