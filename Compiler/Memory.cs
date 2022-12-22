using System.Drawing;
using System.Linq;
using System.Net;

namespace Compiler
{
    public class Memory
    {
        private Stack<Dictionary<string, ValueType>> memory = new();

        private Stack<List<string>> name = new();

        private Dictionary<string, ValueType> current => memory.Peek();

        //Already got used but don't know if it is at zero
        private List<ValueType> garbages = new();
        //Already got used but is at zero
        private List<ValueType> unUsed = new();

        private Compiler Compiler;

        public Memory(Compiler comp)
        {
            memory.Push(new());
            Compiler = comp;
        }

        public bool ContainName(string name) => current.ContainsKey(name);

        public ValueType Add(CodeWriter codeWriter, string name, short size)
        {
            if (nextMemory == 29999)
            {
                if (unUsed.Any(v => v.Size >= size))
                {
                    ValueType v = unUsed.First(v => v.Size >= size);
                    unUsed.Remove(v);
                    if (v.Size > size)
                    {
                        unUsed.Add(new ValueType { Address = (short)(v.Address + size), Size = (short)(v.Size - size) });
                    }
                    v = new ValueType { Address = v.Address, Size = size };
                    current.Add(name, v);
                    this.name.Peek().Add(name);
                    return v;
                }
                else if (garbages.Any(v => v.Size >= size))
                {
                    ValueType v = garbages.First(v => v.Size >= size);
                    garbages.Remove(v);
                    if (v.Size > size)
                    {
                        garbages.Add(new ValueType { Address = (short)(v.Address + size), Size = (short)(v.Size - size) });
                    }
                    v = new ValueType { Address = v.Address, Size = size };
                    current.Add(name, v);
                    this.name.Peek().Add(name);
                    for (short i = v.Address; i < v.Address + size; i++)
                    {
                        Compiler.Move(codeWriter, i);
                        codeWriter.Write("[-]", "set to 0");
                    }
                    while (unUsed.Any(v => v.Address + v.Size == nextMemory))
                    {
                        ValueType v2 = unUsed.First(v => v.Address + v.Size == nextMemory);
                        unUsed.Remove(v2);
                        nextMemory = v2.Address;
                    }
                    return v;
                }
                else
                    throw new Exception("BrainFuck out of memory");
            }
            else
            {
                ValueType v = new ValueType { Address = nextMemory, Size = size };
                current.Add(name, v);
                this.name.Peek().Add(name);
                nextMemory += size;
                return v;
            }
        }

        void remove(CodeWriter codeWriter, bool garbage, string name)
        {
            ValueType v = current[name];
            if (garbage)
            {
                for (short i = v.Address; i < v.Address + v.Size; i++)
                {
                    Compiler.Move(codeWriter, i);
                    codeWriter.Write("[-]", "set to 0");
                }
                if (v.Address + v.Size == nextMemory)
                {
                    nextMemory = v.Address;
                    while (unUsed.Any(v => v.Address + v.Size == nextMemory))
                    {
                        ValueType v2 = unUsed.First(v => v.Address + v.Size == nextMemory);
                        unUsed.Remove(v2);
                        nextMemory = v2.Address;
                    }
                }
                else
                {
                    unUsed.Add(v);
                }
            }
            else
            {
                garbages.Add(v);
            }
            current.Remove(name);
        }

        public void PushFunc(string[] from, string[] to)
        {
            int size = from.Length;
            if (size != to.Length)
                throw new ArgumentException("not same size");
            Dictionary<string, ValueType> current = this.current;
            Dictionary<string, ValueType> dict = new();
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

        public ValueType this[string name] => current[name];
        protected short nextMemory = 0;
    }
}
