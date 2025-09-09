using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

namespace BMCIModule.Models.ETO
{
    [Transaction(TransactionMode.Manual)]
    public class SetLoadNameCurcuits:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var elementsCircuits = collector.WhereElementIsElementType().OfCategory(BuiltInCategory.OST_ElectricalCircuit).ToElements();
            Guid positionOnSchem = new Guid("95e5eb64-92e1-436b-80d8-f06505defc34");
            using (Transaction tr1 = new Transaction(doc, "Set Load Name"))
            {
                tr1.Start();
                foreach (var element in elementsCircuits)
                {
                    var getCurcuits=(element as MEPSystem) as ElectricalSystem;
                    var elementsElectricalEquipment=getCurcuits.Elements;
                    foreach (var elem in elementsElectricalEquipment)
                    {
                        getCurcuits.LoadName=(elem as FamilyInstance).get_Parameter(positionOnSchem).AsString();
                    }
                }
                tr1.Commit();
            }
            return Result.Succeeded;
        }
    }
}