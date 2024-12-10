using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
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
            minDeviation = 0.1;
            TaskDialog.Show("Конфигурация диапазона", "Верхняя плоскость диапазона:"+maxDeviation.ToString()+"\n"+ "Нижняя плоскость диапазона:"+ minDeviation.ToString());
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
            //высота уровня на который опирается пол 
            double elevationLevel = (levelFloor as Level).Elevation;
            //поднятие пола над уровнем
            double aboveLevel = (selectElement as Floor).get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM)
                .AsDouble();
            double zMaxRange = BasePoint.GetProjectBasePoint(doc).Position.Z+elevationLevel+aboveLevel+maxDeviation;
            double zMinRange = BasePoint.GetProjectBasePoint(doc).Position.Z + elevationLevel + aboveLevel -
                               minDeviation;

            XYZ poin1MaxRange = new XYZ(bbSelectElement.Min.X, bbSelectElement.Min.Y, zMaxRange);
            XYZ point2MaxRange = new XYZ(bbSelectElement.Max.X,bbSelectElement.Max.Y, zMaxRange);
            XYZ point3MaxRange = new XYZ(bbSelectElement.Min.X,bbSelectElement.Max.Y,zMaxRange);

            XYZ point1MinRange= new XYZ(bbSelectElement.Min.X, bbSelectElement.Min.Y, zMinRange);
            XYZ point2MinRange= new XYZ(bbSelectElement.Max.X, bbSelectElement.Max.Y, zMinRange);
            XYZ point3MinRange= new XYZ(bbSelectElement.Min.X, bbSelectElement.Max.Y, zMinRange);

            Plane maxRangePlane = Plane.CreateByThreePoints(poin1MaxRange, point2MaxRange, point3MaxRange);
            Plane minRangePlane = Plane.CreateByThreePoints(point1MinRange, point2MinRange, point3MinRange);


            Dictionary<Face,List<XYZ>> dictionaryFaceAndCollectionPointMaxRange = new Dictionary<Face, List<XYZ>>();
            Dictionary<Face, List<XYZ>> dictionaryFaceAndCollectionPointMinRange = new Dictionary<Face, List<XYZ>>();
            List<XYZ> point=new List<XYZ>();
            // скалярное произведение было как для наклонных плоскостей
            topFaces.Select(x =>
            {
                List<XYZ> collectionPointMaxRange = new List<XYZ>();
                List<XYZ> collectionPointMinRange=new List<XYZ>();
                var pointInFace = x.GetListPointInFace();
                // не помню почему именно нормаль плоскости брал
                var faceNormal = (x as PlanarFace).FaceNormal;
                int i = 0;
                double scallarProduct;
                pointInFace.Select(y =>
                {
                    point.Add(y);
                if ((y.Z <= maxRangePlane.Origin.Z) && (y.Z >= minRangePlane.Origin.Z))
                {
                    return y;
                }
                if (y.Z > maxRangePlane.Origin.Z)
                {
                    XYZ pointPlaneMaxRange = new XYZ(y.X, y.Y, zMaxRange);
                    XYZ vectorToPlaneMaxRange = pointPlaneMaxRange - y;
                    scallarProduct = vectorToPlaneMaxRange.DotProduct(maxRangePlane.Normal);
                    if (scallarProduct < 0)
                    {
                        //i++;
                        collectionPointMaxRange.Add(y);
                    }
                }
                if (y.Z < minRangePlane.Origin.Z)
                {
                    XYZ pointPlaneMinRange = new XYZ(y.X, y.Y, zMinRange);
                    XYZ vectorToPlaneMinRange = pointPlaneMinRange - y;
                    scallarProduct = vectorToPlaneMinRange.DotProduct(minRangePlane.Normal);
                    if (scallarProduct > 0)
                    {
                        //i++;
                        collectionPointMinRange.Add(y);
                    }
                }
                return y;
                }).ToList();
                if(collectionPointMaxRange.Count!=0) dictionaryFaceAndCollectionPointMaxRange.Add(x,collectionPointMaxRange);
                if(collectionPointMinRange.Count!=0) dictionaryFaceAndCollectionPointMinRange.Add(x,collectionPointMinRange);
                return x;
            }).ToList();
            var minPoint=point.OrderBy(x => x.Z).First();
            double volume=0;
            double sumVolume = 0;
            double square = 0;
            //точки треугольника
            XYZ pointA, pointB, pointC;

            //список объемов
            List<double> listDouble = new List<double>();
            List<double> listSquare=new List<double>();
            int j = 0;
            foreach (var directoryItem in dictionaryFaceAndCollectionPointMaxRange)
            {
                if (listDouble.Count == 122)
                {

                }
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
                                XYZ curveEndPoint1 = curve.GetEndPoint(1);
                                if (curveEndPoint0.IsAlmostEqualTo(directoryItem.Value.First(), 0.000001) ||
                                    curveEndPoint1.IsAlmostEqualTo(directoryItem.Value.First(), 0.000001))
                                {
                                    maxRangePlane.Project(directoryItem.Value.First(), out UV outUVcoord, out double distanceOUT);
                                    XYZ projectPointInMaxRange = maxRangePlane.Origin + outUVcoord.U * maxRangePlane.XVec +
                                                                 outUVcoord.V * maxRangePlane.YVec;
                                    XYZ crossPoint = GetCrossPoint(curveEndPoint0, curveEndPoint1, maxRangePlane);
                                    collectionCrossPoint.Add(crossPoint);
                                }
                            }
                        }
                    collectionCrossPoint.Add(projectPoint);
                    //ошибка количества точек пересечения
                    XYZ a = collectionCrossPoint[0] - collectionCrossPoint[2];
                    XYZ b = collectionCrossPoint[1] - collectionCrossPoint[2];
                    var vectorCross = a.CrossProduct(b);
                    var length = (directoryItem.Value.First() - projectPoint).GetLength();
                    var vectorHight = projectPoint - directoryItem.Value.First();
                    volume =(Math.Abs(vectorCross.DotProduct(vectorHight)))/6;
                    sumVolume = sumVolume + volume;
                    square = square+ Math.Abs(vectorCross.GetLength() / 2);
                    listSquare.Add(Math.Abs(vectorCross.GetLength() / 2));
                    listDouble.Add(volume);
                    //Plane projectPlane = Plane.CreateByThreePoints(collectionCrossPoint[0], collectionCrossPoint[1], collectionCrossPoint[2]);
                }
                if (directoryItem.Value.Count == 3)
                {
                    //TODO проверить объем пирамиды с 2 точками
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
                    //точки грани
                    List<XYZ> pointTriangle = directoryItem.Key.GetListPointInFace();
                    List<XYZ> collectionCrossPoint = new List<XYZ>();
                    List<XYZ> pointAboveCollection=new List<XYZ>();
                    List<XYZ> pointProjectCollection=new List<XYZ>();
                    // кажется понял, будем через поиск кривой с этими двумя точками и остальных отрезков, точки брать по очерез из этого отрезка
                    foreach (var pointInCollection in directoryItem.Value)
                    {
                        foreach (CurveLoop curveLoop in directoryItem.Key.GetEdgesAsCurveLoops())
                        {
                            foreach (Curve curve in curveLoop)
                            {
                                XYZ curveEndPoint0 = curve.GetEndPoint(0);
                                XYZ curveEndPoint1 = curve.GetEndPoint(1);
                                if ((curveEndPoint0.IsAlmostEqualTo(directoryItem.Value.First()) &&
                                    curveEndPoint1.IsAlmostEqualTo(directoryItem.Value.Last()))|| (curveEndPoint0.IsAlmostEqualTo(directoryItem.Value.Last()) &&
                                    curveEndPoint1.IsAlmostEqualTo(directoryItem.Value.First())) )
                                {
                                    continue;
                                }
                                else
                                {
                                    if (pointInCollection.IsAlmostEqualTo(curveEndPoint0) ||
                                        pointInCollection.IsAlmostEqualTo(curveEndPoint1))
                                    {
                                        maxRangePlane.Project(pointInCollection,out UV outUVcoord, out double distance);
                                        XYZ projectPointInMaxRange= maxRangePlane.Origin + outUVcoord.U * maxRangePlane.XVec +
                                                                    outUVcoord.V * maxRangePlane.YVec;
                                        XYZ crossPoint = GetCrossPoint(curveEndPoint0, curveEndPoint1, maxRangePlane);
                                        pointProjectCollection.Add(projectPointInMaxRange);
                                        pointAboveCollection.Add(crossPoint);
                                    }
                                }
                            }
                        }
                    }
                    //теперь нужно на пирамиды разбить
                    double area1 = GetTheAreaOfThePyramid(pointAboveCollection.First(), pointProjectCollection.First(),
                        pointAboveCollection.Last(), directoryItem.Value.First());
                    double area2 = GetTheAreaOfThePyramid(directoryItem.Value.First(), directoryItem.Value.Last(),
                        pointProjectCollection.Last(), pointAboveCollection.Last());
                    double area3 = GetTheAreaOfThePyramid(pointAboveCollection.Last(), pointProjectCollection.First(),
                        pointProjectCollection.Last(), directoryItem.Value.First());
                    
                    double square1 = GetTheSquareOfTopFace(directoryItem.Value.First(), pointAboveCollection.First(),
                        pointAboveCollection.Last());
                    double square2 = GetTheSquareOfTopFace(directoryItem.Value.First(), directoryItem.Value.Last(),
                        pointAboveCollection.Last());
                    sumVolume = sumVolume + area1 + area2 + area3;
                    square =square+square1+square2;
                    listDouble.Add(area1+area2+area3);
                    listSquare.Add(square1+square2);
                }
            }
            var summListDouble = listDouble.Sum();
            var summListSquare= listSquare.Sum();
            // Значения конвертированные объема и площади
            double convertVolumeToMetricSystem=UnitUtils.Convert(sumVolume,UnitTypeId.CubicFeet,UnitTypeId.CubicMeters);
            double convertSquareToMetricSystem = UnitUtils.Convert(square, UnitTypeId.SquareFeet, UnitTypeId.SquareMeters);
            TaskDialog.Show("Объем и площадь","Объем за верхней границей:"+convertVolumeToMetricSystem.ToString()+"\n"+"Площадь за верхней гранью"+convertSquareToMetricSystem);
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
                if (face is PlanarFace)
                {
                    if ((face as PlanarFace).FaceNormal.Z > nullVector.Z) { topFace.Add(face); }
                }
                //строковое сравнение векторов(надо немного продумать)
            }
            return topFace;
        }

        public double GetTheSquareOfTopFace(XYZ pointA, XYZ pointB, XYZ pointC)
        {
            XYZ BA = pointA - pointB;
            XYZ BC = pointC - pointB;
            var square = BA.CrossProduct(BC);
            return Math.Abs(square.GetLength() / 2);
        }
        //point B откуда уходит вектор высоты
        public double GetTheAreaOfThePyramid(XYZ pointA, XYZ pointB, XYZ pointC, XYZ pointHight)
        {
            XYZ BA = pointA - pointB;
            XYZ BC = pointC - pointB;
            XYZ hight= pointHight - pointB;

            var square = BA.CrossProduct(BC);
            var area = square.DotProduct(hight);
            return Math.Abs(area/2);
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
