using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;

namespace Compiler
{
    public class Memory
    {
        private Stack<Dictionary<string, Data>> memory = new();

        private Stack<List<(string name, bool reference)>> name = new();

        private Dictionary<string, Data> current => memory.Peek();

        //Already got used but don't know if it is at zero
        private List<Data> garbages = new();
        //Already got used but is at zero
        private List<Data> unUsed = new();

        private CodeWriter CodeWriter;

        public Memory(CodeWriter codeWriter)
        {
            memory.Push(new());
            CodeWriter = codeWriter;
        }

        public bool ContainName(string name)
        {
            string[] names = name.Split('.');
            if (!current.ContainsKey(names[0]))
                return false;
            Data output = current[names[0]];
            if (names.Length > 1)
            {
                for (int i = 1; i < names.Length; i++)
                {
                    if (output is not Container container)
                        throw new CompileError(CompileError.ReturnCodeEnum.BadArgs, "Memory name dont exists");
                    if (!container.ContainsKey(names[i]))
                        return false;
                    output = container[names[i], this];
                }
            }
            return true;
        }

        public T Add<T>(string name)
            where T : ValueType
        {
            var type = CodeWriter.Compiler.ValueTypes[typeof(T).Name];
            return (T)Add(name, type.size, type.constructor);
        }

        public T Add<T>(string name, short size, Func<short, Data> constructor)
            where T : Data
            => (T)Add(name, size, constructor);

        public Data Add(string name, short size, Func<short, Data> constructor)
        {
            if (nextMemory == 29999)
            {
                if (unUsed.Any(v => v.Size >= size))
                {
                    Data v = unUsed.First(v => v.Size >= size);
                    unUsed.Remove(v);
                    if (v.Size > size)
                    {
                        unUsed.Add(new Data((short)(v.Address + size), (short)(v.Size - size)));
                    }
                    v = constructor(v.Address);
                    AddToCurrent(name, v);
                    return v;
                }
                else if (garbages.Any(v => v.Size >= size))
                {
                    Data v = garbages.First(v => v.Size >= size);
                    garbages.Remove(v);
                    if (v.Size > size)
                    {
                        garbages.Add(new Data((short)(v.Address + size), (short)(v.Size - size)));
                    }
                    v = constructor(v.Address);
                    AddToCurrent(name, v);
                    for (short i = v.Address; i < v.Address + size; i++)
                    {
                        CodeWriter.Move(i);
                        CodeWriter.Write("[-]", "set to 0");
                    }
                    while (unUsed.Any(v => v.Address + v.Size == nextMemory))
                    {
                        Data v2 = unUsed.First(v => v.Address + v.Size == nextMemory);
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
                Data v = constructor(nextMemory);
                AddToCurrent(name, v);
                nextMemory += size;
                return v;
            }
        }

        void remove(bool needReset, string name)
        {
            Data v = current[name];
            if (needReset)
            {
                for (short i = v.Address; i < v.Address + v.Size; i++)
                {
                    CodeWriter.Move(i);
                    CodeWriter.Write("[-]", "set to 0");
                }
                if (v.Address + v.Size == nextMemory)
                {
                    nextMemory = v.Address;
                    while (unUsed.Any(v => v.Address + v.Size == nextMemory))
                    {
                        Data v2 = unUsed.First(v => v.Address + v.Size == nextMemory);
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
            Dictionary<string, Data> current = this.current;
            Dictionary<string, Data> dict = new();
            for (int i = 0; i < size; i++)
            {
                dict.Add(to[i], current[from[i]]);
            }
            memory.Push(dict);
            PushStack();
        }

        public void PopFunc(bool needReset)
        {
            PopStack(needReset);
            memory.Pop();
        }

        public void PushStack() => name.Push(new());
        public void PopStack(bool needReset)
        {
            if (name.TryPop(out var names))
            {
                foreach (var name in names)
                {
                    if (name.reference)
                        current.Remove(name.name);
                    else
                        remove(needReset, name.name);
                }
            }
            else
            {
                throw new Exception("no stack");
            }
        }

        public void AddToCurrent(string name, Data value, bool isAReference = false)
        {
            current.Add(name, value);
            this.name.Peek().Add((name, isAReference));
        }

        public Data this[string name]
        {
            get
            {
                string[] names = name.Split('.');
                Data output = current[names[0]];
                if (names.Length > 1)
                {
                    for (int i = 1; i < names.Length; i++)
                    {
                        if (output is not Container container)
                            throw new Exception($"not a container {i}");
                        output = container[names[i], this];
                    }
                }
                return output;
            }
        }

        protected short nextMemory = 0;
    }
}
