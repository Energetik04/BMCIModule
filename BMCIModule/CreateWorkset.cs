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
    delegate Workset SearchWorkset(IList<Workset> setWorkset,Document document);
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

        public static Workset GetWorkset(IList<Workset> setWorkset, Document doc)
        {
            string worksetLevelAndGridName = "#BMCI - ОБЩИЕ УРОВНИ И СЕТКИ";
            bool flag = false;
            Workset worksetLevelAndGrid;
            foreach (Workset workset in setWorkset)
            {
                if (workset.Name == worksetLevelAndGridName)
                {
                    flag = true;
                    return worksetLevelAndGrid = workset;
                }
            
            }
            return worksetLevelAndGrid = Workset.Create(doc, worksetLevelAndGridName);
                                  
        }

    }
}
