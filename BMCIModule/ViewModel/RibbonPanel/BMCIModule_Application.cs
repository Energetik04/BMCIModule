using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AW=Autodesk.Windows;
using Color = System.Windows.Media.Color;


// Нужно добавить регионы
namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class BMCIModule_Application : IExternalApplication
    {
        private static AW.RibbonItem _executedRibbon;

        public static AW.RibbonItem ExecutedItem
        {
            get { return _executedRibbon; }
        }


        static AddInId addInId = new AddInId(new Guid("F8AF5982-6FE6-4C62-8F79-E0B6FF252F73"));
        //получение пути к файлу
        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
        private string str = AssemblyDirectory;
        //
        public Result OnStartup (UIControlledApplication application)
        {
            //adWin.RibbonControl ribbon = adWin.ComponentManager.Ribbon;
            string namePlugin = "BMCIModule";
            string ribbonCoordination = "Координация";
            string panelFactConstruction = "Фактические конструкции";
            string ribbonLogo = "ОАО Белгорхимпром";
            string  wayForApplication=@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\bin\Debug\BMCIModule.dll";
            AW.ComponentManager.UIElementActivated += ComponentManager_UIElementActivated;
            
            application.CreateRibbonTab(namePlugin);

            RibbonPanel logo = application.CreateRibbonPanel(namePlugin, ribbonLogo);
            RibbonPanel panelCoordination=application.CreateRibbonPanel(namePlugin,ribbonCoordination);
            RibbonPanel panelFloor= application.CreateRibbonPanel(namePlugin, panelFactConstruction);

            AW.RibbonControl ribbon = AW.ComponentManager.Ribbon;
            // картинку на фон кнопок
            ImageSource imgbg = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\logov2.png"));
            ImageBrush picBrush = new ImageBrush();
            picBrush.ImageSource = imgbg;
            picBrush.AlignmentX = AlignmentX.Center;
            picBrush.AlignmentY = AlignmentY.Center;
            picBrush.Stretch = Stretch.None;
            picBrush.TileMode = TileMode.None;

            //градиент цвет панели
            LinearGradientBrush gradientBrush = new LinearGradientBrush();
            gradientBrush.StartPoint = new System.Windows.Point(0, 0);
            gradientBrush.EndPoint = new System.Windows.Point(0, 1);
            gradientBrush.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromRgb(129,0,63), 1));
            foreach (AW.RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Title==namePlugin)
                {
                    foreach (AW.RibbonPanel panel in tab.Panels)
                    {
                        //ribbon.FontFamily=new System.Windows.Media.FontFamily("Bauhaus 93");
                        //ribbon.FontSize = 14;

                        // бурал пока градиент , иначе букв не видно
                        //panel.CustomPanelTitleBarBackground = gradientBrush;
                        if (panel.Source.Name == ribbonLogo)
                        {
                            //panel.ResizeItemWidthPriority;
                            panel.CustomPanelBackground = picBrush;
                            panel.ResizeStyle = AW.RibbonResizeStyles.NeverHideText;
                        }

                        //panel.CustomPanelBackground = picBrush; // use your picture
                        //panel.CustomPanelBackground 
                        //  = gradientBrush; // use your gradient fill
                    }
                }
                //можно подписаться на события панели и вкладки
            }
            //AW.ComponentManager.UIElementActivated += new EventHandler<AW.UIElementActivatedEventArgs>(ComponentManager_UIElementActivated);//проверить запись


            //добавление кнопки координации
            AddButtonCoordination(panelCoordination);
            AddButtonFloor(panelFloor);
            
            #region Действующая панель


            // Работа с полами,панель
            void AddButtonFloor(RibbonPanel panel)
            {
                //пол
                PushButtonData changeFloorToPointCloudData = new PushButtonData("CreateFloorCloudPointWindow", "Пол", wayForApplication, "BMCIModule.CreateFloorOrRoofCloudPoint");
                changeFloorToPointCloudData.ToolTip = "Изменение формы пола по облаку точек";
                // путь к картинкам либо изменить либо пересмотреть 
                changeFloorToPointCloudData.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\Vector.png"));
                PushButton createFloorToPointCloudDataButton = panel.AddItem(changeFloorToPointCloudData) as PushButton;
                //кровля
                PushButtonData changeRoofToPointCloudData = new PushButtonData("CreateRoofCloudPointWindow", "Кровля", wayForApplication, "BMCIModule.CreateFloorOrRoofCloudPoint");
                changeRoofToPointCloudData.ToolTip = "Изменение формы кровли по облаку точек";

                // путь к картинкам либо изменить либо пересмотреть 
                changeRoofToPointCloudData.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\Vector.png"));
                PushButton createRoofToPointCloudDataButton = panel.AddItem(changeRoofToPointCloudData) as PushButton;
                //анализ
                PushButtonData createAnalysisFloor = new PushButtonData("createAnalysisFloor", "Анализ пола", wayForApplication, "BMCIModule.СreateAnalysisFloor");
                createAnalysisFloor.ToolTip = "Создание вида с аналитической поверхностью, для более наглядного представления неровностей пола, путем нанесенгия цветной диаграммы";
                // путь к картинкам либо изменить либо пересмотреть 
                createAnalysisFloor.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\Vector.png"));
                PushButton createAnalysisFloorButton = panel.AddItem(createAnalysisFloor) as PushButton;
            }

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
                PushButtonData copyLevelButtonData = new PushButtonData("CopyLevelButton", "Копирование уровней", wayForApplication, "BMCIModule.CopyLevel");
                copyLevelButtonData.ToolTip = "Копирование уровней из связанного файла имеющего название Координационный, так же создает рабочий набор с именем #BMCI - ОБЩИЕ УРОВНИ И СЕТКИ";
                copyLevelButtonData.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\Database-32-Light.png"));
                // Объединение кнопок в одну
                SplitButtonData splitCopy = new SplitButtonData("splitCoordination", "Split");
                SplitButton splitButtonCopy = panel.AddItem(splitCopy) as SplitButton;

                splitButtonCopy.AddPushButton(dataButtonCoordination);
                splitButtonCopy.AddPushButton(copyGridButtonData);
                splitButtonCopy.AddPushButton(copyLevelButtonData);

                //кнопка выбора набора марок и создания рабочих наборов
                PushButtonData selectSetTagWorkset = new PushButtonData("CreareWorkset", "Создание рабочих наборов", wayForApplication, "BMCIModule.CreateWorkset");
                selectSetTagWorkset.ToolTip = "Создание рабочих набором путем выбора марок комплектов";
                selectSetTagWorkset.LargeImage = new BitmapImage(new Uri(@"C:\ProgramData\Autodesk\Revit\Addins\2021\BMCIModule\BMCIModule\icon\CreateWorkSet.png"));
                PushButton selectTagName = panel.AddItem(selectSetTagWorkset) as PushButton;
                
            }
            
            #endregion
            
            return Result.Succeeded;

        }

        private void ComponentManager_UIElementActivated(object sender, AW.UIElementActivatedEventArgs e)
        {
            _executedRibbon = e.Item;
        }

        

        public AW.RibbonItem GetButton(string tabName,string panelName, string itemName)
        {
            AW.RibbonControl ribbon = AW.ComponentManager.Ribbon;
            foreach (var tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    foreach (var panels in tab.Panels)
                    {
                        if (panels.Source.Title == panelName)
                        {
                            return panels.FindItem("CustomCtrl_%CustomCtrl_%"+tabName+"%"+panelName+"%"+itemName,true) as AW.RibbonItem;;
                        }
                    }
                }
            }
            return null;
        }
        public Result OnShutdown (UIControlledApplication application)
        {

            return Result.Succeeded;

        }
    }
}
