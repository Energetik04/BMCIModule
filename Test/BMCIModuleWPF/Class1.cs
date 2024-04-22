using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;


namespace BMCIModuleWPF
{

    class Class1:INotifyPropertyChanged
    {
             

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
            
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
