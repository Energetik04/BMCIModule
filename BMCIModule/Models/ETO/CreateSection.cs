using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Models.ETO
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSection : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            
            FilteredElementCollector collectorInView=new FilteredElementCollector(doc,doc.ActiveView.Id);
            var cableTrays = collectorInView.OfCategory(BuiltInCategory.OST_CableTray).OfClass(typeof(CableTray)).ToList();

            throw new NotImplementedException();
        }
    }
}
