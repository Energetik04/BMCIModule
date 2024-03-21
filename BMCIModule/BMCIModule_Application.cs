using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class BMCIModule_Application : IExternalApplication 
    {
        static AddInId addInId = new AddInId(new Guid("F8AF5982-6FE6-4C62-8F79-E0B6FF252F73"));
        public Result OnStartup (UIControlledApplication application)
        {
            string namePlugin = "BMCIModule";
            string nameRibbon1 = "Координация";
            
            application.CreateRibbonTab(namePlugin);
            RibbonPanel panelCoordination=application.CreateRibbonPanel(namePlugin,nameRibbon1);
            /* путь к сборке продумать и продумать 
             название видимое пользователям и добавить название класса*/
            PushButtonData dataButtonCoordination =new PushButtonData("CoordinationButton","Создание рабочего файла", @"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\bin\Debug\BMCIModule.dll", "BMCIModule.CoordinationFile");
            //добавление кнопки в панель и подсказок
            PushButton coordinationButton=panelCoordination.AddItem(dataButtonCoordination) as PushButton;
            coordinationButton.ToolTip = "Перенос площадки, координат базовой точки и точки съемки";
            // путь к картинкам либо изменить либо пересмотреть 
            coordinationButton.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\earth.png"));
            /*List<RibbonItem> items = new List<RibbonItem>();
            items.AddRange(panelCoordination.AddItem(coordinationButton));*/

            
            return Result.Succeeded;

        }
        public Result OnShutdown (UIControlledApplication application)
        {

            return Result.Succeeded;

        }
    }
}
