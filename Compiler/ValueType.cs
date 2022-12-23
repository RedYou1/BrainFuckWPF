using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public class ValueType : Data
    {

        public ValueType(short address, short size) : base(address, size) { }

        public virtual void Add(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            throw new Exception("Can't add a ValueType class");
        }

        public virtual void Sub(Compiler comp, CodeWriter codeWriter, string stringValue)
        {
            throw new Exception("Can't sub a ValueType class");
        }


        public static Dictionary<string, (short size, Func<short, ValueType> constructor)> Types =
        new(){
            { nameof(Bool),(1,Bool.Constructor) },
            { nameof(Byte),(1,Byte.Constructor) },
            { nameof(Char),(1,Char.Constructor) },
            { nameof(Short),(2,Short.Constructor) }
        };
    }
}
