
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BMCIModule.Models.Extension;
using UIFramework.Widget.CustomControls;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;
using POINT = BMCIModule.Models.PointCloud;
using BMCIModule.Models;
using System.Drawing.Drawing2D;
using System.Drawing;

namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class СreateAnalysisFloor:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region Поулчение документа и интерфейса

            var app = commandData.Application;// пользовательский интерфейс
            var uidoc = app.ActiveUIDocument;// активный документ
            var doc = uidoc.Document;// сам документ
            double stepMin, captureRange;
            stepMin = 1;
            captureRange = 0.5;
            #endregion


            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите поверхности (пол/крыша)");
            Selection choiceElement = uidoc.Selection;
            Reference choiceFloorRef = choiceElement.PickObject(ObjectType.Element, "Выбор элемента(пол / перекрытие)");
            Element selectElement = doc.GetElement(choiceFloorRef);
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
                ComputeReferences = true
            };
            // верхняя грань
            Face topFace = null;
            List<Face> topFaces = ((doc.GetElement(selectElementId) as Floor).get_Geometry(op).First() as Solid).GetTopFace();
            if (topFaces.Count == 1) topFace = topFaces.First();

            ICollection<POINT.Point> pointRevit = GeneratePointCollection(ref topFace, ref stepMin);
            ICollection<POINT.Point> collection = IsInFigureWithBordersFace(ref pointRevit,ref topFace, ref stepMin);
            MovieOutFigurePointInBorders(ref collection, ref topFace,ref stepMin);

            var listPointWithoutDeletePoint
                = collection.Where(x => x.getPointPosition != PointInFigureWithBorders.deleteThisPoint)
                            .ToList();

            #region Триангуляция поверхности
            double hight = (Math.Sqrt(3)) * stepMin / 2.0d;



            #endregion
            //найти минимум и максимум
            SlabShapeVertexArray vertexArray = null;
            if (selectElement is Floor)
            {
                vertexArray = (selectElement as Floor).SlabShapeEditor.SlabShapeVertices;
            }
            List<SlabShapeVertex> vertexList = new List<SlabShapeVertex>();
            List<XYZ> listVertexPosition = new List<XYZ>();
            if (vertexArray != null)
            {
                foreach (SlabShapeVertex vertex in vertexArray)
                {
                    vertexList.Add(vertex);
                    listVertexPosition.Add(vertex.Position);
                }
            }
            SlabShapeVertex vertexMin = null;
            SlabShapeVertex vertexMax = null;
            if (vertexList.Count != 0)
            {
                vertexMin = vertexList.Where(x => x.VertexType == SlabShapeVertexType.Interior)
                    .OrderBy(x => x.Position.Z).Select(x => x).First();
                vertexMax = vertexList.Where(x => x.VertexType == SlabShapeVertexType.Interior)
                    .OrderByDescending(x => x.Position.Z).Select(x => x).First();
            }
            // на основе чего размещен пол (величина от базовой точки проекта)
            double elevationElement = (doc.GetElement(selectElement.LevelId) as Level).Elevation;
            //cмещение от уровня
            double heigtAboveLevel = selectElement.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).AsDouble();
            //var vertexMin = listXYZTriangle.OrderBy(x => x.Z).First();
            //var vertexMax = listXYZTriangle.OrderByDescending(x => x.Z).First();


            
            
            //Создание типа легенды и стиля отображения
            using (Transaction tr = new Transaction(doc, "Создание легенды и типа результатов анализа"))
            {
                tr.Start();
                AnalysisDisplayStyle analysisDisplay = null;
                AnalysisDisplayStyle analysisDisplayHide = null;
                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                ICollection<Element> collection1 = collector1.OfClass(typeof(AnalysisDisplayStyle)).ToElements();
                var displayStyle = collection1.Where(x => x.Name == "Фактические конструкции").Select(x => x);
                if (displayStyle.Count() == 0)
                {
                    AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings =
                        new AnalysisDisplayColoredSurfaceSettings();
                   
                    AnalysisDisplayColorSettings coloreSettings = new AnalysisDisplayColorSettings();
                    Autodesk.Revit.DB.Color red = new Autodesk.Revit.DB.Color(255, 0, 0);
                    Autodesk.Revit.DB.Color blue = new Autodesk.Revit.DB.Color(0, 0, 255);
                    AnalysisDisplayColorEntry colorIntermediate=new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0,255,0));
                    IList<AnalysisDisplayColorEntry> colorEnties = new List<AnalysisDisplayColorEntry>()
                        { colorIntermediate };
                    coloreSettings.SetIntermediateColors(colorEnties);

                    coloredSurfaceSettings.ShowContourLines = false;
                    coloredSurfaceSettings.ShowGridLines = false;
                    coloredSurfaceSettings.Transparency = 0;

                    coloreSettings.MaxColor = red;
                    coloreSettings.MinColor = blue;
                    //настройка легенды
                    AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings();
                    legendSettings.NumberOfSteps = 10;//количество шагов в легенде
                    legendSettings.Rounding = 0.05;
                    legendSettings.ShowDataDescription = false;
                    legendSettings.ShowLegend = true;

                    AnalysisDisplayLegendSettings legendSettingsHide = new AnalysisDisplayLegendSettings();
                    legendSettingsHide.NumberOfSteps = 10;//количество шагов в легенде
                    legendSettingsHide.Rounding = 0.05;
                    legendSettingsHide.ShowDataDescription = false;
                    legendSettingsHide.ShowLegend = false;

                    //непонятно зачем эта часть
                    FilteredElementCollector collector2 = new FilteredElementCollector(doc);
                    ICollection<Element> elementCollection = collector2.OfClass(typeof(TextNoteType)).ToElements();
                    var textElements = collector2.Where(x => x.Name == "LegendText").Select(x => x);
                    if (textElements.Count() > 0)
                    {
                        TextNoteType textType = textElements.Cast<TextNoteType>().ElementAt<TextNoteType>(0);
                    }

                    analysisDisplay = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, "Фактические конструкции",
                        coloredSurfaceSettings, coloreSettings, legendSettings);

                    AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettingsHide =
                        new AnalysisDisplayColoredSurfaceSettings(coloredSurfaceSettings);
                    AnalysisDisplayColorSettings coloreSettingsHide = new AnalysisDisplayColorSettings(coloreSettings);
                    analysisDisplayHide=AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, "Фактические конструкции (скрытый)", coloredSurfaceSettings,coloreSettingsHide,legendSettingsHide);
                }
                else
                {
                    analysisDisplay = displayStyle.Cast<AnalysisDisplayStyle>().ElementAt<AnalysisDisplayStyle>(0);
                    
                }
                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplay.Id;//примененение 
               tr.Commit();
            }

            using (Transaction tr1 = new Transaction(doc, "Создание результатов анализа"))
            {
                tr1.Start();
                Stopwatch sw = new Stopwatch(); // чтобы увидеть время выполнения транзакции
                sw.Start();
                SpatialFieldManager spf = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
                if (spf == null) spf = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                ICollection<Element> collection1 = collector1.OfClass(typeof(AnalysisDisplayStyle)).ToElements();
                var analysisDisplay = collection1.Where(x => x.Name == "Фактические конструкции").Select(x => x);
                var analysisDisplayHide = collection1.Where(x => x.Name == "Фактические конструкции (скрытый)").Select(x => x);
                int i = 0;
                List<XYZ> listVerifiedXYZ = new List<XYZ>();
                foreach (Face face in topFaces)
                {
                    AnalysisResultSchema resultSchema = new AnalysisResultSchema("Диаграмма плоскости" + " " + i.ToString(), "Цветная схема перепада отметок");
                    if (i == 0)
                    {
                        resultSchema.AnalysisDisplayStyleId = analysisDisplay?.First().Id ?? null;
                    }
                    else
                    {
                        resultSchema.AnalysisDisplayStyleId = analysisDisplayHide?.First().Id ?? null;
                    }
                    IList<UV> uvpts = new List<UV>();
                    List<XYZ> listXYZ = face.GetListPointInFace();
                    var faceNormal = (face as PlanarFace).FaceNormal;
                    var surface = face.GetSurface();
                    List<double> doubleList = new List<double>();
                    IList<ValueAtPoint> valList = new List<ValueAtPoint>();
                    foreach (var point in listXYZ)
                    {
                        if (listVerifiedXYZ.Contains(point))
                        {
                            continue;
                        }
                        else
                        {
                            listVerifiedXYZ.Add(point);
                        }
                        UV outUV = null;
                        double distance;
                        surface.Project(point, out outUV, out distance);
                        uvpts.Add(outUV);
                        UV outVerexMin = null;
                        surface.Project(vertexMin.Position, out outVerexMin, out distance);
                        uvpts.Add(outVerexMin);
                        UV outVertexMax = null;
                        
                        surface.Project(vertexMax.Position, out outVertexMax, out distance);
                        uvpts.Add(outVertexMax);
                        
                        doubleList.Add(point.Z*304.8);
                        valList.Add(new ValueAtPoint(doubleList));
                        doubleList.Clear();
                        doubleList.Add(vertexMin.Position.Z*304.8);
                        valList.Add(new ValueAtPoint(doubleList));
                        doubleList.Clear();
                        doubleList.Add(vertexMax.Position.Z*304.8);
                        valList.Add(new ValueAtPoint(doubleList));
                        doubleList.Clear();

                    }
                    //опять получаю одну и туже ссылку на разные плокости
                    Reference reference = face.Reference;
                    int idx = spf.AddSpatialFieldPrimitive(reference);
                    
                    BoundingBoxUV bb = face.GetBoundingBox();
                    FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvpts);
                    FieldValues vals = new FieldValues(valList);
                    int schemaIndex = spf.RegisterResult(resultSchema);
                    spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
                    i++;
                    if (i == 600) break; 
                }
                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                TaskDialog.Show("Время выполнения", "Время выполнения изменения высот субэлементов плиты" + "\n" + elapsedTime);
                tr1.Commit();
            }
            return Result.Succeeded;
        }
        public ICollection<POINT.Point> GeneratePointCollection(ref Face topface, ref double stepmin)
        {
            if(topface==null) return null;
            var pointTopFace = topface.GetListPointInFace(); // точки верхней грани
            XYZ xMin, yMin;
            XYZ xMax, yMax;
            //Z координата верхней грани
            xMax = new XYZ(double.MinValue, 0, pointTopFace[0].Z);
            yMax = new XYZ(0, double.MinValue, pointTopFace[0].Z);
            xMin = new XYZ(double.MaxValue, 0, pointTopFace[0].Z);
            yMin = new XYZ(0, double.MaxValue, pointTopFace[0].Z);
            //координаты минимума и максимума 
            foreach (var point in pointTopFace)
            {
                if (point.X >= xMax.X) xMax = point;
                if (point.Y >= yMax.Y) yMax = point;
                if (point.X <= xMin.X) xMin = point;
                if (point.Y <= yMin.Y) yMin = point;
            }
            List<POINT.Point> pointGenerate = new List<POINT.Point>();
            double heightTriangle = (Math.Sqrt(3)) * stepmin / 2.0d;
            bool offset = false;
            int huita = (int)(Math.Abs(yMax.Y - yMin.Y) / heightTriangle) + 1;
            for (int i = 0; i <= ((int)(Math.Abs(yMax.Y - yMin.Y) / heightTriangle)) + 1; i++)
            {
                for (int j = 0; j <= (int)(Math.Abs((xMax.X - xMin.X) / stepmin)) + 1; j++)
                {
                    double posX = xMin.X + stepmin * j + (offset ? stepmin / 2 : 0);
                    double posY = yMin.Y + heightTriangle * i;
                    pointGenerate.Add(new POINT.Point(posX.DoubleToNumbers(3), posY.DoubleToNumbers(3), pointTopFace[0].Z));
                }
                offset = !offset;
            }
            double stepMinForLinq = stepmin;
            foreach (var point in pointGenerate)
            {
                var pointCollection = pointGenerate
                    .Where(x => ((Math.Abs(x.X - point.X) <= stepMinForLinq + 0.01 * stepMinForLinq) ||
                                 (Math.Abs(x.X - point.X) <= stepMinForLinq - 0.01 * stepMinForLinq))
                                && ((Math.Abs(x.Y - point.Y) <= heightTriangle + 0.01 * heightTriangle) ||
                                    (Math.Abs(x.Y - point.Y) <= heightTriangle - 0.01 * heightTriangle) || (x.Y == point.Y)) && (x.Equals(point) == false));
                foreach (var pointToHex in pointCollection)
                {
                    point.pointForHex.Add(pointToHex);
                }
            }
            return pointGenerate;
        }

        public ICollection<POINT.Point> IsInFigureWithBordersFace(ref ICollection<POINT.Point> points, ref Face topFace, ref double stepMin)
        {
            if(points==null || topFace==null) return null;
            var pointInFace = topFace.GetListPointInFace();
            foreach (var point in points)
            {
                if (VisiblePointInRegion(pointInFace, point) == true)
                {
                    point.getPointPosition = PointInFigureWithBorders.inFigure;
                    continue;
                }
                else
                {
                    point.getPointPosition = PointInFigureWithBorders.outFigure;
                }
            }
            foreach (var point in points)
            {
                point.getPointPosition = ChangeStatusPoint(point, ref topFace);
            }
            return points;
        }
        public bool VisiblePointInRegion(List<XYZ> listPointInTopFace, POINT.Point point)//переписать проверку попадания в треугольник, из-за беконечной линии есть фантом точки за треугольником. 
        {
            GraphicsPath graphicsPath = new GraphicsPath();
            listPointInTopFace.Add(listPointInTopFace[0]);
            for (int i = 0; i < listPointInTopFace.Count - 1; i++)
            {
                graphicsPath.AddLine((float)listPointInTopFace[i].X, (float)listPointInTopFace[i].Y, (float)listPointInTopFace[i + 1].X, (float)listPointInTopFace[i + 1].Y);
            }
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }
        public PointInFigureWithBorders ChangeStatusPoint(POINT.Point point, ref Face topFace)
        {
            IList<CurveLoop> curveArray = topFace.GetEdgesAsCurveLoops();
            foreach (var curveLoop in curveArray)
            {
                foreach (var curve in curveLoop)
                {
                    XYZ pointA = curve.GetEndPoint(0);
                    XYZ pointB = curve.GetEndPoint(1);
                    double vecScalyar = (point.X - curve.GetEndPoint(0).X.DoubleToNumbers(3)) *
                                        (curve.GetEndPoint(1).Y.DoubleToNumbers(3) -
                                         curve.GetEndPoint(0).Y.DoubleToNumbers(3)) -
                                        ((curve.GetEndPoint(1).X.DoubleToNumbers(3) -
                                          curve.GetEndPoint(0).X.DoubleToNumbers(3)) *
                                         (point.Y - curve.GetEndPoint(0).Y.DoubleToNumbers(3)));
                    if (vecScalyar == 0)
                    {
                        return PointInFigureWithBorders.inBorders;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
            return point.getPointPosition;
        }
        public void MovieOutFigurePointInBorders(ref ICollection<POINT.Point> pointCollection, ref Face topFace,ref double stepMin)
        {
            var pointTopface = topFace.GetListPointInFace();
            int countPointInFigure = 0;
            // центр масс
            foreach (var point in pointCollection)
            {
                if (point.getPointPosition == PointInFigureWithBorders.inFigure || point.getPointPosition == PointInFigureWithBorders.inBorders)
                {
                    continue;
                }
                else
                {
                    double superPositionX = 0;
                    double superPositionY = 0;
                    double superPositionZ = pointTopface[0].Z;
                    int countPointInFigureHex = 0;
                    foreach (var pointToHex in point.pointForHex)
                    {

                        if (pointToHex.getPointPosition == PointInFigureWithBorders.inFigure)
                        {
                            countPointInFigureHex++;
                            superPositionX = superPositionX + pointToHex.X;
                            superPositionY = superPositionY + pointToHex.Y;
                        }
                    }
                    if (countPointInFigureHex == 0)
                    {
                        point.getPointPosition = PointInFigureWithBorders.deleteThisPoint;
                        continue;
                    }
                    superPositionX = (superPositionX / countPointInFigureHex).DoubleToNumbers(3);
                    superPositionY = (superPositionY / countPointInFigureHex).DoubleToNumbers(3);
                    if (point.getPointPosition == PointInFigureWithBorders.outFigure)
                    {
                        XYZ origin = new XYZ(superPositionX, superPositionY, superPositionZ);
                        var line = (Autodesk.Revit.DB.Line.CreateBound(point.CreateXYZFromPoint(), origin) as Curve);
                        line.MakeUnbound();
                        List<XYZ> pointCross = new List<XYZ>();
                        foreach (var curveLoop in topFace.GetEdgesAsCurveLoops())
                        {
                            foreach (var curve in curveLoop)
                            {
                                //разобрать  с результатом
                                var result = curve.Intersect(line, out IntersectionResultArray resultArray);
                                //List<double> distanceIntersectPoint=new List<double>();
                                if (result == SetComparisonResult.Overlap)
                                {
                                    foreach (var getResult in resultArray)
                                    {
                                        var getResultIntersectionResult = getResult as IntersectionResult;
                                        var intersectPoint = getResultIntersectionResult.XYZPoint;
                                        pointCross.Add(intersectPoint);
                                        if (intersectPoint == null)
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        XYZ convertPoint = new XYZ(point.X, point.Y, superPositionZ);
                        XYZ tempPoint = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                        foreach (var intersectPoint in pointCross)
                        {
                            if (intersectPoint.DistanceTo(convertPoint) <= convertPoint.DistanceTo(tempPoint))
                            {
                                tempPoint = intersectPoint;
                            }
                        }

                        if (convertPoint.DistanceTo(tempPoint) <= 0.25 * stepMin || pointCross.Count == 0)
                        {
                            point.getPointPosition = PointInFigureWithBorders.deleteThisPoint;
                        }
                        else
                        {
                            point.X = tempPoint.X;
                            point.Y = tempPoint.Y;
                            point.Z = superPositionZ;
                            point.getPointPosition = PointInFigureWithBorders.moviePointFromOutFigure;
                        }
                    }
                }
            }
        }
    }
}
