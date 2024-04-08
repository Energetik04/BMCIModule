using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Department
{
    public class Mark
    {
        string name;
        bool statusmark;

        public string Name { get => name; set => name = value; }
        public bool Statusmark { get => statusmark; set => statusmark = value; }

        public Mark() { }

        public Mark(string name)
        {
            Name = name;
        }

        public Mark(string name, bool statusmark)
        {
            Name = name;
            Statusmark = statusmark;
        }

    }
}
