
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
using Point = Autodesk.Revit.DB.Point;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.DB.DirectContext3D;

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
            stepMin = 0.5;
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
                    .Select(x =>
                    {
                        x.GetListEdgeForPoint();
                        return x;
                    })
                    .ToList();
            List<XYZ> pointTopFace = topFace.GetListPointInFace();
            foreach (var XYZToPoint in pointTopFace)
            {
                listPointWithoutDeletePoint.Add(new POINT.Point(XYZToPoint.X,XYZToPoint.Y,XYZToPoint.Z));
            }
            //using (Transaction tr = new Transaction(doc,"Визуализация точками"))
            //{
            //    tr.Start();
            //    foreach (POINT.Point point in listPointWithoutDeletePoint)
            //    {
            //        XYZ convertPoint = new XYZ(point.X, point.Y, point.Z);
            //        var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));// размещение обобщенной модели точки
            //        directShape.SetShape(new List<GeometryObject>() { Autodesk.Revit.DB.Point.Create(convertPoint) });// размещение обощенной модели точки
            //    }
            //tr.Commit();
            //}

            #region Триангуляция поверхности
            Stopwatch sw1 = new Stopwatch(); // чтобы увидеть время выполнения транзакции
            sw1.Start();
            var floorParameters = selectElement.Parameters;
            double thicknesFloor=0;
            foreach (Parameter parameter in floorParameters)
            {
                //Высота пола
                ElementId searchElementId = new ElementId(-1001900);
                if (parameter.Id == searchElementId)
                {
                    thicknesFloor = parameter.AsDouble();
                    break;
                }
            }
            double hight = (Math.Sqrt(3)) * stepMin / 2.0d;
            Dictionary<XYZ, List<XYZ>> dictionaryVertexAndPointCloud = new Dictionary<XYZ, List<XYZ>>();
            double zMin = (topFace as PlanarFace).Origin.Z - thicknesFloor;
            double zMax = (topFace as PlanarFace).Origin.Z + 0.75;
            PointCloudInstance pcInstance = selectPointCloud as PointCloudInstance;
            foreach (var pointToTriangulate in listPointWithoutDeletePoint)
            {
                // создадим треугольник с вершинами ABC B известная
                XYZ pointB = pointToTriangulate.CreateXYZFromPoint();
                XYZ pointA = new XYZ(pointB.X - hight, pointB.Y - stepMin, pointB.Z);
                XYZ pointC = new XYZ(pointA.X+stepMin, pointA.Y, pointA.Z);
                List<XYZ> listPointFace = new List<XYZ>() { pointB, pointA, pointC,pointB };
                //планы боковые
                List<Plane> planeFace=new List<Plane>();
                for (int i = 0; i < listPointFace.Count - 1; i++)
                {
                    XYZ newPointInBottomFace = new XYZ(listPointFace[i].X, listPointFace[i].Y, zMin);
                    Plane getPlaneFace = Plane.CreateByThreePoints(listPointFace[i], listPointFace[i + 1], newPointInBottomFace);
                    planeFace.Add(getPlaneFace);
                }
                IList<Plane> faceToPlane = new List<Plane>();
                foreach (Plane plane in planeFace)
                {
                    XYZ reversNormalPlane = new XYZ(plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
                    Plane reversPlane = Plane.CreateByNormalAndOrigin(reversNormalPlane, plane.Origin);
                    faceToPlane.Add(reversPlane);
                }
                Plane planeMinHeight = Plane.CreateByThreePoints(
                    new XYZ(listPointFace[0].X, listPointFace[0].Y, zMin),
                    new XYZ(listPointFace[1].X, listPointFace[1].Y, zMin),
                    new XYZ(listPointFace[2].X, listPointFace[2].Y, zMin));
                Plane planeMaxHeight = Plane.CreateByThreePoints(new XYZ(listPointFace[0].X, listPointFace[0].Y, zMax),
                    new XYZ(listPointFace[1].X, listPointFace[1].Y, zMax),
                    new XYZ(listPointFace[2].X, listPointFace[2].Y, zMax));
                XYZ reversNormalMaxheight = new XYZ(planeMaxHeight.Normal.X, planeMaxHeight.Normal.Y, planeMaxHeight.Normal.Z * (-1));
                Plane reversPlaneMaxHeight = Plane.CreateByNormalAndOrigin(reversNormalMaxheight, planeMaxHeight.Origin);
                faceToPlane.Add(reversPlaneMaxHeight);
                faceToPlane.Add(planeMinHeight);
                PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(faceToPlane);
                Autodesk.Revit.DB.Transform pointTransform = pcInstance.GetTotalTransform();

                PointCollection pointCollection = pcInstance.GetPoints(filter, 0.0001, 500);// как-будто надо увеличить количество точек
                var variable = pointCollection.Select(x => pointTransform.OfPoint((XYZ)x)).ToList();
                dictionaryVertexAndPointCloud.Add(pointB,variable);
            }
            Dictionary<XYZ,double> XYZAndPredictZ=new Dictionary<XYZ,double>();
            foreach (var VARIABLE in dictionaryVertexAndPointCloud)
            {
                double zPredict;
                if (VARIABLE.Value.Count > 0 && VARIABLE.Value.Count > 10)
                {
                    AproximateXZ(VARIABLE.Value, out double a, out double b);
                    zPredict = a * VARIABLE.Key.X + b;
                }
                else
                {
                    zPredict = VARIABLE.Key.Z;
                }
                XYZAndPredictZ.Add(VARIABLE.Key, zPredict);
            }
            sw1.Stop();
            TimeSpan ts1 = sw1.Elapsed;
            string elapsedTime1 = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts1.Hours, ts1.Minutes, ts1.Seconds,
                ts1.Milliseconds / 10);
            TaskDialog.Show("Время выполнения", "Время выполнения изменения высот субэлементов плиты" + "\n" + elapsedTime1);
            #endregion

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

            //using (Transaction tr1 = new Transaction(doc, "Создание результатов анализа"))
            //{
            //    tr1.Start();
            //    Stopwatch sw = new Stopwatch(); // чтобы увидеть время выполнения транзакции
            //    sw.Start();
            //    SpatialFieldManager spf = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            //    if (spf == null) spf = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            //    FilteredElementCollector collector1 = new FilteredElementCollector(doc);
            //    ICollection<Element> collection1 = collector1.OfClass(typeof(AnalysisDisplayStyle)).ToElements();
            //    var analysisDisplay = collection1.Where(x => x.Name == "Фактические конструкции").Select(x => x);
            //    var analysisDisplayHide = collection1.Where(x => x.Name == "Фактические конструкции (скрытый)").Select(x => x);
            //    int i = 0;
            //    List<XYZ> listVerifiedXYZ = new List<XYZ>();
            //    foreach (Face face in topFaces)
            //    {
            //        AnalysisResultSchema resultSchema = new AnalysisResultSchema("Диаграмма плоскости" + " " + i.ToString(), "Цветная схема перепада отметок");
            //        if (i == 0)
            //        {
            //            resultSchema.AnalysisDisplayStyleId = analysisDisplay?.First().Id ?? null;
            //        }
            //        else
            //        {
            //            resultSchema.AnalysisDisplayStyleId = analysisDisplayHide?.First().Id ?? null;
            //        }
            //        IList<UV> uvpts = new List<UV>();
            //        List<XYZ> listXYZ = face.GetListPointInFace();
            //        var faceNormal = (face as PlanarFace).FaceNormal;
            //        var surface = face.GetSurface();
            //        List<double> doubleList = new List<double>();
            //        IList<ValueAtPoint> valList = new List<ValueAtPoint>();
            //        foreach (var point in listXYZ)
            //        {
            //            if (listVerifiedXYZ.Contains(point))
            //            {
            //                continue;
            //            }
            //            else
            //            {
            //                listVerifiedXYZ.Add(point);
            //            }
            //            UV outUV = null;
            //            double distance;
            //            surface.Project(point, out outUV, out distance);
            //            uvpts.Add(outUV);
            //            UV outVerexMin = null;
            //            surface.Project(vertexMin.Position, out outVerexMin, out distance);
            //            uvpts.Add(outVerexMin);
            //            UV outVertexMax = null;
                        
            //            surface.Project(vertexMax.Position, out outVertexMax, out distance);
            //            uvpts.Add(outVertexMax);
                        
            //            doubleList.Add(point.Z*304.8);
            //            valList.Add(new ValueAtPoint(doubleList));
            //            doubleList.Clear();
            //            doubleList.Add(vertexMin.Position.Z*304.8);
            //            valList.Add(new ValueAtPoint(doubleList));
            //            doubleList.Clear();
            //            doubleList.Add(vertexMax.Position.Z*304.8);
            //            valList.Add(new ValueAtPoint(doubleList));
            //            doubleList.Clear();

            //        }
            //        //опять получаю одну и туже ссылку на разные плокости
            //        Reference reference = face.Reference;
            //        int idx = spf.AddSpatialFieldPrimitive(reference);
                    
            //        BoundingBoxUV bb = face.GetBoundingBox();
            //        FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvpts);
            //        FieldValues vals = new FieldValues(valList);
            //        int schemaIndex = spf.RegisterResult(resultSchema);
            //        spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
            //        i++;
            //        if (i == 600) break; 
            //    }
            //    sw.Stop();
            //    TimeSpan ts = sw.Elapsed;
            //    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
            //        ts.Milliseconds / 10);
            //    TaskDialog.Show("Время выполнения", "Время выполнения изменения высот субэлементов плиты" + "\n" + elapsedTime);
            //    tr1.Commit();
            //}

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
                
                //List<XYZ> listXYZ = face.GetListPointInFace();
                var faceNormal = (topFace as PlanarFace).FaceNormal;
                var surface = topFace.GetSurface();
                
                var minValue = XYZAndPredictZ.OrderBy(x => x.Value).First();
                var maxValue = XYZAndPredictZ.OrderByDescending(x => x.Value).First();
                IList<UV> uvpts = new List<UV>();
                IList<ValueAtPoint> valList = new List<ValueAtPoint>();
                AnalysisResultSchema resultSchema = new AnalysisResultSchema("Диаграмма плоскости" + " " + i.ToString(), "Цветная схема перепада отметок");
                if (i == 0)
                {
                    resultSchema.AnalysisDisplayStyleId = analysisDisplay?.First().Id ?? null;
                }
                else
                {
                    resultSchema.AnalysisDisplayStyleId = analysisDisplayHide?.First().Id ?? null;
                }
                foreach (var dictionaryItem in XYZAndPredictZ)
                {
                    
                    List<double> doubleList = new List<double>();
                    UV outUV = null; 
                    double distance;
                    surface.Project(dictionaryItem.Key, out outUV, out distance); 
                    uvpts.Add(outUV);
                    double zPredict = dictionaryItem.Value;
                    doubleList.Add(zPredict * 304.8);
                    valList.Add(new ValueAtPoint(doubleList));
                    doubleList.Clear();
                    //опять получаю одну и туже ссылку на разные плокости
                    i++;
                    //if (i == 100) break;
                }
                Reference reference = topFace.Reference;
                int idx = spf.AddSpatialFieldPrimitive(reference);
                FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvpts);
                FieldValues vals = new FieldValues(valList);
                int schemaIndex = spf.RegisterResult(resultSchema);
                spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
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
        public bool EqualsPlane(Plane currentPlane, Plane plane)
        {
            if (ReferenceEquals(currentPlane, plane)) return true;
            if (plane == null || currentPlane == null) return false;
            if (currentPlane.Normal.IsAlmostEqualTo(plane.Normal) &&
                currentPlane.Origin.IsAlmostEqualTo(plane.Origin) &&
                currentPlane.XVec.IsAlmostEqualTo(plane.XVec) &&
                currentPlane.YVec.IsAlmostEqualTo(plane.XVec))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void AproximateXZ(List<XYZ> pointCollection, out double a, out double b)
        {
            a = 0;
            b = 0;
            double a1 = 0, a2 = 0, a3 = 0, sumx = 0, sumy = 0;
            foreach (var point in pointCollection)
            {
                a1 = a1 + point.X * point.Z;
                sumx = sumx + point.X;
                sumy = sumy + point.Z;
                a2 = a2 + Math.Pow(point.X, 2);
            }
            a3 = Math.Pow(sumx, 2);
            a = (pointCollection.Count * a1 - sumx * sumy) / (pointCollection.Count * a2 - a3);
            b = (a2 * sumy - sumx * a1) / (pointCollection.Count * a2 - a3);
        }
    }
}