using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
            this.DialogResult = false;
        }

        private void ClickApplyWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        private void valueStepMin_LostFocus(object sender, RoutedEventArgs e)
        {
            double valueInString;
            try
            {
                valueInString = double.Parse(valueStepMin.Text);
                if (valueInString.GetType() != typeof(double)) ApplyButton.IsEnabled = false;
            }
            catch
            {
                ApplyButton.IsEnabled = false;
            }
        }

    }
}