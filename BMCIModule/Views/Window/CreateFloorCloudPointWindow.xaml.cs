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
using Autodesk.Revit.DB;

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
        }

        private void ClickCloseWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult=false;
        }

        private void ClickApplyWindow(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        //bool IsValid(DependencyObject node)
        //{
        //    if (node != null)
        //    {
        //        // Check if dependency object is valid.
        //        // NOTE: Validation.GetHasError works for controls that have validation rules attached
        //        bool isValid = !Validation.GetHasError(node);
        //        if (!isValid)
        //        {
        //            // If the dependency object is invalid, and it can receive the focus,
        //            // set the focus
        //            if (node is IInputElement) Keyboard.Focus((IInputElement)node);
        //            return false;
        //        }
        //    }

        //    // If this dependency object is valid, check all child dependency objects
        //    foreach (object subnode in LogicalTreeHelper.GetChildren(node))
        //    {
        //        if (subnode is DependencyObject)
        //        {
        //            // If a child dependency object is invalid, return false immediately,
        //            // otherwise keep checking
        //            if (IsValid((DependencyObject)subnode) == false) return false;
        //        }
        //    }

            // All dependency objects are valid
            //return true;
    }
}
