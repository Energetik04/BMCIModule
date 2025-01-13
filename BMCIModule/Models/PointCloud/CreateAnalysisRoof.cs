using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using POINT = BMCIModule.Models.PointCloud;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using BMCIModule.Models.Extension;
using BMCIModule.Models;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Net;
using System.Net.Configuration;
using System.Windows.Media;
using System.Windows.Media.Media3D;

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

            //начальные параметры
            stepMin = 1.5;
            captureRange = 0.2;
            double hight = (Math.Sqrt(3)) * stepMin / 2.0d;

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите экземпляр (Крыша)");
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
            PointCloudInstance pcInstance = selectPointCloud as PointCloudInstance;
            Autodesk.Revit.DB.Transform pointTransform = pcInstance.GetTotalTransform();

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
                faceList.Add(face);
            }
            //var topFaceList=new List<Face>();

            //TODO: херовина с нормалью, она мождет быть приближенной к 0 и выше, т.е. отрицательной
            var planarFace = faceList.Where(x => (x is PlanarFace))
                .Where(x => Math.Round((x as PlanarFace).FaceNormal.Z,9) > 0)
                .Select(x => x).Cast<Face>()
                .ToList();
            
            var cylindricalFace = faceList.Where(x => x is CylindricalFace).Select(x => (x as CylindricalFace))
                .Where(x=>Math.Round(x.Axis.X,9)>=0 && Math.Round(x.Axis.Y,9)>=0 && Math.Round(x.Axis.Z,9)>=0)
                .Cast<Face>()
                .ToList();
            List<Face> listFaceToAnalysis = new List<Face>();
            listFaceToAnalysis.AddRange(cylindricalFace);
            listFaceToAnalysis.AddRange(planarFace);
            #region Работа с CylindricalFace
            
            Dictionary<Face, List<UV>> dictionaryFaceAndUV = new Dictionary<Face, List<UV>>();
            Dictionary<UV, double> collectionPointCloudPoint = new Dictionary<UV, double>();
            Dictionary<XYZ,double> XYZPredict=new Dictionary<XYZ,double>();
            foreach (Face face in cylindricalFace)
            {
                BoundingBoxUV cylindircalParameterzs = face.GetBoundingBox();
                UV maxParameters = cylindircalParameterzs.Max;
                UV minParameters= cylindircalParameterzs.Min;
                var radius = (face.GetSurface() as CylindricalSurface).Radius;
                var lengthSigment = (maxParameters.U - minParameters.U) * radius;
                var countStep = (double)Math.Round(lengthSigment / stepMin);
                var stepSigmentAngle = (maxParameters.U - minParameters.U)/(countStep);
                var stepSigmentV = (maxParameters.V - minParameters.V) / stepMin;
                List<UV> listPointFace= new List<UV>();
                for (int i = 0; i <= (int)(lengthSigment / stepMin); i++)
                {
                    for (int j = 0; j <= (int)stepSigmentV; j++)
                    {
                        var Upoint = minParameters.U + stepSigmentAngle * i;
                        var Vpoint = minParameters.V + stepMin * j;
                        UV point = new UV(Upoint,Vpoint);
                        listPointFace.Add(point);
                    }
                }
                foreach (UV pointUV in listPointFace)
                {
                    XYZ point1 = face.Evaluate(pointUV);
                    CylindricalFace getFace=face as CylindricalFace;
                    XYZ normalFaceEnd2 = getFace.Axis.Normalize() * pointUV.V+getFace.Origin;
                    XYZ normalToFace=point1-normalFaceEnd2;
                    Plane startPlane = Plane.CreateByNormalAndOrigin(normalToFace, point1);
                    BoundingBoxUV bbToTriangle = startPlane.GetBoundingBoxUV();
                    UV minTriangle = bbToTriangle.Min;
                    IList<Plane> faceToPlane = new List<Plane>();

                    #region Работа с треугольником
                    //UV pointB = minTriangle;
                    UV pointB=new UV();
                    startPlane.Project(point1,out pointB,out double distance);
                    UV pointA = new UV(pointB.U - stepMin / 2, pointB.V - hight);
                    UV pointC = new UV(pointA.U + stepMin, pointA.V);
                    //TODO математически не обосновано 
                    UV centre=new UV(pointB.U, pointB.V - hight/2);

                    XYZ pointBXYZ = startPlane.Origin + pointB.U * startPlane.XVec + pointB.V * startPlane.YVec;
                    XYZ pointAXYZ = startPlane.Origin + pointA.U * startPlane.XVec + pointA.V * startPlane.YVec;
                    XYZ pointCXYZ = startPlane.Origin + pointC.U * startPlane.XVec + pointC.V * startPlane.YVec;
                    XYZ centreStartPlane = startPlane.Origin + centre.U * startPlane.XVec + centre.V * startPlane.YVec;
                    List<XYZ> triangleStart= new List<XYZ>(){pointBXYZ,pointAXYZ,pointCXYZ};

                    #endregion

                    #region Сдвигаем треугольник наверх

                    XYZ pointBXYZMax = pointBXYZ + (normalToFace).Normalize() * captureRange;
                    XYZ pointAXYZMax = pointAXYZ + (normalToFace).Normalize() * captureRange;
                    XYZ pointCXYZMax = pointCXYZ + (normalToFace).Normalize() * captureRange;

                    Plane maxHightTriangle = Plane.CreateByThreePoints(pointBXYZMax, pointAXYZMax, pointCXYZMax);
                    List<XYZ> triangleMaxHight=new List<XYZ>(){ pointBXYZMax , pointAXYZMax , pointCXYZMax , pointBXYZMax };

                    XYZ pointBXYZMin = pointBXYZ + (-1)*(normalToFace).Normalize() * captureRange;
                    XYZ pointAXYZMin = pointAXYZ + (-1) * (normalToFace).Normalize() * captureRange;
                    XYZ pointCXYZMin = pointCXYZ + (-1) * (normalToFace).Normalize() * captureRange;

                    Plane minHightTriangle = Plane.CreateByThreePoints(pointBXYZMin, pointAXYZMin, pointCXYZMin);
                    List<XYZ> triangleMinHight = new List<XYZ>() { pointBXYZMin , pointAXYZMin , pointCXYZMin , pointBXYZMin };

                    #endregion

                    for (int i = 0; i < triangleMaxHight.Count()-1; i++)
                    {
                        Plane sidePlane = Plane.CreateByThreePoints(triangleMinHight[i], triangleMinHight[i+1], triangleMaxHight[i]);
                        faceToPlane.Add(sidePlane);
                    }
                    IList<Plane> reversList= new List<Plane>();
                    foreach (var plane in faceToPlane)
                    {
                        XYZ reversNormal = plane.Origin - centreStartPlane;
                        Plane reversPlane = Plane.CreateByNormalAndOrigin(reversNormal*(-1), plane.Origin);
                        reversList.Add(reversPlane);
                    }
                    Plane reversMaxPlane =
                        Plane.CreateByNormalAndOrigin((maxHightTriangle.Normal * (-1)), maxHightTriangle.Origin);
                    reversList.Add(minHightTriangle);
                    reversList.Add(reversMaxPlane);
                    PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(reversList);
                    Autodesk.Revit.DB.PointClouds.PointCollection pointCollection = pcInstance.GetPoints(filter, 0.0001, 4000);// как-будто надо увеличить количество точек
                    var variable = pointCollection.Select(x => pointTransform.OfPoint((XYZ)x)).ToList();
                    variable.TrimExcess();
                    double zPredict;
                    if (variable.Count > 10)
                    {
                        AproximateXZ(variable, out double a, out double b);
                        zPredict = a * point1.X + b;
                        if (zPredict.Equals(Double.NaN))
                        {
                            zPredict = 0;
                        }
                        if (Math.Abs(point1.Z - zPredict) >= captureRange)
                        {
                            zPredict = 0;
                        }
                    }
                    else
                    {
                        zPredict = point1.Z;
                    }
                    collectionPointCloudPoint.Add(pointUV, zPredict);
                    XYZPredict.Add(pointBXYZ,zPredict);
                }
                dictionaryFaceAndUV.Add(face,listPointFace);
            }
            #endregion

            #region Работа с PlanarFace
            // вообще можно такой же механизм попробовать применить как с круглыми плоскостями
            Dictionary<Face, List<UV>> dictionaryFaceAndUVPlanarface = new Dictionary<Face, List<UV>>();
            

            Dictionary<UV, double> collectionPointCloudPointPlanarFace = new Dictionary<UV, double>();
            Dictionary<Face, ICollection<POINT.Point>> dictionaryPlanarFaceAndPoint =
                new Dictionary<Face, ICollection<POINT.Point>>();
            foreach (Face face in planarFace)
            {
                List<UV> listPointInPlanarFace = new List<UV>();
                Face topFace = face;
                ICollection<POINT.Point> pointRevit = GeneratePointCollection(ref topFace, ref stepMin);
                ICollection<POINT.Point> collection = IsInFigureWithBordersFace(ref pointRevit, ref topFace, ref stepMin);
                MovieOutFigurePointInBorders(ref collection, ref topFace, ref stepMin);
                List<XYZ> pointTopFace = topFace.GetListPointInFace();
                var listPointWithoutDeletePoint
                    = collection.Where(x => x.getPointPosition != PointInFigureWithBorders.deleteThisPoint)
                        .Select(x =>
                        {
                            x.GetListEdgeForPoint();
                            return x;
                        })
                        .ToList();
                foreach (var XYZToPoint in pointTopFace)
                {
                    listPointWithoutDeletePoint.Add(new POINT.Point(XYZToPoint.X, XYZToPoint.Y, XYZToPoint.Z));
                }
                foreach (var pointToTriangulate in listPointWithoutDeletePoint)
                {
                    IList<Plane> faceToPlanePlaanrFace = new List<Plane>();
                    XYZ pointCreateFromPoint = pointToTriangulate.CreateXYZFromPoint();
                    var result = face.Project(pointCreateFromPoint);
                    if(result==null) continue;
                    XYZ pointBPlanarFace = result.XYZPoint;
                    XYZ normalToPlanarFace = (face as PlanarFace)?.FaceNormal;
                    Plane startPlanePlanarFace = Plane.CreateByNormalAndOrigin(normalToPlanarFace, pointBPlanarFace);
                    UV pointBPlanarFaceUV=new UV();
                    face.GetSurface().Project(pointBPlanarFace,out pointBPlanarFaceUV,out double distance);
                    listPointInPlanarFace.Add(pointBPlanarFaceUV);
                    UV pointAPlanarFaceUV = new UV(pointBPlanarFaceUV.U-stepMin/2,pointBPlanarFaceUV.V-hight);
                    UV pointCPlanarFaceUV = new UV(pointAPlanarFaceUV.U+stepMin,pointAPlanarFaceUV.V);
                    UV centreTrianglePlanarFaceUV = new UV(pointBPlanarFaceUV.U, pointBPlanarFaceUV.V - hight / 2);
                    var faceToPlane = ((face as PlanarFace).GetSurface() as Plane);
                    XYZ pointBXYZPlanarFace = (face as PlanarFace).Origin +
                                              pointBPlanarFaceUV.U * faceToPlane.XVec +
                                              pointBPlanarFaceUV.V * faceToPlane.YVec;
                    XYZ pointAXYZPlanarFace = (face as PlanarFace).Origin +
                                              pointAPlanarFaceUV.U * faceToPlane.XVec +
                                              pointAPlanarFaceUV.V * faceToPlane.YVec;
                    XYZ pointCXYZPlanarFace = (face as PlanarFace).Origin +
                                              pointCPlanarFaceUV.U * faceToPlane.XVec +
                                              pointCPlanarFaceUV.V * faceToPlane.YVec;
                    XYZ centreTrianglePlanarface = (face as PlanarFace).Origin +
                                                   centreTrianglePlanarFaceUV.U * faceToPlane.XVec +
                                                   centreTrianglePlanarFaceUV.V * faceToPlane.YVec;

                    XYZ pointBXYZMaxPlanarFace = pointBXYZPlanarFace + (normalToPlanarFace).Normalize() * captureRange;
                    XYZ pointAXYZMaxPlanarFace = pointAXYZPlanarFace + (normalToPlanarFace).Normalize() * captureRange;
                    XYZ pointCXYZMaxPlanarFace = pointCXYZPlanarFace + (normalToPlanarFace).Normalize() * captureRange;

                    Plane maxHightTrianglePlanarFace = Plane.CreateByThreePoints(pointBXYZMaxPlanarFace,
                        pointAXYZMaxPlanarFace, pointCXYZMaxPlanarFace);
                    List<XYZ> trinagleMaxHightPlanarFace=new List<XYZ>(){ pointBXYZMaxPlanarFace , pointAXYZMaxPlanarFace , pointCXYZMaxPlanarFace , pointBXYZMaxPlanarFace };

                    XYZ pointBXYZMinPlanarFace = pointBXYZPlanarFace + (-1)*(normalToPlanarFace).Normalize() * captureRange;
                    XYZ pointAXYZMinPlanarFace = pointAXYZPlanarFace + (-1) * (normalToPlanarFace).Normalize() * captureRange;
                    XYZ pointCXYZMinPlanarFace = pointCXYZPlanarFace + (-1) * (normalToPlanarFace).Normalize() * captureRange;

                    Plane minHightTrianglePlanarFace = Plane.CreateByThreePoints(pointBXYZMinPlanarFace,
                        pointAXYZMinPlanarFace, pointCXYZMinPlanarFace);
                    List<XYZ> trinagleMinHightPlanarFace = new List<XYZ>() { pointBXYZMinPlanarFace, pointAXYZMinPlanarFace, pointCXYZMinPlanarFace, pointBXYZMinPlanarFace };

                    for (int i = 0; i < trinagleMaxHightPlanarFace.Count() - 1; i++)
                    {
                        Plane sidePlane = Plane.CreateByThreePoints(trinagleMaxHightPlanarFace[i], trinagleMaxHightPlanarFace[i + 1], trinagleMinHightPlanarFace[i]);
                        faceToPlanePlaanrFace.Add(sidePlane);
                    }
                    IList<Plane> reversListPlanarFace=new List<Plane>();
                    foreach (var plane in faceToPlanePlaanrFace)
                    {
                        XYZ reversNormalPlanarFace = plane.Origin - centreTrianglePlanarface;
                        Plane reversPlanePlanarFace=Plane.CreateByNormalAndOrigin(reversNormalPlanarFace*(-1),plane.Origin);
                        reversListPlanarFace.Add(reversPlanePlanarFace);
                    }

                    Plane reversMaxHightPlanarFace =
                        Plane.CreateByNormalAndOrigin(maxHightTrianglePlanarFace.Normal * (-1),
                            maxHightTrianglePlanarFace.Origin);
                    reversListPlanarFace.Add(reversMaxHightPlanarFace);
                    reversListPlanarFace.Add(minHightTrianglePlanarFace);

                    PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(reversListPlanarFace);

                    Autodesk.Revit.DB.PointClouds.PointCollection pointCollection = pcInstance.GetPoints(filter, 0.0001, 4000);// как-будто надо увеличить количество точек
                    var variable = pointCollection.Select(x => pointTransform.OfPoint((XYZ)x)).ToList();
                    variable.TrimExcess();
                    double zPredict;
                    if (variable.Count > 10)
                    {
                        AproximateXZ(variable, out double a, out double b);
                        zPredict = a * pointBPlanarFace.X + b;
                        if (zPredict.Equals(Double.NaN))
                        {
                            zPredict = pointBPlanarFace.Z;
                        }
                        if (Math.Abs(pointBPlanarFace.Z - zPredict) >= captureRange)
                        {
                            zPredict = pointBPlanarFace.Z;
                        }
                    }
                    else
                    {
                        zPredict = pointBPlanarFace.Z;
                    }
                    collectionPointCloudPointPlanarFace.Add(pointBPlanarFaceUV,zPredict);
                    XYZPredict.Add(pointBPlanarFace,zPredict);
                }
                dictionaryFaceAndUVPlanarface.Add(face,listPointInPlanarFace);
            }

            #endregion
            #region Создание типа легенды
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
                    AnalysisDisplayColorEntry colorIntermediate = new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0));
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
                    analysisDisplayHide = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, "Фактические конструкции (скрытый)", coloredSurfaceSettings, coloreSettingsHide, legendSettingsHide);
                }
                else
                {
                    analysisDisplay = displayStyle.Cast<AnalysisDisplayStyle>().ElementAt<AnalysisDisplayStyle>(0);

                }
                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplay.Id;//примененение 
                tr.Commit();
            }
            #endregion

            #region Создание таблицы результатов
            using (Transaction tr1 = new Transaction(doc, "Создание результатов анализа"))
            {
                //TODO найти max и min среди всех плоскостей, чтобы диаграмма была одинаковой
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
                IList<ValueAtPoint> valList = new List<ValueAtPoint>();
                IList<UV> uvpts = new List<UV>();
                // хлам для контроля значений
                var maxValue = XYZPredict.OrderByDescending(x => x.Value).First();
                var minValue = XYZPredict.OrderBy(x => x.Value).First();
                UV outUVmax = null;
                // TODO можно проще сделать
                foreach (Face face in listFaceToAnalysis)
                {
                    if (face is CylindricalFace)
                    {
                            var dictionaryItem = dictionaryFaceAndUV.Where(x => x.Key.Equals(face)).Select(x => x).First();
                            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Диаграмма плоскости" + " " + i.ToString(), "Цветная схема перепада отметок");
                            if (i == 0)
                            {
                                resultSchema.AnalysisDisplayStyleId = analysisDisplay?.First().Id ?? null;
                            }
                            else
                            {
                                resultSchema.AnalysisDisplayStyleId = analysisDisplayHide?.First().Id ?? null;
                            }

                            foreach (var UVpoint in dictionaryItem.Value)
                            {
                                var zPredict = collectionPointCloudPoint.Where(x => x.Key.Equals(UVpoint)).Select(x => x.Value).First();
                                List<double> doubleList = new List<double>();
                                UV outUV = null;
                                double distance;
                                var result = 0.0;
                                var zFace = dictionaryItem.Key.Evaluate(UVpoint).Z;
                                if (zFace < zPredict) result = zPredict - zFace;
                                if (zFace > zPredict) result = (-1) * Math.Abs(zFace - zPredict);
                                uvpts.Add(UVpoint);
                                doubleList.Add(result * 304.8);
                                valList.Add(new ValueAtPoint(doubleList));
                                doubleList.Clear();
                            }
                            face.GetSurface().Project(maxValue.Key, out outUVmax, out double distanceMax);
                            uvpts.Add(outUVmax);
                            valList.Add(new ValueAtPoint(new List<double>() { maxValue.Value }));
                            face.GetSurface().Project(maxValue.Key, out outUVmax, out double distanceMin);
                            uvpts.Add(outUVmax);
                            valList.Add(new ValueAtPoint(new List<double>() { minValue.Value }));
                            //опять получаю одну и туже ссылку на разные плокости
                            i++;
                            //if (i == 100) break;
                            Reference reference = dictionaryItem.Key.Reference;
                            int idx = spf.AddSpatialFieldPrimitive(reference);
                            FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvpts);
                            FieldValues vals = new FieldValues(valList);
                            int schemaIndex = spf.RegisterResult(resultSchema);
                            spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
                            
                    }
                    if (face is PlanarFace)
                    {
                        var dictionaryItem = dictionaryFaceAndUVPlanarface.Where(x => x.Key.Equals(face)).Select(x=>x).First();
                        //foreach (var dictionaryItem in dictionaryFaceAndUVPlanarface)
                        //{
                            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Диаграмма плоскости" + " " + i.ToString(), "Цветная схема перепада отметок");
                            if (i == 0)
                            {
                                resultSchema.AnalysisDisplayStyleId = analysisDisplay?.First().Id ?? null;
                            }
                            else
                            {
                                resultSchema.AnalysisDisplayStyleId = analysisDisplayHide?.First().Id ?? null;
                            }

                            foreach (var UVpoint in dictionaryItem.Value)
                            {
                                var zPredict = collectionPointCloudPointPlanarFace.Where(x => x.Key.Equals(UVpoint)).Select(x => x.Value).First();
                                List<double> doubleList = new List<double>();
                                UV outUV = null;
                                double distance;
                                var result = 0.0;
                                var zFace = dictionaryItem.Key.Evaluate(UVpoint).Z;
                                if (zFace < zPredict) result = zPredict - zFace;
                                if (zFace > zPredict) result = (-1) * Math.Abs(zFace - zPredict);
                                uvpts.Add(UVpoint);
                                doubleList.Add(result * 304.8);
                                valList.Add(new ValueAtPoint(doubleList));
                                doubleList.Clear();
                            }
                            //опять получаю одну и туже ссылку на разные плокости
                            i++;
                            face.GetSurface().Project(maxValue.Key, out outUVmax, out double distanceMax);
                            uvpts.Add(outUVmax);
                            valList.Add(new ValueAtPoint(new List<double>() { maxValue.Value }));
                            face.GetSurface().Project(maxValue.Key, out outUVmax, out double distanceMin);
                            uvpts.Add(outUVmax);
                            valList.Add(new ValueAtPoint(new List<double>() { minValue.Value }));
                            Reference reference = dictionaryItem.Key.Reference;
                            int idx = spf.AddSpatialFieldPrimitive(reference);
                            FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvpts);
                            FieldValues vals = new FieldValues(valList);
                            int schemaIndex = spf.RegisterResult(resultSchema);
                            spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
                            
                        //}
                    }
                    uvpts.Clear();
                    valList.Clear();
                }
                
                
                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                TaskDialog.Show("Время выполнения", "Время выполнения изменения высот субэлементов плиты" + "\n" + elapsedTime);
                tr1.Commit();
            }
            #endregion
            //topFaceList.AddRange(planarFace);
            //topFaceList.AddRange(cylindricalFace);

            return Result.Succeeded;
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
            double a4 = (pointCollection.Count * a1 - sumx * sumy);
            a = (Math.Round((pointCollection.Count * a1 - sumx * sumy), 9) / (Math.Round((pointCollection.Count * a2 - a3), 9)));
            b = Math.Round((a2 * sumy - sumx * a1), 9) / Math.Round((pointCollection.Count * a2 - a3), 9);
        }
        public ICollection<POINT.Point> GeneratePointCollection(ref Face topface, ref double stepmin)
        {
            if (topface == null) return null;
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
                    pointGenerate.Add(new POINT.Point(Math.Round(posX, 3), Math.Round(posY, 3), pointTopFace[0].Z));
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
            if (points == null || topFace == null) return null;
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
            List<double> vecScal = new List<double>();
            foreach (var curveLoop in curveArray)
            {
                foreach (var curve in curveLoop)
                {
                    XYZ pointA = curve.GetEndPoint(0);
                    XYZ pointB = curve.GetEndPoint(1);
                    double vecScalyar = (Math.Round(point.X - curve.GetEndPoint(0).X, 3)) *
                                        (Math.Round(curve.GetEndPoint(1).Y, 3) -
                                         Math.Round(curve.GetEndPoint(0).Y, 3)) -
                                        ((Math.Round(curve.GetEndPoint(1).X, 3) -
                                          Math.Round(curve.GetEndPoint(0).X, 3)) *
                                         (point.Y - Math.Round(curve.GetEndPoint(0).Y, 3)));
                    vecScal.Add(vecScalyar);
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
        public void MovieOutFigurePointInBorders(ref ICollection<POINT.Point> pointCollection, ref Face topFace, ref double stepMin)
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
                    superPositionX = Math.Round((superPositionX / countPointInFigureHex), 3);
                    superPositionY = Math.Round((superPositionY / countPointInFigureHex), 3);
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
