using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xaml;

namespace BMCIModule.Models.ETO
{
    [Transaction(TransactionMode.Manual)]
    public class CreateSection : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var app = uiapp.Application;
            var doc = uidoc.Document;

            FilteredElementCollector collectorInView = new FilteredElementCollector(doc, doc.ActiveView.Id);
            var cableTrays = collectorInView.OfCategory(BuiltInCategory.OST_CableTray).OfClass(typeof(CableTray)).ToList();
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var electricalSystem = collector.OfCategory(BuiltInCategory.OST_ElectricalCircuit).OfType<ElectricalSystem>().ToList();

            double X = 0, Y = 0, Z = 0;

            Guid guidCL = new Guid("3c465675-d0fa-4d5f-a997-77e21d61ca4d");// Кабельная трасса

            foreach (var cableTray in cableTrays)
            {
                var centreSolidEnumerator = cableTray.get_Geometry(new Options());
                foreach (var centre in centreSolidEnumerator)
                {
                    if (centre is Solid)
                    {
                        var solid = centre as Solid;
                        X = solid.ComputeCentroid().X;
                        Y = solid.ComputeCentroid().Y;
                        Z = solid.ComputeCentroid().Z;
                    }
                }

                string lineArray = cableTray.get_Parameter(guidCL).AsString();
                if (lineArray==null) continue;
                string[] line = lineArray.Split(',');

                XYZ centeCableTray = new XYZ(X, Y, Z);

                XYZ xAxis = doc.ActiveView.RightDirection;
                XYZ yAxis = doc.ActiveView.UpDirection;
                
                Arc arcLine = null;


                using (Transaction tr1 = new Transaction(doc, "Раскладка кабеля"))
                {
                    //указать плоскость
                    tr1.Start();
                    foreach (var lineEs in line)
                    {
                        foreach (ElectricalSystem es in electricalSystem)
                        {
                            if (es.LoadName == lineEs)
                            {
                                double d = es.GetParameters("Dк").First().AsDouble();
                                arcLine = Arc.Create(centeCableTray, d / 304.8/2, 0, 2 * Math.PI, xAxis, yAxis);
                                DetailArc arcDetail = doc.Create.NewDetailCurve(doc.ActiveView, arcLine) as DetailArc;
                                if(xAxis.X<0 || xAxis.Y<0 || xAxis.Z<0)
                                {
                                    X = centeCableTray.X - xAxis.Multiply(d / 304.8 / 2 * 3.5).X;
                                }
                                else
                                {
                                    X = centeCableTray.X + d / 304.8 / 2 * 3.5;
                                }

                                centeCableTray = new XYZ(X, Y, Z);
                                break;
                            }
                        }
                    }
                    tr1.Commit();
                }
            }
            return Result.Succeeded;
        }
    }
}
