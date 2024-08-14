using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BMCIModule.Views.Window
{
    /// <summary>
    /// Логика взаимодействия для CreateFloorCloudPointWindow.xaml
    /// </summary>
    public partial class CreateFloorCloudPointWindow : System.Windows.Window
    {
        
        public CreateFloorCloudPointWindow()
        {
            InitializeComponent();
            stepMinWindow.Text = "0";
            numberPoint.Text = "0";
            captureRange.Text = "0";
            Topmost = true;
        }

        private void ClickCloseWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult=false;
        }

        private void ClickApplyWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult=true;
        }
    }
}
