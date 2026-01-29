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
    public class GetFireLoad : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var electricalSystem = collector.OfCategory(BuiltInCategory.OST_ElectricalCircuit).OfType<ElectricalSystem>().ToList();
            FilteredElementCollector collectorSpace=new FilteredElementCollector(doc);
            var spaceList=collectorSpace.OfCategory(BuiltInCategory.OST_MEPSpaces).OfType<Space>().ToList();
            Options op = new Options()
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true,
                DetailLevel = ViewDetailLevel.Fine,
            };
            SolidCurveIntersectionOptions intersectionOptions= new SolidCurveIntersectionOptions();
            foreach (var space in spaceList)
            {
                Solid solidSpace = space.get_Geometry(op).First() as Solid;
                foreach(var elSys in electricalSystem)
                {
                    var listXYZ = elSys.GetCircuitPath();
                    double length = 0;
                    for (int i=0; i<listXYZ.Count()-1; i++)
                    {
                        Curve curve = Line.CreateBound(listXYZ[i], listXYZ[i+1]);
                        
                        SolidCurveIntersection intersection = solidSpace.IntersectWithCurve(curve, intersectionOptions);
                        
                        for (int segment=0; segment <= intersection.SegmentCount - 1; segment++)
                        {
                            Curve curveInside=intersection.GetCurveSegment(segment);
                            length=curveInside.Length + length;
                            
                        }
                        
                    }
                    if (length != 0)
                    {
                        using (Transaction tr = new Transaction(doc, "длина"))
                        {
                            tr.Start();
                            elSys.GetParameters("Комментарии").First().Set(length.ToString());
                            tr.Commit();
                        }
                    }
                }
            }

            return Result.Succeeded;

        }
    }
}
