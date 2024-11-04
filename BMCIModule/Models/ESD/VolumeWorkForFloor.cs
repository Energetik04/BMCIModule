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
            double volume=0;
            double sumVolume = 0;
            double square = 0;
            //точки треугольника
            XYZ pointA, pointB, pointC;

            //список объемов
            List<double> listDouble = new List<double>();
            List<double> listSquare=new List<double>();
            int j = 0;
            foreach (var directoryItem in faceList)
            {
                if (directoryItem.Value.Count == 1)
                {
                    List<XYZ> collectionCrossPoint = new List<XYZ>();
                    maxRangePlane.Project(directoryItem.Value.First(),out UV outUV,out double distance);
                    XYZ projectPoint = maxRangePlane.Origin + outUV.U * maxRangePlane.XVec +
                                       outUV.V * maxRangePlane.YVec;
                    foreach (CurveLoop curveLoop in directoryItem.Key.GetEdgesAsCurveLoops())
                    {
                        foreach (Curve curve in curveLoop)
                        {
                            XYZ curveEndPoint0 = curve.GetEndPoint(0);
                            XYZ curveEndPoint1=curve.GetEndPoint(1);
                            if (curve.GetEndPoint(0).IsAlmostEqualTo(directoryItem.Value.First()) ||
                                curve.GetEndPoint(1) .IsAlmostEqualTo(directoryItem.Value.First()))
                            {
                                int curveEndPointForPlane = 0;
                                if (curve.GetEndPoint(0).IsAlmostEqualTo(directoryItem.Value.First())) curveEndPointForPlane = 1;
                                XYZ point2Curve = curve.GetEndPoint(curveEndPointForPlane);
                                XYZ crossPoint = GetCrossPoint(directoryItem.Value.First(), point2Curve, maxRangePlane);
                                collectionCrossPoint.Add(crossPoint);
                                //XYZ crossPoint1 = GetCrossPoint(directoryItem.Value.First(),
                                //    curve.GetEndPoint(curveEndPointForPlane), maxRangePlane);
                            }
                        }
                    }
                    collectionCrossPoint.Add(projectPoint);
                    
                    XYZ a = collectionCrossPoint[2] - collectionCrossPoint[1];
                    XYZ b = collectionCrossPoint[2] - collectionCrossPoint[0];
                    var vectorCross = a.CrossProduct(b);
                    var length = (directoryItem.Value.First() - projectPoint).GetLength();
                    volume =(Math.Abs(vectorCross.DotProduct(directoryItem.Value.First() - projectPoint)))/2;
                    listSquare.Add(Math.Abs(vectorCross.GetLength()/2));
                    sumVolume = sumVolume + volume;
                    square = square+vectorCross.GetLength()/2;
                    //Plane projectPlane = Plane.CreateByThreePoints(collectionCrossPoint[0], collectionCrossPoint[1], collectionCrossPoint[2]);
                }
                if (directoryItem.Value.Count == 3)
                {
                    List<XYZ> pointTriangle = directoryItem.Key.GetListPointInFace();
                    var maxZ = pointTriangle.OrderByDescending(x => x.Z).First();
                    var minZ = pointTriangle.OrderBy(x => x.Z).First();
                    //TODO приделать проекцию на maxRange,потом подумать над подъемом фигуры до максимальной точки и вытеснение остатка
                    pointA= pointTriangle[0];
                    pointB= pointTriangle[1];
                    pointC= pointTriangle[2];
                    XYZ AB = pointB - pointA;
                    XYZ AC = pointC - pointA;
                    var vectorCross= AB.CrossProduct(AC);

                    maxRangePlane.Project(pointA,out UV outUVA,out double distanceA);
                    maxRangePlane.Project(pointB,out UV outUVB,out double distanceB);
                    maxRangePlane.Project(pointC, out UV outUVC, out double distanceC);
                    maxRangePlane.Project(maxZ, out UV outUVmax, out double distanceMax);

                    XYZ projectPointA= maxRangePlane.Origin + outUVA.U * maxRangePlane.XVec +
                                       outUVA.V * maxRangePlane.YVec;
                    XYZ projectPointB = maxRangePlane.Origin + outUVB.U * maxRangePlane.XVec +
                                        outUVB.V * maxRangePlane.YVec;
                    XYZ projectPointC = maxRangePlane.Origin + outUVC.U * maxRangePlane.XVec +
                                        outUVC.V * maxRangePlane.YVec;
                    XYZ maxZProject = maxRangePlane.Origin + outUVmax.U * maxRangePlane.XVec +
                                      outUVmax.V * maxRangePlane.YVec;

                    XYZ ABProject = projectPointB - projectPointA;
                    XYZ ACProject = projectPointC - projectPointA;

                    var vectorCrossProject=ABProject.CrossProduct(ACProject);
                    //максимальный объем, осталось отнять экструдер пустой
                    var maxVolume=Math.Abs(vectorCrossProject.DotProduct(maxZ-maxZProject))/2;
                    //площадь треугольника
                    square = square + Math.Abs(vectorCross.GetLength() / 2);

                    var helps2 = maxZ - minZ;
                    //подумать над просчетом объема еще раз 
                    var volumeExtruder = Math.Abs(vectorCrossProject.DotProduct(helps2))/2;
                    //объем итоговый
                    sumVolume = sumVolume + (maxVolume - volumeExtruder);
                    listDouble.Add(maxVolume-volumeExtruder);
                    listSquare.Add(Math.Abs(vectorCross.GetLength() / 2));
                }
                if (directoryItem.Value.Count == 2)
                {
                    List<XYZ> pointTriangle = directoryItem.Key.GetListPointInFace();
                    List<XYZ> collectionCrossPoint = new List<XYZ>();
                    List<XYZ> poitAbove=new List<XYZ>();

                    foreach (XYZ pointFace in directoryItem.Value)
                    {
                        UV outUV;
                        double distance;
                        maxRangePlane.Project(pointFace,out outUV,out distance);
                        XYZ pointAboveMaxRange = maxRangePlane.Origin + outUV.U * maxRangePlane.XVec +
                                            outUV.V * maxRangePlane.YVec;
                        poitAbove.Add(pointAboveMaxRange);
                    }
                    foreach (CurveLoop curveLoop in directoryItem.Key.GetEdgesAsCurveLoops())
                    {
                        foreach (Curve curve in curveLoop)
                        {
                            XYZ curveEndPoint0 = curve.GetEndPoint(0);
                            XYZ curveEndPoint1 = curve.GetEndPoint(1);
                            if (curve.GetEndPoint(0).IsAlmostEqualTo(directoryItem.Value.First()) ||
                                curve.GetEndPoint(1).IsAlmostEqualTo(directoryItem.Value.First()))
                            {
                                int curveEndPointForPlane = 0;
                                if (curve.GetEndPoint(0).IsAlmostEqualTo(directoryItem.Value.First())) curveEndPointForPlane = 1;
                                XYZ point2Curve = curve.GetEndPoint(curveEndPointForPlane);
                                XYZ crossPoint = GetCrossPoint(directoryItem.Value.First(), point2Curve, maxRangePlane);
                                collectionCrossPoint.Add(crossPoint);
                            }
                        }
                    }

                    pointA = pointTriangle[0];
                    pointB = pointTriangle[1];
                    pointC = pointTriangle[2];

                    maxRangePlane.Project(pointA, out UV outUVA, out double distanceA);
                    maxRangePlane.Project(pointB, out UV outUVB, out double distanceB);
                    maxRangePlane.Project(pointC, out UV outUVC, out double distanceC);

                    XYZ projectPointA = maxRangePlane.Origin + outUVA.U * maxRangePlane.XVec +
                                        outUVA.V * maxRangePlane.YVec;
                    XYZ projectPointB = maxRangePlane.Origin + outUVB.U * maxRangePlane.XVec +
                                        outUVB.V * maxRangePlane.YVec;
                    XYZ projectPointC = maxRangePlane.Origin + outUVC.U * maxRangePlane.XVec +
                                        outUVC.V * maxRangePlane.YVec;
                    XYZ abovePointA = collectionCrossPoint[0];
                    XYZ abovePointB = collectionCrossPoint[1];
                    XYZ ABproject = projectPointB - projectPointA;
                    XYZ ACproject=projectPointC - projectPointA;
                    var squareProject = ABproject.CrossProduct(ACproject);

                }
            }
            // Значения конвертированные объема и площади
            double convertVolumeToMetricSystem=UnitUtils.Convert(sumVolume,UnitTypeId.CubicFeet,UnitTypeId.CubicMeters);
            double convertSquareToMetricSystem = UnitUtils.Convert(square, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters);


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

        public XYZ GetCrossPoint(XYZ endPointLine1,XYZ endPointLine2,Plane plane)
        {
            double x,y,z;
            double t;
            double x0,y0,z0;
            double a, b, c;
            x0=endPointLine1.X;
            y0=endPointLine1.Y;
            z0=endPointLine1.Z;
            XYZ directionVector = endPointLine2 - endPointLine1;
            a = directionVector.X;
            b = directionVector.Y;
            c = directionVector.Z;
            XYZ normalPlane= plane.Normal;
            XYZ origin= plane.Origin;
            double A,B,C,D;
            A = normalPlane.X;
            B = normalPlane.Y;
            C = normalPlane.Z;
            D = (-1) * (A * origin.X + B * origin.Y + C * origin.Z);
            t = (-1) * (A * x0 + B * y0 + C * z0 + D) / (a * A + B * b + C * c);
            x = x0 + a * t;
            y = y0 + b * t;
            z= z0 + c * t;
            return new XYZ(x, y, z);
        }

    }
}
