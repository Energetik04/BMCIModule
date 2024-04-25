using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Serialization;


namespace BMCIModuleWPF
{

    class Class1:INotifyPropertyChanged
    {
        public ICommand SavePresetWorksetToXMLCommand { get; }
        private bool CanSavePresetWorksetToXMLCommandExecute(object p) => true;
        private void OnSavePresetWorksetToXMLCommandExecute(object p)
        {
            string path = @"C:\Users\rnosevich\Documents";
            XmlSerializer serializer = new XmlSerializer(typeof(ObservableCollection<Department>));
            using (FileStream fs=new FileStream(path,FileMode.OpenOrCreate))
            {
                serializer.Serialize(fs, departmentCollection);
            }
        }

        public ObservableCollection<Department> departmentCollection { get; }
        public ICommand CloseAplicationCommand { get; } // Создание комманды

        private void OnCloseAplicationCommandExecuted(object p)
        {
            Application.Current.Shutdown();
        }
        private bool CanCloseAplicationCommandExecute(object p) => true;


        public Class1()
        {
            SetDepartmentBMCI setDepartmentBMCI = new SetDepartmentBMCI();
           
            departmentCollection = new ObservableCollection<Department>(setDepartmentBMCI.DepartmentArray);
            CloseAplicationCommand=new RelayCommand(OnCloseAplicationCommandExecuted, CanCloseAplicationCommandExecute);
            SavePresetWorksetToXMLCommand = new RelayCommand(OnSavePresetWorksetToXMLCommandExecute, CanSavePresetWorksetToXMLCommandExecute);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
