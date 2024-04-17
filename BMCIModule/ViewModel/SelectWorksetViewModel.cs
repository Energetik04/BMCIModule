using Autodesk.Revit.DB;
using BMCIModule.Department;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;


namespace BMCIModule.ViewModel
{
    internal class SelectWorksetViewModel:Base.ViewModel
    {
        #region Заголовок
        private string _Title="Создание рабочих наборов";
        public string  Title
        {
            get => _Title; //возвращаем свойство
            set => Set(ref _Title,value);
        }
        #endregion Заголовок

        #region Команды

        #endregion 

       public ObservableCollection<Department.Department> departmentCollection 
        {
            get
            {
                setDepartmentBMCI.PropertyChanging += CreateSetDepartmet();
                return departmentCollection;
            }
            set { }
            
        }
        SetDepartmentBMCI setDepartmentBMCI = new SetDepartmentBMCI();

        public  void CreateSetDepartmet()
        {
            departmentCollection = new ObservableCollection<Department.Department>(SetDepartmentBMCI.departmentArray);
        }

        public SelectWorksetViewModel()
        {
            
           // departmentCollection = new ObservableCollection<Department.Department>(SetDepartmentBMCI.departmentArray);// необходимо обновление бинарника окна
                               
        }

    }
}
