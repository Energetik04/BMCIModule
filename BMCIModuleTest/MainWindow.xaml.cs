using System.Windows;

namespace BMCIModuleTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        private void ClickCloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ClickApplyWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

    }
}