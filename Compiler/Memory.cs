using System.Linq;

namespace Compiler
{
    public class Memory
    {
        private Stack<Dictionary<string, short>> memory = new();

        private Stack<List<string>> name = new();

        private Dictionary<string, short> current => memory.Peek();

        //Already got used but don't know if it is at zero
        private List<short> garbages = new();
        //Already got used but is at zero
        private List<short> unUsed = new();

        private Compiler Compiler;

        public Memory(Compiler comp)
        {
            memory.Push(new());
            Compiler = comp;
        }

        public bool ContainName(string name) => current.ContainsKey(name);

        public void Add(CodeWriter codeWriter, string name)
        {
            if (nextMemory == 29999)
            {
                if (unUsed.Any())
                {
                    current.Add(name, unUsed.First());
                    unUsed.RemoveAt(0);
                    this.name.Peek().Add(name);
                }
                else if (garbages.Any())
                {
                    short address = garbages.First();
                    current.Add(name, address);
                    garbages.RemoveAt(0);
                    this.name.Peek().Add(name);
                    Compiler.Move(codeWriter, address);
                    codeWriter.Write("[-]", "set to 0");
                    while (unUsed.Contains((short)(nextMemory - 1)))
                    {
                        unUsed.Remove(--nextMemory);
                    }
                }
                else
                    throw new Exception("BrainFuck out of memory");
            }
            else
            {
                current.Add(name, nextMemory);
                this.name.Peek().Add(name);
                nextMemory++;
            }
        }

        void remove(CodeWriter codeWriter, bool garbage, string name)
        {
            short address = current[name];
            if (garbage)
            {
                Compiler.Move(codeWriter, address);
                codeWriter.Write("[-]", "set to 0");
                if (address == nextMemory - 1)
                {
                    nextMemory--;
                    while (unUsed.Contains((short)(nextMemory - 1)))
                    {
                        unUsed.Remove(--nextMemory);
                    }
                }
                else
                {
                    unUsed.Add(address);
                }
            }
            else
            {
                garbages.Add(address);
            }
            current.Remove(name);
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

        public void PopFunc(CodeWriter codeWriter, bool garbage)
        {
            PopStack(codeWriter, garbage);
            memory.Pop();
        }

        public void PushStack() => name.Push(new());
        public void PopStack(CodeWriter codeWriter, bool garbage)
        {
            List<string>? names;
            if (name.TryPop(out names))
            {
                foreach (string name in names)
                {
                    remove(codeWriter, garbage, name);
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
