using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class Array : Data, Container
    {
        public short ElementSize { get; }
        public short Amount { get; }
        public Func<short, Data> Constructor { get; }

        public Array(short address, short elementSize, short amount, Func<short, Data> constructor) : base(address, (short)(amount * elementSize))
        {
            ElementSize = elementSize;
            Amount = amount;
            Constructor = constructor;
        }

        public static Func<short, Array> ConstructorOf<T>(short amount)
            where T : ValueType
        {
            var t = ValueType.Types[typeof(T).Name];
            return (address) => new Array(address, t.size, amount, t.constructor);
        }

        public static Func<short, Array> ConstructorOf(short elementSize, short amount, Func<short, Data> constructor)
            => (address) => new Array(address, elementSize, amount, constructor);

        public Data Get(short index)
            => Constructor((short)(Address + index * ElementSize));

        public bool ContainsKey(string name)
        {
            if (short.TryParse(name, out short index))
            {
                return 0 <= index && index < Amount;
            }
            return false;
        }

        public Data this[string name, Memory memory]
        {
            get
            {
                if (short.TryParse(name, out short index))
                {
                    return Get(index);
                }
                throw new NotImplementedException();
            }
        }
    }
}
