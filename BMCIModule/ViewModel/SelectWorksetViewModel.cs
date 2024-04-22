using Autodesk.Revit.DB;
using BMCIModule.Department;
using BMCIModule.Infrastructure.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;


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

        #region CloseAplicationCommand
        public ICommand CloseAplicationCommand {  get; } // Создание комманды

        private void OnCloseAplicationCommandExecuted(object p)
        {
            Application.Current.Shutdown();
        }
        private bool CanCloseAplicationCommandExecute(object p) => true;
         #endregion


        #endregion

        #region Выбор рабочих наборов
        /// <summary>
        /// Окно выбора рабочих наборов
        /// </summary>
        public ObservableCollection<Department.Department> departmentCollection { get; }// свойство коллекции департаментов
        public SelectWorksetViewModel()
        {
            departmentCollection = new ObservableCollection<Department.Department>(SetDepartmentBMCI.departmentArray);// необходимо обновление бинарника окна
            CloseAplicationCommand = new RelayCommand(OnCloseAplicationCommandExecuted,CanCloseAplicationCommandExecute);// есть рабочая лошадка и по ее конструктору создаем команду

        }
        #endregion
    }
}
