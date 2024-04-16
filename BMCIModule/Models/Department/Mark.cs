using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Department
{
    public class Mark:ICollection
    {
        private string _name;
        private bool _statusMark;
        public string Name { get => _name; set => _name = value; }
        public bool StatusMark { get => _statusMark; set => _statusMark = value; }

        public int Count => throw new NotImplementedException();

        public object SyncRoot => throw new NotImplementedException();

        public bool IsSynchronized => throw new NotImplementedException();

        public Mark() { }

        //public Mark(string name)
        //{
        //    Name = name;
        //}

        public Mark(string name, bool statusmark)
        {
            Name = name;
            StatusMark = statusmark;
        }

        public void CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}
