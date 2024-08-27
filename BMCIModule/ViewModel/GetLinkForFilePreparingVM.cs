using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Autodesk.Revit.UI;
using BMCIModule.Infrastructure.Commands;

namespace BMCIModule.ViewModel
{
    internal class GetLinkForFilePreparingVM:Base.ViewModel
    {
        #region Заголовок

        private string _Title= "Выбор файла для координации";

        public string Title
        {
            get => _Title;
            set => Set(ref _Title, value);
        }

        private List<string> _Links = new List<string>();

        public List<string> Links
        {
            get => _Links;
            set => Set(ref _Links, value);
        }

        #endregion


        #region CloseApplicationCommand

        internal Action Close;
        public ICommand CloseApplicationCommand { get; }

        private void OnCloseApplicationCommandExecute(object p)
        {
            Close();
        }

        private bool CanCloseApplicationCommandExecute(object p) => true;

        #endregion


        public GetLinkForFilePreparingVM()
        {
            CloseApplicationCommand =
                new RelayCommand(OnCloseApplicationCommandExecute, CanCloseApplicationCommandExecute);

        }
    }
}
