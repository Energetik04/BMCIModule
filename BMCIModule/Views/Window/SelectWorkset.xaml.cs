using System.Windows;




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
    }
}
