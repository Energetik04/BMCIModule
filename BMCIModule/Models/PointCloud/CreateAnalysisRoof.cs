using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CreateAnalysisRoof:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elments)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            double stepMin, captureRange;
            int numberPoint;

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите поверхности (ЗдфтуPlaneпол/крыша)");
            Selection choiceElement = uidoc.Selection;
            Reference choiceElementRef = choiceElement.PickObject(ObjectType.Element, "Выбор элемента(пол / перекрытие)");
            Element selectElement = doc.GetElement(choiceElementRef);
            ElementId selectElementId = selectElement.Id;

            #endregion

            #region SelectPointCloud

            TaskDialog.Show("Выбор элемента", "Выберите облако точек для построения пола");
            Selection choicePointCloud = uidoc.Selection;
            Reference choicePointCloudRef = choicePointCloud.PickObject(ObjectType.Element, "Выбор элемента");
            Element selectPointCloud = doc.GetElement(choicePointCloudRef);

            #endregion

            Options op = new Options()
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects  = true,
                DetailLevel = ViewDetailLevel.Fine,
            };
            List<Face> faceList=new List<Face>();
            //List<Face> cylindrical
            FaceArray faceArray = ((selectElement as RoofBase).get_Geometry(op).First() as Solid).Faces;
            foreach (Face face in faceArray)
            {
                if (face is CylindricalFace)
                {
                    var h=face as CylindricalFace;
                    h.GetBoundingBox();
                    var max = h.GetBoundingBox().Max;
                    var min = h.GetBoundingBox().Min;
                }
                faceList.Add(face);
            }
            var topFaceList=new List<Face>();

            var planarFace = faceList.Where(x => (x is PlanarFace)).Where(x => (x as PlanarFace).FaceNormal.Z > 0)
                .Select(x => x).Cast<Face>().ToList();
            var cylindricalFace = faceList.Where(x => x is CylindricalFace).Select(x => (x as CylindricalFace))
                .Where(x=>x.Axis.X>=0 && x.Axis.Y>=0 && x.Axis.Z>=0).Cast<Face>().ToList();
             topFaceList.AddRange(planarFace);
             topFaceList.AddRange(cylindricalFace);

            return Result.Succeeded;
        }
    }

}
