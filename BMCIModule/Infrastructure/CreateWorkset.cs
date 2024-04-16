using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BMCIModule.Department;
using BMCIModule.ViewModel;
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

            FilteredWorksetCollector collectorWorkset=new FilteredWorksetCollector(doc);
            //Фильтрация рабочих наборов
            WorksetKindFilter setFilter = new WorksetKindFilter(WorksetKind.UserWorkset);
            IList<Workset> setWorkset=collectorWorkset.WherePasses(setFilter).ToWorksets();

            SelectWorkset viewModel = new SelectWorkset();

            //SetDepartmentBMCI setDepartmentBMCI = new SetDepartmentBMCI();
            //setDepartmentBMCI.CreateSetMark();
            //SelectWorkset selectWorkset = new SelectWorkset(setDepartmentBMCI.ETO.ListMark);
            viewModel.ShowDialog();

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
