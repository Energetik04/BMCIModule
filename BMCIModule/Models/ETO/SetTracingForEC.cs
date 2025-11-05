using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Models.ETO
{
    [Transaction(TransactionMode.Manual)]
    public class SetTracingForEC : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var electricalSystem = collector.OfCategory(BuiltInCategory.OST_ElectricalCircuit).OfType<MechanicalEquipment>().ToList();



            throw new NotImplementedException();
        }
    }
}
