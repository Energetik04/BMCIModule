using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Private.InfoCenter;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace BMCIModule.Models.ETO
{
    [Transaction(TransactionMode.Manual)]
    public class GetPathForCircuit : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            
            FilteredElementCollector collector=new FilteredElementCollector(doc);
            //все цепи проекта
            IList<Element> collectionEC=collector.OfCategory(BuiltInCategory.OST_ElectricalCircuit).ToElements();

            //все короба
            FilteredElementCollector collectorCT=new FilteredElementCollector(doc);
            var collectionCT=collectorCT.OfCategory(BuiltInCategory.OST_CableTray).ToElements();
            



            throw new NotImplementedException();
        }
    }
}
