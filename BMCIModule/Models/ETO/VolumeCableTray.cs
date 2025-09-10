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
    public class VolumeCableTray : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var electricalSystem = collector.OfCategory(BuiltInCategory.OST_ElectricalCircuit).OfType<ElectricalSystem>().ToList();

            FilteredElementCollector collectorCableTray=new FilteredElementCollector(doc);
            var cableTrays=collectorCableTray.OfCategory(BuiltInCategory.OST_CableTray).OfClass(typeof(CableTray)).ToList();

            Guid guidCL = new Guid("3c465675-d0fa-4d5f-a997-77e21d61ca4d");
            Guid guidValCable = new Guid("02605d3c-419c-4860-8d2c-04d6a9d4af2f");

            using (Transaction tr1=new Transaction(doc,"Подсчет объема заполнения"))
            {
                tr1.Start();
                foreach (CableTray cableTray in cableTrays)
                {
                    double volumeCable = 0;
                    string line = null;
                    if (cableTray.get_Parameter(guidCL).AsString() != null)
                    {
                        line = cableTray.get_Parameter(guidCL).AsString();
                        string[] lineES = line.Split(new char[] { ',' });
                        double volume = cableTray.Height * cableTray.Width * Math.Pow(304.8,2);
                        for (int i = 0; i < lineES.Length; i++)
                        {
                            foreach (ElectricalSystem es in electricalSystem)
                            {
                                if (es.LoadName == lineES[i])
                                {
                                    foreach (Parameter parameter in es.Parameters)
                                    {
                                        ElementId elementId = new ElementId(4459288);
                                        if (parameter.Id == elementId)
                                        {
                                            volumeCable = volumeCable + Math.PI * Math.Pow(parameter.AsDouble(), 2) / 4*es.get_Parameter(guidValCable).AsInteger();
                                            break;
                                        }
                                    }
                                    break;
                                }
                            }
                        }
                        if (volumeCable != 0)
                        {
                            double procent = volumeCable/volume*100;
                            Guid cableSquare = new Guid("b387334b-95f4-475a-8fd5-3ffd56556f2f");
                            Guid cableProcent = new Guid("8e6494c3-aac9-4faa-b3ca-7950322ca990");
                            cableTray.get_Parameter(cableSquare).Set(volumeCable);
                            cableTray.get_Parameter(cableProcent).Set(procent);
                        }

                    }

                }
                tr1.Commit();
            }
                
            return Result.Succeeded;
        }
    }
}
