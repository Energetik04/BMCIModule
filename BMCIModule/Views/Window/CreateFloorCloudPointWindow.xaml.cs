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
            Topmost = true;
            //textStepMin.ToolTip = "При создании треугольников внутри фигуры"+"\n"+"будет использоваться равносторонний треугольник"+"\n"+"с сторонами указанными в этом параметре.";
            //textNumberPoint.ToolTip = "Количество точек из облака точек,"+"\n"+"попадающих в область треугольника,"+"\n"+ "для определения высотной отметки";
            //textCaptureRange.ToolTip = "Диапазон по вертикали для захвата облако точек в области треугольника,"+"\n"+"применяется если облако точек охватывает все сооружение";

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
