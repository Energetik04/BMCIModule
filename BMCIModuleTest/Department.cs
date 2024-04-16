using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModuleTest
{
    public class Department
    {

        public string Name { get; set; }

        public string Tag { get; set; }
        public ICollection<Mark> ListMark { get; set; }

        /*public Department(string name)
        {
            Name = name;
        }

        public Department(string name, string tag)
        {
            Name = name;
            Tag = tag;
        }*/
        public Department() { }

        public Department(string name, string tag, ICollection<Mark> marks)
        {
            Name = name;
            Tag = tag;
            ListMark = marks;
        }

    }
}
