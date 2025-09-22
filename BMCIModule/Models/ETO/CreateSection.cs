using Autodesk.Revit.Attributes;
using Autodesk.Revit.Creation;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
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
            
            FilteredElementCollector collectorInView=new FilteredElementCollector(doc,doc.ActiveView.Id);
            var cableTrays = collectorInView.OfCategory(BuiltInCategory.OST_CableTray).OfClass(typeof(CableTray)).ToList();
            double X=0, Y=0, Z=0;
            
            foreach(var cableTray in cableTrays)
            {
                var centreSolidEnumerator = cableTray.get_Geometry(new Options());
                foreach(var centre in centreSolidEnumerator)
                {
                    if(centre is Solid)
                    {
                        var solid = centre as Solid;
                        X = solid.ComputeCentroid().X;
                        Y = solid.ComputeCentroid().Y;
                        Z = solid.ComputeCentroid().Z;
                    }
                }
                XYZ centeCableTray = new XYZ(X,Y,Z);
                XYZ xAxis = new XYZ(1, 0, 0);
                XYZ yAxis = new XYZ(0, 1, 0);
                Curve arcLine = Arc.Create(centeCableTray,0.16,0,3,xAxis,yAxis);
                using (Transaction tr1=new Transaction(doc,"jxrj"))
                {
                    //указать плоскость
                    tr1.Start();
                    DetailCurve arcDetail=doc.Create.NewDetailCurve(doc.ActiveView as ViewSection, arcLine as Arc) as DetailCurve;
                    tr1.Commit();
                }
            }
            return Result.Succeeded;
        }
    }
}
