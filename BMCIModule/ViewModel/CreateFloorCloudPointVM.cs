using System;
using System.Drawing;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using BMCIModule.Infrastructure.Commands;
using Color = Autodesk.Revit.DB.Color;

namespace BMCIModule.ViewModel
{
    internal class CreateFloorCloudPointVM:ViewModel.Base.ViewModel
    {
        #region CloseAplicationCommand
        /// <summary>
        /// Пока не работает
        /// </summary>
        /// 
        internal Action Close;

        private string _Title = "Параметры преобразования пола";
        private int numberPoint;
        private double stepMin;
        private double captureRange;

        public ImageSource BackgroundWindow
        {
            get => new BitmapImage(
                    new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\logoBack.png"));
        }
        public ImageSource LogoForWindow
        {
            get => new BitmapImage(
                new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\logoForWindow.png"));
        }

        public Color TrueType
        {
            get => new Color(Colors.LawnGreen.R, Colors.LawnGreen.G, Colors.LawnGreen.B);
        }
        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }
        public double Step
        {
            get => stepMin;
            set
            {
                if (value is double)
                {
                    Set(ref stepMin, value);

                }
                else
                {

                }
                
            }
        }

        public int NumberPoint
        {
            get => numberPoint;
            set { Set(ref numberPoint, value); }
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

        public ICommand CloseAplicationCommand { get; } // Создание комманды

        private void OnCloseAplicationCommandExecuted(object p)
        {
            Close();
        }

        private bool CanCloseAplicationCommandExecute(object p) => true;
        #endregion

        #region Apply
        /// <summary>
        /// Применение рабочих наборов
        /// </summary>

        public ICommand ApplyAplicationCommand { get; }
        private void OnApplyAplicationCommandExecuted(object p)
        {
            Close();
        }

        private bool CanApplyAplicationCommandExecuted(object p) => true;
        
        #endregion

        public CreateFloorCloudPointVM()
        {
            CloseAplicationCommand =
                new RelayCommand(OnCloseAplicationCommandExecuted, CanCloseAplicationCommandExecute);
            ApplyAplicationCommand =
                new RelayCommand(OnApplyAplicationCommandExecuted, CanApplyAplicationCommandExecuted);
        }
    }
}
