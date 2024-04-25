using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BMCIModule.Department;
using BMCIModule.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule
{
    delegate Workset SearchWorkset(IList<Workset> setWorkset,Document document);
    [Transaction(TransactionMode.Manual)]
    //[RegenerationAttribute(RegenerationOption.Manual)]
        
    public class CreateWorkset:IExternalCommand
    {
        static SelectWorksetViewModel SelectWorksetViewModel = new SelectWorksetViewModel();
        SelectWorksetWindow viewModel = new SelectWorksetWindow();
               
        public Result Execute (ExternalCommandData commanddata, ref  string message,ElementSet elements)
        {
            // Создание класса окна для Создания рабочих наборов
            viewModel.DataContext = SelectWorksetViewModel;// не понятен для чего датаконтекст
            SelectWorksetViewModel.Close = viewModel.Close;


            Document doc = commanddata.Application.ActiveUIDocument.Document;

            FilteredWorksetCollector collectorWorkset=new FilteredWorksetCollector(doc);
            //Фильтрация рабочих наборов
            WorksetKindFilter setFilter = new WorksetKindFilter(WorksetKind.UserWorkset);
            IList<Workset> setWorkset=collectorWorkset.WherePasses(setFilter).ToWorksets();
            
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
