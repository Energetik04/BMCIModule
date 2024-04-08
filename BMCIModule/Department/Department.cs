using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BMCIModule.Department
{
    public class Department
    {
        string name;
        string tag;
        List<Mark> listMark = new List<Mark>();


        public string Name { get { return Name; } set { Name = value; } }

        public string Tag { get => Tag; set => Tag = value; }
        public List<Mark> ListMark { get => listMark; set => listMark = value; }

        public Department(string name)
        {
            this.name = name;
        }

        public Department(string name, string tag)
        {
            this.name = name;
            this.tag = tag;
        }
        public Department() { }

        public List<Mark> AddItemListMark(List<Mark> mark)
        {
            listMark = mark;
            return listMark;
        }
    }

}
