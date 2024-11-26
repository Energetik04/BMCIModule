using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BMCIModuleTest
{
    public partial class CreateFloorCloudPointVM:ViewModel
    {
        //TODO проверка вводных данных в поля черезе события
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
                    new Uri(null));
        }
        public ImageSource LogoForWindow
        {
            get => new BitmapImage(
                new Uri(null));
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
                Set(ref stepMin, value);
            }
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

        //private bool _checkEnableButtom;

        //public bool CheckEnableButtom
        //{
        //    get => _checkEnableButtom;
        //    set
        //    {
        //        if ((stepMin.GetType() == typeof(double) || stepMin.GetType() == typeof(int)) &&
        //            (numberPoint.GetType() == typeof(int)) && (captureRange.GetType() == typeof(double) ||
        //                                                       captureRange.GetType() == typeof(int)))
        //        {
        //            Set(ref _checkEnableButtom, true);
        //        }
        //        else
        //        {
        //            Set(ref _checkEnableButtom, false);
        //        }
        //    }
        //}

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

        public ICommand CloseApplicationCommand { get; } // Создание комманды

        private void OnCloseApplicationCommandExecuted(object p)
        {
            Close();
        }

        private bool CanCloseApplicationCommandExecute(object p) => true;
        

        #region Apply
        
        public ICommand ApplyApplicationCommand { get; }
        private void OnApplyApplicationCommandExecuted(object p)
        {
            Close();
        }

        private bool CanApplyApplicationCommandExecuted(object p)=> true;
        
        #endregion

        public CreateFloorCloudPointVM()
        {
            CloseApplicationCommand =
                new RelayCommand(OnCloseApplicationCommandExecuted, CanCloseApplicationCommandExecute);
            ApplyApplicationCommand =
                new RelayCommand(OnApplyApplicationCommandExecuted, CanApplyApplicationCommandExecuted);
        }
    }
    public partial class CreateFloorCloudPointVM : IDataErrorInfo
    {
        //вызывается каждый раз когда возникает OnProportyChange
        //в качестве параметра columnName индексатор использует имя свойства из события
        //public string this[string columnName] => string.Empty;

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(stepMin):
                        if (stepMin.GetType() == typeof(double))
                        {
                            string eror = "huita";
                            AddError(nameof(stepMin), eror);
                        }
                        break;
                    case nameof(numberPoint):
                        if (numberPoint.GetType() == typeof(int))
                        {
                            string eror = "Число должно быть целочисленным";
                            AddError(nameof(numberPoint), eror);
                        }
                        break;
                    case nameof(captureRange):
                        if (captureRange.GetType() == typeof(double))
                        {
                            string eror = "Необходимо числовой тип данных";
                            AddError(nameof(captureRange), eror);
                        }
                        break;
                }
                return string.Empty;
            }
        }
        public string Error { get; }
    }
}

