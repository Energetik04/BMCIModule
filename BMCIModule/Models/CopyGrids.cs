using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;


namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CopyGrids : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            FilteredElementCollector collectorGrid = new FilteredElementCollector(doc);
            FilteredElementCollector collectorLink = new FilteredElementCollector(doc);
           

            IList<Element> gridProject = collectorGrid.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Grids).ToElements();
            IList<Element> rvtLink = collectorLink.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_RvtLinks).ToElements();
            

            if (gridProject.Count != 0)
            {
                TaskDialog taskDialogGrid = new TaskDialog("Предупреждение пользователю");
                taskDialogGrid.MainContent = "В данном файле уже есть оси, выберите следующее действие:";
                
                taskDialogGrid.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Ничего не делать, получить список созданных осей");
                taskDialogGrid.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Пропустить");
                taskDialogGrid.AddCommandLink(TaskDialogCommandLinkId.CommandLink3, "Проигнорировать и создать оси, есть вероятность получить ошибку");
                taskDialogGrid.CommonButtons=TaskDialogCommonButtons.Close;
                taskDialogGrid.DefaultButton = TaskDialogResult.Close;
                TaskDialogResult tresult = taskDialogGrid.Show();
                if ( tresult==TaskDialogResult.CommandLink3 )
                {
                    try
                    {
                        foreach (Element element1 in rvtLink)
                        {
                            using (Transaction tCopyGrid = new Transaction(doc, "Копирование осей"))
                            {
                                tCopyGrid.Start();
                                IterateRvtLink(element1, doc, tCopyGrid);
                                tCopyGrid.Commit();
                            }
                        }
                    }
                    catch (Exception InvalidOperationException)
                    {
                        return Result.Succeeded;
                    }
                }
                else
                {
                    // не работает с закрыть
                    if((tresult==TaskDialogResult.CommandLink2) || (tresult == TaskDialogResult.Cancel))
                    {
                        /*message = "В данном файле присутствуют оси:";
                        foreach (Element grid in gridProject)
                        {
                            message +="\n ось"+(grid as Autodesk.Revit.DB.Grid).Name;
                        }*/
                    }
                    message = "Команда копирования осей прервана";
                 return Result.Cancelled;  
                }
            }
            else
            {
                foreach (Element element1 in rvtLink)
                {
                    using (Transaction tCopyGrid = new Transaction(doc, "Копирование осей"))
                    {
                        tCopyGrid.Start();
                        IterateRvtLink(element1, doc, tCopyGrid);
                        tCopyGrid.Commit();
                    }
                }
            }
            return Result.Succeeded;
        }

         void IterateRvtLink(Element element1,Document doc,Transaction tCopyGrid)
         {

            FilteredWorksetCollector collectorWorkset = new FilteredWorksetCollector(doc);
            //фильтр рабочих наборов и перечень
            WorksetKindFilter setFilter = new WorksetKindFilter(WorksetKind.UserWorkset);
            IList<Workset> setWorkset = collectorWorkset.WherePasses(setFilter).ToWorksets();
            //делегирование поиска и создание рабочего набора
            SearchWorkset getWorkset = new SearchWorkset(CreateWorkset.GetWorkset);
            Workset worksetLevelAndGrid = getWorkset(setWorkset, doc);

            RevitLinkInstance revitLinkInstance = element1 as RevitLinkInstance;
            FilteredElementCollector collectorElementInstance = new FilteredElementCollector(revitLinkInstance.GetLinkDocument());
            ICollection<Element> linkGrids = collectorElementInstance.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Grids).ToElements();
            // Добавить выбор семейства для марки внизу
            
           
                foreach (Element gridCreate in linkGrids)
                {
                Autodesk.Revit.DB.Grid linkGrid = gridCreate as Autodesk.Revit.DB.Grid;
                Line lineLinkGrid = linkGrid.Curve as Line;
                Autodesk.Revit.DB.Grid newGrid = Autodesk.Revit.DB.Grid.Create(doc, lineLinkGrid);
                newGrid.Name = linkGrid.Name;
                //Parameter setGridWorkset = newGrid.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                //setGridWorkset.Set(worksetLevelAndGrid.Id.IntegerValue);
                newGrid.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).Set(worksetLevelAndGrid.Id.IntegerValue);
                newGrid.Pinned = true;
                }
            
        }
    }
}
