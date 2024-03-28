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
    public class CreateWorkset:IExternalCommand
    {
        public Result Execute (ExternalCommandData commanddata, ref  string message,ElementSet elements)
        {
            Document doc = commanddata.Application.ActiveUIDocument.Document;

            //FilteredElementCollector collector=new FilteredElementCollector(doc);
            //посмотреть как получить набор рабочих наборов в проекте
            FilteredWorksetCollector collectorWorkset=new FilteredWorksetCollector(doc);
            //Фильтрация рабочих наборов
            WorksetKindFilter setFilter = new WorksetKindFilter(WorksetKind.UserWorkset);
            IList<Workset> setWorkset=collectorWorkset.WherePasses(setFilter).ToWorksets();

            
            
            // марки проктов электриков
            List<string> tagProjectETO = new List<string>();
            string tagProjectETOName = "ЭМ,ЭО,ЭГ,ЭС,ЭК,ЭП";
            string[] newTagProjectETOName = tagProjectETOName.Split(',').ToArray();
            for (int i = 0; i < newTagProjectETOName.Length; i++)
            {
                tagProjectETO.Add(newTagProjectETOName[i]);
            }
            SelectWorkset selectWorkset = new SelectWorkset(tagProjectETO);
            selectWorkset.ShowDialog();

            return Result.Succeeded;
        }
    }
}
