using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BMCIModule.Models.Extension;

namespace BMCIModule.Models.ESD
{
    [Transaction((TransactionMode.Manual))]
    public class VolumeWorkForFloor:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            double maxDeviation,minDeviation;
            maxDeviation =0.1;
            minDeviation = 0.0;

            #region Получение документа и интерфейса

            var app = commandData.Application;// пользовательский интерфейс
            var uidoc = app.ActiveUIDocument;// активный документ
            var doc = uidoc.Document;// сам документ

            #endregion


            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите поверхности (пол/крыша)");
            Selection choiceElement = uidoc.Selection;
            Reference choiceFloorRef = choiceElement.PickObject(ObjectType.Element, "Выбор элемента(пол / перекрытие)");
            Element selectElement = doc.GetElement(choiceFloorRef);
            ElementId selectElementId = selectElement.Id;

            #endregion

            List<Face> topFaces = GetTopFace(selectElement, doc);
            BoundingBoxXYZ bbSelectElement = selectElement.get_BoundingBox(doc.ActiveView);
            Element levelFloor =doc.GetElement(selectElement.LevelId);
            double elevationLevel = (levelFloor as Level).Elevation;
            double aboveLevel = (selectElement as Floor).get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)
                .AsDouble();
            double zMaxRange = BasePoint.GetProjectBasePoint(doc).Position.Z+elevationLevel+aboveLevel+maxDeviation;

            XYZ point1 = new XYZ(bbSelectElement.Min.X, bbSelectElement.Min.Y, zMaxRange);
            XYZ point2 = new XYZ(bbSelectElement.Max.X,bbSelectElement.Max.Y, zMaxRange);
            XYZ point3 = new XYZ(bbSelectElement.Min.X,bbSelectElement.Max.Y,zMaxRange);

            Plane maxRangePlane = Plane.CreateByThreePoints(point1, point2, point3);
            Surface maxRangeSurface=maxRangePlane;

            List<double> distancePointFaceInRefPlane= new List<double>();

            topFaces.Select(x =>
            {
                var pointInFace = x.GetListPointInFace();
                var faceNormal = (x as PlanarFace).FaceNormal;
                pointInFace.Select(y =>
                {
                    XYZ pointPlane2 = new XYZ(y.X, y.Y, zMaxRange);
                    XYZ vectorToPlane =pointPlane2-y;
                    distancePointFaceInRefPlane.Add(vectorToPlane.DotProduct(faceNormal));
                    //maxRangePlane.Project(y, out UV uvOut, out double distance);
                    //distancePointFaceInRefPlane.Add(distance);
                    return y;
                }).ToList();
                return x;
            }).ToList();
            var listNegative=distancePointFaceInRefPlane.Where(x => x < 0).ToList();
            return Result.Succeeded;
        }

        public List<Face> GetTopFace(Element selectElement, Document doc)
        {
            #region Options

            Options op = doc.Application.Create.NewGeometryOptions();// Определить Options для объекта заредачить
            op.ComputeReferences = true;
            op.DetailLevel = ViewDetailLevel.Undefined;
            op.IncludeNonVisibleObjects = false;

            #endregion

            GeometryElement getGeometryElement = selectElement.get_Geometry(op);
            List<GeometryObject> list = new List<GeometryObject>();
            foreach (GeometryObject geometryObject in getGeometryElement)// вытянул перечислением фигуры которые внутри
            {
                if (geometryObject is Solid) list.Add(geometryObject);// потом подумать и разобраться с количеством
            }
            // модумать над сложными полами и переборке этих полов 
            Solid solid = list[0] as Solid;
            FaceArray faceSolid = solid.Faces;
            List<Face> topFace = new List<Face>();
            XYZ nullVector = new XYZ(0.000000000, 0.000000000, 0.000000000); // с вектором 1 , для того чтобы вытащить парраллельно оси Z 
            foreach (Face face in faceSolid)
            {
                //строковое сравнение векторов(надо немного продумать)
                if ((face as PlanarFace).FaceNormal.Z>nullVector.Z) { topFace.Add(face); }
            }
            return topFace;
        }

    }
}
