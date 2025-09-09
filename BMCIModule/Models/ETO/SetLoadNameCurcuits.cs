using System;
using System.Linq;
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

            var elementsEquipment = collector.OfCategory(BuiltInCategory.OST_ElectricalEquipment).OfClass(typeof(FamilyInstance)).ToElements();
            Guid positionOnSchem = new Guid("95e5eb64-92e1-436b-80d8-f06505defc34");
            using (Transaction tr1 = new Transaction(doc, "Set Load Name"))
            {
                tr1.Start();
                foreach (var element in elementsEquipment)
                {
                    var familyInstance = element as FamilyInstance;
                        if (familyInstance != null)
                        {
                            if ((element as FamilyInstance).MEPModel.GetAssignedElectricalSystems() != null)
                            {
                                var getCurcuits = (element as FamilyInstance).MEPModel.GetAssignedElectricalSystems();

                                foreach (var elem in getCurcuits)
                                {
                                    var elementC = elem.Elements;
                                    foreach (var ele in elementC)
                                    {
                                        elem.LoadName = (ele as FamilyInstance).get_Parameter(positionOnSchem).AsString();
                                    }

                                }
                            }
                        }
                    }
                           
                tr1.Commit();
            }
            TaskDialog.Show("Цепи", "Цепи переименованы");
            return Result.Succeeded;
        }
    }
}