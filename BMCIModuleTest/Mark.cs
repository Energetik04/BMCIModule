using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModuleTest
{
    public class Mark
    {
        public string Name { get; set; }
        public bool Statusmark { get; set; }

        public Mark() { }

        //public Mark(string name)
        //{
        //    Name = name;
        //}

        public Mark(string name, bool statusmark)
        {
            Name = name;
            Statusmark = statusmark;
        }

    }
}
