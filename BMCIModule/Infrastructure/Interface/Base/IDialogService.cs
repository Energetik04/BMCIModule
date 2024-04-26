using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Infrastructure.Interface.Base
{
    // дефолтный интерфейс который поможет создать диалоговые окна 
    internal interface IDialogService
    {
        void ShowMessage(string message);//сообщение
        string FilePath { get; set; }//получение пути в окне (Save/open)

        bool OpenFileDialog();//открытие файла
        bool SaveFileDialog();// сохранение файла
    }
}
