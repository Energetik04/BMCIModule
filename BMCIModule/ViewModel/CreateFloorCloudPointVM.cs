using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using BMCIModule.Infrastructure.Commands;

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

        private double stepMin;
        private int numberPoint;

        private string _Title = "Параметры преобразования пола";

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
            set { Set(ref numberPoint, value); }
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
            stepMin = 0;
            numberPoint = 0;
        }
    }
}
