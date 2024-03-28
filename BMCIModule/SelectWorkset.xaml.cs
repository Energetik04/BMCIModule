using Autodesk.Revit.DB;
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


namespace BMCIModule
{
    /// <summary>
    /// Логика взаимодействия для SelectWorkset.xaml
    /// </summary>
    public partial class SelectWorkset : Window
    {
        List<string> tagName;
        public SelectWorkset(List<string> tagNameETO)
        {
            InitializeComponent();
            tagName=tagNameETO;
            //список получил,надо отрегулировать размеры получаймой дрыснни
            tagETOName.ItemsSource = tagName;
            
        }

        private void SetSelectWorkset(object sender,EventArgs e)
        {
            
        }
    }
}
