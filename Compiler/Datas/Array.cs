using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class Array<T> : Data
        where T : Data
    {

        public short ElementSize { get; }
        public short Amount { get; }

        public Array(short address, short elementSize, short amount) : base(address, (short)(amount * elementSize))
        {
            ElementSize = elementSize;
            Amount = amount;
        }

        public static Func<short, Array<T2>> ConstructorOf<T2>(short amount)
            where T2 : ValueType
            => (address) => new Array<T2>(address, ValueType.Types[typeof(T).Name].size, amount);

        public static Func<short, Array<T2>> ConstructorOf<T2>(short elementSize, short amount)
            where T2 : Data
            => (address) => new Array<T2>(address, elementSize, amount);

        public T Get(short index, Func<short, T> constructor)
            => constructor((short)(Address + index));
    }
}
