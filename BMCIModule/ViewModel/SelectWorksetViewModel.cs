using Autodesk.Revit.UI;
using BMCIModule.Department;
using BMCIModule.Infrastructure.Commands;
using System;
using System.Collections.ObjectModel;
using System.IO;
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
        /// Пока не работает
        /// </summary>
        /// 
               
        internal Action Close;
        public ICommand CloseAplicationCommand {  get; } // Создание комманды

        private void OnCloseAplicationCommandExecuted(object p)
        {
            Close();
        }

        private bool CanCloseAplicationCommandExecute(object p) => true;
        #endregion

        #region SaveFileXML
        /// <summary>
        /// Сохраниене файла в формает xml (доработать проверку null)
        /// </summary>
        public ICommand SavePresetWorksetToXMLCommand { get; }
        private bool CanSavePresetWorksetToXMLCommandExecute(object p) => true;
        public void OnSavePresetWorksetToXMLCommandExecute(object p)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Department.Department>));
            defaultDialogService.FileExt = ".xml";
            defaultDialogService.FileName = "setPreset";
            //defaultDialogService.SaveFileDialog();
            if (defaultDialogService.SaveFileDialog() == true)
            {
                using (FileStream fs = new FileStream(defaultDialogService.FilePath, FileMode.OpenOrCreate))
                {
                    serializer.Serialize(fs, departmentCollection);
                }
                TaskDialog.Show("Сохранение", "Сохранил файл состава рабочих наборов");
            }
        }
        #endregion

        #region OpenFileXML
        /// <summary>
        /// Открытие файла и воссоздание коллекции обратно
        /// </summary>
        public ICommand OpenPresetWorkserToXMLCommand { get; }

        private bool CanOpenPresetWorkserToXMLCommandExecute(object p) => true;

        public void OnOpenPresetWorkserToXMLCommandExecute(object p)
        {
            XmlSerializer deSerializer=new XmlSerializer(typeof(ObservableCollection<Department.Department>));
            defaultDialogService.FileExt = ".xml";
            if (defaultDialogService.OpenFileDialog() == true)
            {
                using (FileStream fs = new FileStream(defaultDialogService.FilePath, FileMode.OpenOrCreate))
                {
                    //departmentCollection.Clear();
                    ObservableCollection<Department.Department> departmentCollectionTemp = new ObservableCollection<Department.Department>(deSerializer.Deserialize(fs) as ObservableCollection<Department.Department>);
                    departmentCollection= departmentCollectionTemp;
                    //foreach (Department.Department department in departmentCollectionTemp)
                    //{
                    //    departmentCollection.Add(department);
                    //}
                }
            }
        }
        #endregion

        #region Apply
        /// <summary>
        /// Применение рабочих наборов
        /// </summary>

        public ICommand ApplyAplicationCommand { get; }
        private void OnApplyAplicationCommandExecuted(object p)
        {
           
            Close();
        }

        private bool CanApplyAplicationCommandExecuted(object p) => true;


        #endregion

        #endregion

        #region Выбор рабочих наборов
        /// <summary>
        /// Окно выбора рабочих наборов
        /// </summary>
        DefaultDialogService defaultDialogService =new DefaultDialogService();

        // поле коллекции департаментов
        private ObservableCollection<Department.Department> _departmentCollection=new ObservableCollection<Department.Department>();
        // свойство коллекции департаментов
        public ObservableCollection<Department.Department> departmentCollection 
        {   get => _departmentCollection;
            set
            {
                Set(ref _departmentCollection, value);
            } 
        }
        #endregion

        public SelectWorksetViewModel()
        {
            departmentCollection = new ObservableCollection<Department.Department>(SetDepartmentBMCI.departmentArray);// необходимо обновление бинарника окна
            CloseAplicationCommand = new RelayCommand(OnCloseAplicationCommandExecuted, CanCloseAplicationCommandExecute);// есть рабочая лошадка и по ее конструктору создаем команду
            SavePresetWorksetToXMLCommand = new RelayCommand(OnSavePresetWorksetToXMLCommandExecute, CanSavePresetWorksetToXMLCommandExecute);
            OpenPresetWorkserToXMLCommand = new RelayCommand(OnOpenPresetWorkserToXMLCommandExecute, CanOpenPresetWorkserToXMLCommandExecute);
            ApplyAplicationCommand = new RelayCommand(OnApplyAplicationCommandExecuted, CanApplyAplicationCommandExecuted);
        }
     
    }
}
