using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BMCIModule.ViewModel
{
    public class CreateAnalysisFaceVM:Base.ViewModel
    {
        #region Сборка
        static Assembly assembly = Assembly.GetExecutingAssembly();
        AppDomain domain = AppDomain.CurrentDomain;
        static string wayForApplication = assembly.Location;
        //метод статический поэтому инициализатор должен быть статик
        string wayForIcon = Path.GetDirectoryName(wayForApplication) + @"\icon";
        #endregion


        internal Action Close;
        private string _Title = "Параметры преобразования пола";
        private int numberPoint;
        private double stepMin;
        private double captureRange;
        
        public ImageSource BackgroundWindow
        {
            get => new BitmapImage(
                new Uri(wayForIcon+@"C\logoBack.png"));
        }
        public ImageSource LogoForWindow
        {
            get => new BitmapImage(
                new Uri(wayForIcon+@"\logoForWindow.png"));
        }

        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }
        public double Step
        {
            get => stepMin;
            set => Set(ref stepMin, value);
        }

        public int NumberPoint
        {
            get => numberPoint;
            set => Set(ref numberPoint, value);
        }

        public double CaptureRange
        {
            get => captureRange;
            set => Set(ref captureRange, value);
        }

        private bool _checkBoxMax;
        public bool CheckBoxMax
        {
            get => _checkBoxMax;
            set => Set(ref _checkBoxMax, value);
        }

        private bool _checkBoxMin;

        public bool CheckBoxMin
        {
            get => _checkBoxMin;
            set => Set(ref _checkBoxMin, value);
        }

        private bool _checkBoxBottomFace;
        public bool CheckBoxBottomFace
        {
            get => _checkBoxBottomFace;
            set => Set(ref _checkBoxBottomFace, value);
        }

        public string ToolTipStepMin
        {
            get => "При создании треугольников внутри фигуры" + "\n" + "будет использоваться равносторонний треугольник" + "\n" + "с сторонами указанными в этом параметре.";
        }

        public string ToolTipNumberPoint
        {
            get => "Количество точек из облака точек," + "\n" + "попадающих в область треугольника," + "\n" + "для определения высотной отметки";
        }

        public string ToolTipCaptureRange
        {
            get => "Диапазон по вертикали для захвата облако точек в области треугольника," + "\n" + "применяется если облако точек охватывает все сооружение";
        }
    }
}
