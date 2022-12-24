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

        public Array(short address, short elementSize, short amount) : base(address, (short)(amount * elementSize))
        {
            ElementSize = elementSize;
            Amount = amount;
        }

        public override void Set(Compiler comp, CodeWriter codeWriter, string stringValue, bool needReset)
        {
            throw new Exception("Can't set an Array class");
        }

        public static Func<short, Array> ConstructorOf<T>(short amount)
            where T : ValueType
            => (address) => new Array(address, ValueType.Types[typeof(T).Name].size, amount);

        public static Func<short, Array> ConstructorOf(short elementSize, short amount)
            => (address) => new Array(address, elementSize, amount);

        public T Get<T>(short index, Func<short, T> constructor)
            where T : Data
            => constructor((short)(Address + index));

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
                    return Get(index, (address) => new ValueType(address, ElementSize));
                }
                throw new NotImplementedException();
            }
        }
    }
}
