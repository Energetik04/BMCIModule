using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BMCIModule.Department
{
    public class Department
    {

        private string _name;
        private string _tag;
        private List<Mark> _listmark=new List<Mark>();

        public string Name { get { return _name; } set { _name = value; } }

        public string Tag { get { return _tag; } set { _tag = value; } }
        public List<Mark> ListMark 
        { get => _listmark;
          set => _listmark = value;
        }
        
        public Department() { }

        public Department(string name, string tag, List<Mark> marks)
        {
            Name = name;
            Tag = tag;
            ListMark = marks;
        }

    }

}
