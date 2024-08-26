using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BMCIModule.ViewModel;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using BMCIModule.Views.Window;
using BMCIModule.ViewModel.Base;


namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CoordinationFile:IExternalCommand
    {
        GetLinkForFilePreparingVM getLinkVM = new GetLinkForFilePreparingVM();
        GetLinkForFilePreparingWindow getLinkWindow = new GetLinkForFilePreparingWindow();
        public Result Execute(ExternalCommandData commandData,ref string message, ElementSet element)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            getLinkWindow.DataContext = getLinkVM;
            getLinkVM.Close = getLinkWindow.Close;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> rvtLink = collector.WhereElementIsNotElementType()
                .OfCategory(BuiltInCategory.OST_RvtLinks).ToElements();
            var list = rvtLink.Select(x => x.Name).ToList();
            getLinkVM.Links = rvtLink.Select(x =>(x as RevitLinkInstance).Name.Split(':').First()).ToList(); // убрал все что полсе двоеточия, но надо проверить для других файлов
            if (getLinkWindow.ShowDialog() == false)
            {
                return Result.Succeeded;
            }
            else
            {
                try
                {
                    
                //переборка коллекции
                if (rvtLink.Count == 0)
                {
                        message = "Нету связанных файлов. Добавьте файл для координации проекта.";
                        return Result.Cancelled;
                }
                else
                {
                        foreach (Element element1 in rvtLink)
                        {
                            using (Transaction t = new Transaction(doc, "Координация точек"))
                            {
                                t.Start();
                                Parameter projectBasePointNorthSouth = BasePoint.GetProjectBasePoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM);
                                Parameter projectBasePointEastWest = BasePoint.GetProjectBasePoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM);
                                Parameter projectBasePointElevation = BasePoint.GetProjectBasePoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
                                Parameter projectSurveyPointNorthSouth = BasePoint.GetSurveyPoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM);
                                Parameter projectSurveyPointEastWest = BasePoint.GetSurveyPoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM);
                                Parameter projectSurveyPointElev = BasePoint.GetSurveyPoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM);
                                if (element1.Name.Contains("Координационный"))
                                {
                                    if ((projectBasePointEastWest.AsDouble() == 0) &&
                                        (projectBasePointElevation.AsDouble() == 0) &&
                                        (projectBasePointNorthSouth.AsDouble() == 0)
                                        && (projectSurveyPointEastWest.AsDouble() == 0) &&
                                        (projectSurveyPointNorthSouth.AsDouble() == 0) &&
                                        (projectSurveyPointElev.AsDouble() == 0))
                                    {
                                        doc.AcquireCoordinates(element1.Id);
                                    }
                                    else
                                    {
                                        TaskDialog.Show("Координация точек проекта",
                                            "Базовая точка проекта или точка съемки не находятся в позиции 0.0.0");
                                        return Result.Cancelled;
                                    }
                                }
                                else
                                {
                                    throw new FileNotFoundException();
                                }

                                // работа с базовой точкой
                                BasePoint.GetSurveyPoint(doc).Clipped = false;

                                // работа с точкой съемки
                                projectSurveyPointNorthSouth.Set(BasePoint.GetProjectBasePoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_NORTHSOUTH_PARAM).AsDouble());
                                projectSurveyPointEastWest.Set(BasePoint.GetProjectBasePoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_EASTWEST_PARAM).AsDouble());
                                projectSurveyPointElev.Set(BasePoint.GetProjectBasePoint(doc)
                                    .get_Parameter(BuiltInParameter.BASEPOINT_ELEVATION_PARAM).AsDouble());

                                BasePoint.GetProjectBasePoint(doc).Pinned = true;
                                BasePoint.GetSurveyPoint(doc).Clipped = true;

                                t.Commit();
                            }
                        }
                }
                    return Result.Succeeded;
                }
                catch (FileNotFoundException)
                {
                    message = "Нету связанной модели с именем Координационный. Проверьте диспетчер связей";
                    return Result.Cancelled;
                }
            }
        }
    }
}
