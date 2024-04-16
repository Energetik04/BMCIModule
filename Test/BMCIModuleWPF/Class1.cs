using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModuleWPF
{

    class Class1
    {
        public ObservableCollection<Department> departmentCollection { get; }

        public Class1()
        {
            SetDepartmentBMCI setDepartmentBMCI = new SetDepartmentBMCI();
            departmentCollection = new ObservableCollection<Department>(setDepartmentBMCI.DepartmentArray);
            
        }
    }
}
