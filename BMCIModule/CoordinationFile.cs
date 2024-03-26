using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;


namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CoordinationFile:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message, ElementSet element)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            try
            {
                
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
                        else
                        {
                            throw new FileNotFoundException();
                            
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
                                                                     
                        t.Commit();
                    }

                }
                                             
                return Result.Succeeded;
            }
            catch (FileNotFoundException)
            {
                message = "Нету связанной модели с именем Координационный или связь выгружена. Проверьте диспетчер связей";
                return Result.Cancelled;
            }
            catch
            {
                message = "Нету связанных файлов. Добавьте файл для координации проекта.";
                return Result.Cancelled;
            }
            

        }
    }
}
