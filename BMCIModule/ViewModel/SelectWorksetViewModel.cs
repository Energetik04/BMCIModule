using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Visual;
using BMCIModule.Department;
using BMCIModule.Infrastructure.Commands;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;


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
        /// <summary>
        /// Пока не работает через ShutDown
        /// </summary>
        public ICommand CloseAplicationCommand {  get; } // Создание комманды

        private void OnCloseAplicationCommandExecuted(object p)
        {
            Application.Current.MainWindow.Close();
        }
        private bool CanCloseAplicationCommandExecute(object p) => true;
        #endregion

        #region SaveFileXML
        /// <summary>
        /// Сохраниене файла в формает xml
        /// </summary>
        public ICommand SavePresetWorksetToXMLCommand { get; }
        private bool CanSavePresetWorksetToXMLCommandExecute(object p) => true;
        private void OnSavePresetWorksetToXMLCommandExecute(object p)
        {
            List<Department.Department> saveCollection=new List<Department.Department>(SetDepartmentBMCI.departmentArray);
            XmlSerializer xmlFile = new XmlSerializer(typeof(List<Department.Department>));
            StreamWriter file = new StreamWriter("saveCollectionXML.xml");
            xmlFile.Serialize(file, saveCollection);
            file.Close();
        }
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
            SavePresetWorksetToXMLCommand = new RelayCommand(OnSavePresetWorksetToXMLCommandExecute, CanSavePresetWorksetToXMLCommandExecute);
        }
        #endregion
    }
}
