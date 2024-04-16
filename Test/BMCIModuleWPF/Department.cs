using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModuleWPF
{
    public class Department
    {
        private string _name;
        private string _tag;
        private List<Mark> _listmark=new List<Mark>();
                
        public string Name { get { return _name; } set { _name = value; } }

        public string Tag { get {return _tag; } set {_tag=value;} }
        public List<Mark> ListMark { get {return _listmark; } set { _listmark=value; } }
        public Department() { }

        public Department(string name, string tag, List<Mark> marks)
        {
            Name = name;
            Tag = tag;
            ListMark = marks;
        }

    }
}
