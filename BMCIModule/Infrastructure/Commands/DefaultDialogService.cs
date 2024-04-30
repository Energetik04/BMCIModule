using BMCIModule.Infrastructure.Interface.Base;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace BMCIModule.Infrastructure.Commands
{
    //реализация интерфейса открытия/сохранения файла
    public class DefaultDialogService : IDialogService
    {
        public string FilePath { get ; set ; }

        public string FileExt { get ; set ; }

        public string FileName { get ; set ; }

        public bool OpenFileDialog()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.DefaultExt = FileExt;
            if (openFileDialog.ShowDialog() == true)
            {
                FilePath=openFileDialog.FileName;
                return true;
            }
            return false;
        }

        public bool SaveFileDialog()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = FileName;
            saveFileDialog.DefaultExt = FileExt;
           if (saveFileDialog.ShowDialog() == true)
           {
           FilePath = saveFileDialog.FileName;
           return true;
           }
           return false;
        }

        public void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }
    }
}
