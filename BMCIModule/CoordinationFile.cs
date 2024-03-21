using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CoordinationFile:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message, ElementSet element)
        {
            try
            {
                Document doc = commandData.Application.ActiveUIDocument.Document;
                FilteredElementCollector collector=new FilteredElementCollector(doc);
                IList<Element> rvtLink= collector.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_RvtLinks).ToElements();
                //переборка коллекции
                foreach (Element element1 in rvtLink)
                {
                    using (Transaction t = new Transaction(doc, "Координация"))
                    {
                        t.Start();
                        if (element1.Name.Contains("Координационный"))
                        {
                            doc.AcquireCoordinates(element1.Id);
                            
                        }
                        //получение экземпляра связи
                        RevitLinkInstance revitLinkInstance=element1 as RevitLinkInstance;
                        FilteredElementCollector collectorElementInstance = new FilteredElementCollector(revitLinkInstance.GetLinkDocument());
                        ICollection<Element> linkGrids = collectorElementInstance.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Grids).ToElements();

                        foreach(Element gridCreate in linkGrids)
                        {
                            Grid linkGrid = gridCreate as Grid;
                            
                            Line lineLinkGrid = linkGrid.Curve as Line;
                            Grid newGrid=Grid.Create(doc, lineLinkGrid);
                            //Parameter nameGrid = newGrid.get_Parameter(BuiltInParameter.DATUM_TEXT);
                            newGrid.Name=linkGrid.Name;
                            
                        }

                        // работа с базовой точкой
                        BasePoint.GetSurveyPoint(doc).Clipped=false;

                        // работа с точкой съемки
                        Parameter projectSurveyPointNorthSouth =BasePoint.GetSurveyPoint(doc).get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM);
                        Parameter projectSurveyPointEastWest = BasePoint.GetSurveyPoint(doc).get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM);
                        Parameter projectSurveyPointElev=BasePoint.GetSurveyPoint(doc).get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
                        
                        projectSurveyPointNorthSouth.Set(BasePoint.GetProjectBasePoint(doc).get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble());
                        projectSurveyPointEastWest.Set(BasePoint.GetProjectBasePoint(doc).get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble());
                        projectSurveyPointElev.Set(BasePoint.GetProjectBasePoint(doc).get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble());

                        BasePoint.GetProjectBasePoint(doc).Pinned = true;
                        BasePoint.GetSurveyPoint(doc).Clipped = true;

                        foreach(Element element2 in linkGrids)
                        {
                            string info="\n"+">"+element2.Id.ToString();
                            TaskDialog.Show("id элементов", info);
                        }
                        //поиск сосей
                       
                        t.Commit();
                    }

                }

                
                
                              
                return Result.Succeeded;
            }
            catch 
            {
                message = "Не работает.";
                return Result.Cancelled;
            }
        }
    }
}
