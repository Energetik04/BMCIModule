using Autodesk.Revit.DB;
using BMCIModule.ViewModel;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;



namespace BMCIModule
{
    /// <summary>
    /// Логика взаимодействия для SelectWorkset.xaml
    /// </summary>
    public partial class SelectWorksetWindow : Window
    {
        public SelectWorksetWindow()
        {
            InitializeComponent();
            Topmost = true;
        }

        private void ClickCloseWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult=false;
        }

        private void ClickApplyWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
    }
}
