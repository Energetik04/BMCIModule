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
            Dictionary<Face,List<XYZ>> faceList = new Dictionary<Face, List<XYZ>>();
            topFaces.Select(x =>
            {
                List<XYZ> collectionPoint = new List<XYZ>();
                var pointInFace = x.GetListPointInFace();
                var listCurveIntersectPoint= new List<XYZ>();
                var faceNormal = (x as PlanarFace).FaceNormal;
                int i = 0;
                pointInFace.Select(y =>
                {
                    XYZ pointPlane2 = new XYZ(y.X, y.Y, zMaxRange);
                    XYZ vectorToPlane =pointPlane2-y;
                    var scallarProduct = vectorToPlane.DotProduct(faceNormal);
                    if (scallarProduct < 0)
                    {
                        i++;
                        collectionPoint.Add(y);
                    }
                    //maxRangePlane.Project(y, out UV uvOut, out double distance);
                    //distancePointFaceInRefPlane.Add(distance);
                    return y;
                }).ToList();
                if(i!=0) faceList.Add(x,collectionPoint);
                return x;
            }).ToList();
            foreach (var directoryItem in faceList)
            {
                if (directoryItem.Value.Count == 1)
                {
                    maxRangePlane.Project(directoryItem.Value.First(),out UV outUV,out double distance);
                    XYZ projectPoint = maxRangePlane.Origin + outUV.U * maxRangePlane.XVec +
                                       outUV.V * maxRangePlane.YVec;
                    foreach (CurveLoop curveLoop in directoryItem.Key.GetEdgesAsCurveLoops())
                    {
                        foreach (Curve curve in curveLoop)
                        {
                            if (curve.GetEndPoint(0) == directoryItem.Value.First() ||
                                curve.GetEndPoint(1) == directoryItem.Value.First())
                            {
                                int curveEndPointForPlane = 0;
                                if (curve.GetEndPoint(0) == directoryItem.Value.First()) curveEndPointForPlane = 1;
                                Plane sidePlane=Plane.CreateByThreePoints(projectPoint,directoryItem.Value.First(),curve.GetEndPoint(curveEndPointForPlane));

                            }
                        }
                    }
                }
            }
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
