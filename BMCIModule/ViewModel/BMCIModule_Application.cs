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
            string  wayForApplication=@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\bin\Debug\BMCIModule.dll";
            
            application.CreateRibbonTab(namePlugin);
            RibbonPanel panelCoordination=application.CreateRibbonPanel(namePlugin,nameRibbon1);

            //добавление кнопки координации
            AddButtonCoordination(panelCoordination);
            

            void AddButtonCoordination(RibbonPanel panel) 
            {
                /* путь к сборке продумать и продумать 
             название видимое пользователям и добавить название класса*/
                PushButtonData dataButtonCoordination = new PushButtonData("CoordinationButton", "Копирование координат", wayForApplication, "BMCIModule.CoordinationFile");
                dataButtonCoordination.ToolTip = "Перенос площадки, координат базовой точки и точки съемки с файла имеющий название Координационный";
                // путь к картинкам либо изменить либо пересмотреть 
                dataButtonCoordination.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\Vector.png"));


                // кнопка копирование осей
                PushButtonData copyGridButtonData = new PushButtonData("CopyGridButton", "Копирование осей", wayForApplication, "BMCIModule.CopyGrids");
                copyGridButtonData.ToolTip = "Копирование осей из связанного файла имеющего название Координационный, так же создает рабочий набор с именем #BMCI - ОБЩИЕ УРОВНИ И СЕТКИ";
                copyGridButtonData.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\Scale-32-Dark.png"));

                // кнопка копирования уровней
                PushButtonData copyLevelButtonData = new PushButtonData("CopyLevelButton","Копирование уровней",wayForApplication, "BMCIModule.CopyLevel");
                copyLevelButtonData.ToolTip = "Копирование уровней из связанного файла имеющего название Координационный, так же создает рабочий набор с именем #BMCI - ОБЩИЕ УРОВНИ И СЕТКИ";
                copyLevelButtonData.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\Database-32-Light.png"));
                // Объединение кнопок в одну
                SplitButtonData splitCopy = new SplitButtonData("splitCoordination", "Split");
                SplitButton splitButtonCopy=panel.AddItem(splitCopy) as SplitButton;

                splitButtonCopy.AddPushButton(dataButtonCoordination);
                splitButtonCopy.AddPushButton(copyGridButtonData);
                splitButtonCopy.AddPushButton(copyLevelButtonData);

                //кнопка выбора набора марок и создания рабочих наборов
                PushButtonData selectSetTagWorkset = new PushButtonData("CreareWorkset", "Создание рабочих наборов", wayForApplication, "BMCIModule.CreateWorkset");
                selectSetTagWorkset.ToolTip = "Создание рабочих набором путем выбора марок комплектов";
                selectSetTagWorkset.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\CreateWorkSet.png"));
                PushButton selectTagName = panelCoordination.AddItem(selectSetTagWorkset) as PushButton;
            }

           






            return Result.Succeeded;

        }
        public Result OnShutdown (UIControlledApplication application)
        {

            return Result.Succeeded;

        }
    }
}
