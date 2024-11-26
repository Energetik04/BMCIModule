using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.DB;
using BMCIModule.Infrastructure.Commands;
using BMCIModule.Models;
using Color = Autodesk.Revit.DB.Color;

namespace BMCIModule.ViewModel
{
    public partial class CreateFloorCloudPointVM:ViewModel.Base.ViewModel
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
                    new Uri(wayForIcon + @"\logoBack.png"));
        }
        public ImageSource LogoForWindow
        {
            get => new BitmapImage(
                new Uri(wayForIcon + @"\logoForWindow.png"));
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
        //вызывается каждый раз когда возникает OnPropertyChange
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
                            AddError(nameof(stepMin), "Необходимо числовой тип данных");
                        }
                        break;
                    case nameof(numberPoint):
                        if (numberPoint.GetType() == typeof(int))
                        {
                            AddError(nameof(numberPoint), "Число должно быть целочисленным");
                        }
                        break;
                    case nameof(captureRange):
                        if (captureRange.GetType() == typeof(double))
                        {
                            AddError(nameof(captureRange), "Необходимо числовой тип данных");
                        }
                        break;
                }
                return string.Empty;
            }
        }
        public string Error { get; }

        ////Возвращает список ошибок,либо пустой лист
        //public IEnumerable GetErrors(string propertyName)
        //{
        //    if (string.IsNullOrEmpty(propertyName))
        //    {
        //        return dictionaryErrors.Values;
        //    }
        //    return dictionaryErrors.ContainsKey(propertyName) ? dictionaryErrors[propertyName] : null;
        //}

        //public bool HasErrors => dictionaryErrors.Any();
        //public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        //private readonly Dictionary<string,List<string>> dictionaryErrors = new Dictionary<string,List<string>>();

        ////вызывает событие ErrorsChanged
        //private void OnErrorsChanged(string proportyName)
        //{
        //    ErrorsChanged?.Invoke(this,new DataErrorsChangedEventArgs(proportyName));
        //}

        //private void AddError(string propertyName, string error)
        //{
        //    AddErrors(propertyName, new List <string> {error});
        //}

        //private void AddErrors(string propertyName, IList<string> errors)
        //{
        //    if (errors == null || !errors.Any())
        //    {
        //        return;
        //    }

        //    var changed = false;
        //    if (!dictionaryErrors.ContainsKey(propertyName))
        //    {
        //        dictionaryErrors.Add(propertyName,new List<string>());
        //        changed=true;
        //    }
        //    //тут чехарда какая-то. надо разобраться
        //    foreach (var error in dictionaryErrors)
        //    {
        //        if (dictionaryErrors[propertyName].Contains(error.Key)) continue;
        //        dictionaryErrors[propertyName].Add(error.Key);
        //        changed=true;
        //    }

        //    if (changed)
        //    {
        //        OnErrorsChanged(propertyName);
        //    }
        //}

        //protected void ClearErrors(string propertyName = "")
        //{
        //    if (string.IsNullOrEmpty(propertyName))
        //    {
        //        dictionaryErrors.Clear();
        //    }
        //    else
        //    {
        //        dictionaryErrors.Remove(propertyName);
        //    }
        //    OnErrorsChanged(propertyName);
        //}
    }
}
