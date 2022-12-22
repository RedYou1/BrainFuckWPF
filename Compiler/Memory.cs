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

        public T Add<T>(CodeWriter codeWriter, string name)
            where T : ValueType
        {
            var type = ValueType.Types[typeof(T).Name];
            if (nextMemory == 29999)
            {
                if (unUsed.Any(v => v.Size >= type.size))
                {
                    ValueType v = unUsed.First(v => v.Size >= type.size);
                    unUsed.Remove(v);
                    if (v.Size > type.size)
                    {
                        unUsed.Add(new ValueType((short)(v.Address + type.size), (short)(v.Size - type.size)));
                    }
                    T r = (T)type.constructor(v.Address);
                    current.Add(name, r);
                    this.name.Peek().Add(name);
                    return r;
                }
                else if (garbages.Any(v => v.Size >= type.size))
                {
                    ValueType v = garbages.First(v => v.Size >= type.size);
                    garbages.Remove(v);
                    if (v.Size > type.size)
                    {
                        garbages.Add(new ValueType((short)(v.Address + type.size), (short)(v.Size - type.size)));
                    }
                    T r = (T)type.constructor(v.Address);
                    current.Add(name, r);
                    this.name.Peek().Add(name);
                    for (short i = r.Address; i < r.Address + type.size; i++)
                    {
                        Compiler.Move(codeWriter, i);
                        codeWriter.Write("[-]", "set to 0");
                    }
                    while (unUsed.Any(v => r.Address + r.Size == nextMemory))
                    {
                        ValueType v2 = unUsed.First(v => r.Address + r.Size == nextMemory);
                        unUsed.Remove(v2);
                        nextMemory = v2.Address;
                    }
                    return r;
                }
                else
                    throw new Exception("BrainFuck out of memory");
            }
            else
            {
                T r = (T)type.constructor(nextMemory);
                current.Add(name, r);
                this.name.Peek().Add(name);
                nextMemory += type.size;
                return r;
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
