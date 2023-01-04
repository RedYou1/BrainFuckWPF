using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Compiler
{
    public class Struct : ValueType, Container
    {
        public string? name;
        public override string Name => name!;

        public (string name, Data data)[] Datas { get; }

        public Struct(short address, (string name, Data data)[] datas)
            : base(address, (short)datas.Sum(d => d.data.Size))
        {
            Datas = datas;
        }

        public bool ContainsKey(string name)
        {
            if (Regex.Match(name, @"^'\\{0,1}.'$").Success || Regex.Match(name, "^\"\\\\{0,1}.+\"$").Success)
            {
                name = String.GetValue(name);
                return Datas.Any(d => d.name == name);
            }
            return false;
        }

        public Data this[string name, Memory memory]
        {
            get
            {
                if (Regex.Match(name, @"^'\\{0,1}.'$").Success || Regex.Match(name, "^\"\\\\{0,1}.+\"$").Success)
                {
                    name = String.GetValue(name);
                    return Datas.First(d => d.name == name).data;
                }
                throw new NotImplementedException();
            }
        }
    }
}
