using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;


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
            
            if (gridProject.Count > 0)
            {
                TaskDialog taskDialogGrid = new TaskDialog("Предупреждение пользователю");
                taskDialogGrid.MainContent = "В данном файле уже есть оси, при нажатии:"
                    + "\n\"Ok\" - существующие оси удаляться, будут добавлены оси из связи;" 
                    + "\n\"Cancel\" - никакие действия предприниматься не будут.";
                taskDialogGrid.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Ok");//исправить
                taskDialogGrid.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Cancel");// исправить и добавить + 1 кнопку
                taskDialogGrid.CommonButtons=TaskDialogCommonButtons.Close;
                taskDialogGrid.DefaultButton = TaskDialogResult.Close;
                TaskDialogResult tresult = taskDialogGrid.Show();
                if ( tresult==TaskDialogResult.CommandLink1 )
                {
                    foreach (Element element1 in rvtLink)
                    {
                        using (Transaction tCopyGrid = new Transaction(doc, "Копирование осей"))
                        {
                            tCopyGrid.Start();

                            RevitLinkInstance revitLinkInstance = element1 as RevitLinkInstance;
                            FilteredElementCollector collectorElementInstance = new FilteredElementCollector(revitLinkInstance.GetLinkDocument());
                            ICollection<Element> linkGrids = collectorElementInstance.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Grids).ToElements();
                            // Добавить выбор семейства для марки внизу
                            foreach (Element gridCreate in linkGrids)
                            {
                                Grid linkGrid = gridCreate as Grid;

                                Line lineLinkGrid = linkGrid.Curve as Line;
                                Grid newGrid = Grid.Create(doc, lineLinkGrid);
                                newGrid.Name = linkGrid.Name;
                                newGrid.Pinned = true;

                            }
                            tCopyGrid.Commit();
                        }
                    }
                }
                else
                {
                    // не работает с закрыть
                    if((tresult==TaskDialogResult.CommandLink2) || (tresult == TaskDialogResult.Cancel))
                    {
                        message = "В данном файле присутствуют оси:";
                        foreach (Element grid in gridProject)
                        {
                            message +="\n ось"+(grid as Grid).Name;
                        }
                    }
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

                        RevitLinkInstance revitLinkInstance = element1 as RevitLinkInstance;
                        FilteredElementCollector collectorElementInstance = new FilteredElementCollector(revitLinkInstance.GetLinkDocument());
                        ICollection<Element> linkGrids = collectorElementInstance.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Grids).ToElements();
                        // Добавить выбор семейства для марки внизу
                        foreach (Element gridCreate in linkGrids)
                        {
                            Grid linkGrid = gridCreate as Grid;

                            Line lineLinkGrid = linkGrid.Curve as Line;
                            Grid newGrid = Grid.Create(doc, lineLinkGrid);
                            newGrid.Name = linkGrid.Name;
                            newGrid.Pinned = true;

                        }
                        tCopyGrid.Commit();
                    }
                }
            }
            return Result.Succeeded;
        }
    }
    
}
