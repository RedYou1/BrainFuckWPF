using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compiler
{
    public interface Container
    {
        public bool ContainsKey(string name);
        public Data this[string name, Memory memory] { get; }
    }
}
