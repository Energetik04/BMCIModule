using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Input;




namespace BMCIModule
{
    /// <summary>
    /// Логика взаимодействия для SelectWorkset.xaml
    /// </summary>
    public partial class SelectWorkset : Window
    {
       
        public SelectWorkset()
        {
            InitializeComponent();
            
        }

        private void Close_SelectWorkset(object sender, RoutedEventArgs e)
        {
            Close();
        }

        //private void SaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = true;
        //}
        //private void SaveCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        //{
        //    var saveDlg = new SaveFileDialog { Filter = "Text Files|*.txt" };
        //    // Был ли совершен щелчок на кнопке ОК?
        //    if (true == saveDlg.ShowDialog())
        //    {
        //        // Сохранить данные из TextBox в указанном файле.
        //        File.WriteAllText(saveDlg.FileName, txtData.Text);
        //    }
        //}
    }
}
