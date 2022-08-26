namespace Compiler
{
    public class Memory
    {
        private Stack<Dictionary<string, short>> memory = new();

        private Stack<List<string>> name = new();

        private Dictionary<string, short> current => memory.Peek();

        public Memory()
        {
            memory.Push(new());
        }

        public bool ContainName(string name) => current.ContainsKey(name);

        public void Add(string name)
        {
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
        }

        public void PopFunc()
        {
            memory.Pop();
        }

        public void PushStack() => name.Push(new());
        public void PopStack()
        {
            List<string>? names;
            if (name.TryPop(out names))
            {
                foreach (string name in names)
                {
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
