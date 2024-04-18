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
        /// <summary>
        /// Окно создания рабочих наборов
        /// </summary>
        private string _Title="Создание рабочих наборов";
        public string  Title
        {
            get => _Title; //возвращаем свойство
            set => Set(ref _Title,value);
        }
        #endregion Заголовок

        #region Команды

        #endregion

        #region Выбор рабочих наборов
        /// <summary>
        /// Окно выбора рабочих наборов
        /// </summary>
        public ObservableCollection<Department.Department> departmentCollection { get; }// свойство коллекции департаментов
        public SelectWorksetViewModel()
        {
            SetDepartmentBMCI setDepartmentBMCI = new SetDepartmentBMCI();
            departmentCollection = new ObservableCollection<Department.Department>(SetDepartmentBMCI.departmentArray);// необходимо обновление бинарника окна
        }
        #endregion
    }
}
