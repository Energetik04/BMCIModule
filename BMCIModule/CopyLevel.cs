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
    public class CopyLevel:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string messgae, ElementSet elementSet)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            FilteredElementCollector collectorLevelProject = new FilteredElementCollector(doc);
            FilteredElementCollector collectorRvtLink=new FilteredElementCollector(doc);

            IList<Element> levelProject = collectorLevelProject.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Levels).ToElements();
            IList<Element> rvtLink = collectorRvtLink.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_RvtLinks).ToElements();

            foreach (Element element1 in rvtLink)
            {
                using (Transaction tCopyGrid = new Transaction(doc, "Копирование уровень"))
                {
                    tCopyGrid.Start();

                    RevitLinkInstance revitLinkInstance = element1 as RevitLinkInstance;
                    FilteredElementCollector collectorElementInstance = new FilteredElementCollector(revitLinkInstance.GetLinkDocument());
                    ICollection<Element> linkLevels = collectorElementInstance.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Levels).ToElements();
                    // Добавить выбор семейства для марки внизу
                    foreach (Element levelCreate in linkLevels)
                    {

                        Parameter elevation = (levelCreate as Level).get_Parameter(BuiltInParameter.LEVEL_ELEV);
                        Level newLevel=Level.Create(doc, elevation.AsDouble());
                        newLevel.Name = levelCreate.Name;
                        newLevel.Pinned= true;

                    }
                    tCopyGrid.Commit();
                }
            }
            return Result.Succeeded;
        }
    }
}
