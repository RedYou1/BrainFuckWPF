namespace Compiler
{
    public class Memory
    {
        private Stack<Dictionary<string, short>> memory = new();

        private Stack<List<string>> name = new();

        private Dictionary<string, short> current => memory.Peek();

        private Compiler Compiler;

        public Memory(Compiler comp)
        {
            memory.Push(new());
            Compiler = comp;
        }

        public bool ContainName(string name) => current.ContainsKey(name);

        public void Add(string name, Compiler comp)
        {
            if (nextMemory == 29999)
            {
                throw new Exception("BrainFuck out of memory");
            }
            current.Add(name, nextMemory);
            this.name.Peek().Add(name);
            nextMemory++;
        }

        public void PushFunc(string[] from, string[] to)
        {
            int size = from.Length;
            if (size != to.Length)
                throw new ArgumentException("not same size");
            Dictionary<string, short> current = this.current;
            Dictionary<string, short> dict = new();
            for (int i = 0; i < size; i++)
            {
                dict.Add(to[i], current[from[i]]);
            }
            memory.Push(dict);
            PushStack();
        }

        public void PopFunc(bool garbage)
        {
            PopStack(garbage);
            memory.Pop();
        }

        public void PushStack() => name.Push(new());
        public void PopStack(bool garbage)
        {
            List<string>? names;
            if (name.TryPop(out names))
            {
                foreach (string name in names)
                {
                    if (garbage)
                    {
                        Compiler.Move(current[name]);
                        Compiler.StreamWriter.Write("[-]");
                    }
                    current.Remove(name);
                }
            }
            else
            {
                throw new Exception("no stack");
            }
        }

        public short this[string name] => current[name];
        protected short nextMemory = 0;
    }
}
