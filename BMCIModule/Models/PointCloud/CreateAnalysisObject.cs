using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.PointClouds;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace BMCIModule.Models.PointCloud
{
    [Transaction(TransactionMode.Manual)]
    public class CreateAnalysisObject: IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            double stepMin, captureRange;
            int numberPoint;
            
            stepMin = 1;
            captureRange = 0.3;
            double hight = (Math.Sqrt(3)) * stepMin / 2.0d;

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите экземпляр (Крыша)");
            Selection choiceElement = uidoc.Selection;
            //Reference choiceElementRef = choiceElement.PickObject(ObjectType.Face, "выбор плоскости");
            Reference choiceElementRef = choiceElement.PickObject(ObjectType.Element, "Выбор элемента");
            Element selectElement = doc.GetElement(choiceElementRef);
            ElementId selectElementId = selectElement.Id;

            #endregion

            #region SelectPointCloud

            TaskDialog.Show("Выбор элемента", "Выберите облако точек для построения аналитической поверхности");
            Selection choicePointCloud = uidoc.Selection;
            Reference choicePointCloudRef = choicePointCloud.PickObject(ObjectType.Element, "Выбор элемента");
            Element selectPointCloud = doc.GetElement(choicePointCloudRef);
            PointCloudInstance pcInstance = selectPointCloud as PointCloudInstance;
            Autodesk.Revit.DB.Transform pointTransform = pcInstance.GetTotalTransform();

            #endregion

            Options op = new Options()
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true,
                DetailLevel=ViewDetailLevel.Undefined
                //View=doc.ActiveView
            };

            List<Face> faceList = new List<Face>();
            //List<Face> cylindrical
            //FaceArray faceArray = (selectElement.get_Geometry(op).First() as Solid).Faces;
            FaceArray faceArray = (selectElement.get_Geometry(op).First() as GeometryInstance).SymbolGeometry.ToList().Select(x=>x as Solid).Where(x=>x.Volume!=0).First().Faces;
            foreach (Face face in faceArray)
            {
                if (face is CylindricalFace)
                {
                    if((face as CylindricalFace).Axis.Z<0) faceList.Add(face);
                }
            }

            FilteredElementCollector collectorPoint = new FilteredElementCollector(doc);
            var elementSet=collectorPoint.OfClass(typeof(FamilyInstance)).Where(x => x.Name == "Семейство1").ToList();

            Dictionary<Face, List<double>> collectionPointCloudPoint = new Dictionary<Face, List<double>>();
            Dictionary<Face, List<UV>> dictionaryFaceAndUV = new Dictionary<Face, List<UV>>();

            foreach (var face in faceList)
            {

                // равномерный шаг генерации точек
                BoundingBoxUV cylindircalParameterzs = face.GetBoundingBox();
                UV maxParameters = cylindircalParameterzs.Max;
                UV minParameters = cylindircalParameterzs.Min;
                var radius = (face.GetSurface() as CylindricalSurface).Radius;
                var lengthSigment = (maxParameters.U - minParameters.U) * radius;
                var countStep = (double)Math.Round(lengthSigment / stepMin);
                var stepSigmentAngle = (maxParameters.U - minParameters.U) / (countStep);
                var stepSigmentV = (maxParameters.V - minParameters.V) / stepMin;
                List<UV> pointUVInFace = new List<UV>();
                for (int i = 0; i <= (int)(lengthSigment / stepMin); i++)
                {
                    for (int j = 0; j <= (int)stepSigmentV; j++)
                    {
                        var Upoint = minParameters.U + stepSigmentAngle * i;
                        var Vpoint = minParameters.V + stepMin * j;
                        UV point = new UV(Upoint, Vpoint);
                        pointUVInFace.Add(point);
                    }
                }

                //шаг через семейство 

                //List<UV> pointUVInFace = new List<UV>();
                //foreach (var element in elementSet)
                //{
                //    var asd = element as FamilyInstance;
                //    if (asd.HostFace == null) continue;
                //    Reference refElement = (element as FamilyInstance).HostFace;
                //    var projectPoint = face.Project((element as Instance).GetTotalTransform().Origin);
                //    if (projectPoint == null) continue;
                //    bool isInsidePoint = face.IsInside(projectPoint.UVPoint);
                //    if (isInsidePoint == true) pointUVInFace.Add(projectPoint.UVPoint);
                //}
                dictionaryFaceAndUV.Add(face,pointUVInFace);
            }
            //TODO поработать над пересписком, так как количество исследованных точек может быть 0
            foreach (var value in dictionaryFaceAndUV)
            {
                List<double> listValuesForReport = new List<double>();
                BoundingBoxUV cylindircalParameterzs = value.Key.GetBoundingBox();
                UV maxParameters = cylindircalParameterzs.Max;
                UV minParameters = cylindircalParameterzs.Min;
                var radiusR = (value.Key.GetSurface() as CylindricalSurface).Radius;
                var lengthSigment = (maxParameters.U - minParameters.U) * radiusR;
                var countStep = (double)Math.Round(lengthSigment / stepMin);
                var stepSigmentAngle = (maxParameters.U - minParameters.U) / (countStep);
                foreach (var pointUV in value.Value)
                {
                    XYZ point1 = value.Key.Evaluate(pointUV);
                    CylindricalFace getFace = value.Key as CylindricalFace;
                    XYZ normalFaceEnd2 = getFace.Axis.Normalize() * pointUV.V + getFace.Origin;
                    XYZ normalToFace = point1 - normalFaceEnd2;
                    Plane startPlane = Plane.CreateByNormalAndOrigin(normalToFace, point1);
                    BoundingBoxUV bbToTriangle = startPlane.GetBoundingBoxUV();
                    UV minTriangle = bbToTriangle.Min;
                    IList<Plane> faceToPlane = new List<Plane>();

                    #region Работа с треугольником

                    UV pointCentre = new UV();
                    startPlane.Project(point1, out pointCentre, out double distance);

                    UV pointB = new UV(pointCentre.U, pointCentre.V + hight * 0.5);

                    // косяк с захватом плоскости 
                    UV pointA = new UV(pointB.U - stepSigmentAngle / 2, pointB.V - hight);
                    UV pointC = new UV(pointA.U + stepSigmentAngle, pointA.V);
                    XYZ pointBXYZ = startPlane.Origin + pointB.U * startPlane.XVec + pointB.V * startPlane.YVec;
                    XYZ pointAXYZ = startPlane.Origin + pointA.U * startPlane.XVec + pointA.V * startPlane.YVec;
                    XYZ pointCXYZ = startPlane.Origin + pointC.U * startPlane.XVec + pointC.V * startPlane.YVec;
                    XYZ centreStartPlane = startPlane.Origin + pointCentre.U * startPlane.XVec + pointCentre.V * startPlane.YVec;
                    List<XYZ> triangleStart = new List<XYZ>() { pointBXYZ, pointAXYZ, pointCXYZ };

                    #endregion

                    #region Сдвигаем треугольник наверх

                    XYZ pointBXYZMax = pointBXYZ + (normalToFace).Normalize() * captureRange;
                    XYZ pointAXYZMax = pointAXYZ + (normalToFace).Normalize() * captureRange;
                    XYZ pointCXYZMax = pointCXYZ + (normalToFace).Normalize() * captureRange;

                    Plane maxHightTriangle = Plane.CreateByThreePoints(pointBXYZMax, pointAXYZMax, pointCXYZMax);
                    List<XYZ> triangleMaxHight = new List<XYZ>()
                        { pointBXYZMax, pointAXYZMax, pointCXYZMax, pointBXYZMax };

                    XYZ pointBXYZMin = pointBXYZ + (-1) * (normalToFace).Normalize() * captureRange;
                    XYZ pointAXYZMin = pointAXYZ + (-1) * (normalToFace).Normalize() * captureRange;
                    XYZ pointCXYZMin = pointCXYZ + (-1) * (normalToFace).Normalize() * captureRange;

                    Plane minHightTriangle = Plane.CreateByThreePoints(pointBXYZMin, pointAXYZMin, pointCXYZMin);
                    List<XYZ> triangleMinHight = new List<XYZ>()
                        { pointBXYZMin, pointAXYZMin, pointCXYZMin, pointBXYZMin };

                    #endregion

                    for (int i = 0; i < triangleMaxHight.Count() - 1; i++)
                    {
                        Plane sidePlane = Plane.CreateByThreePoints(triangleMinHight[i], triangleMinHight[i + 1],
                            triangleMaxHight[i]);
                        faceToPlane.Add(sidePlane);
                    }

                    IList<Plane> reversList = new List<Plane>();
                    foreach (var plane in faceToPlane)
                    {
                        XYZ reversNormal = plane.Origin - centreStartPlane;
                        Plane reversPlane = Plane.CreateByNormalAndOrigin(reversNormal * (-1), plane.Origin);
                        reversList.Add(reversPlane);
                    }

                    Plane reversMaxPlane =
                        Plane.CreateByNormalAndOrigin((maxHightTriangle.Normal * (-1)), maxHightTriangle.Origin);
                    reversList.Add(minHightTriangle);
                    reversList.Add(reversMaxPlane);
                    PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(reversList);
                    Autodesk.Revit.DB.PointClouds.PointCollection pointCollection =
                        pcInstance.GetPoints(filter, 0.0001, 4000); // как-будто надо увеличить количество точек
                    var variable = pointCollection.Select(x => pointTransform.OfPoint((XYZ)x)).ToList();
                    variable.TrimExcess();
                    double xPredict;
                    double yPredict;
                    //предсказание не выполняется правильно, надо подумать 
                    //if (variable.Count > 10)
                    //{
                        AproximateXZ(variable, out double a, out double b);
                        AproximateYZ(variable, out double c, out double d);
                        //TODO подумать над функцией ниже 
                        xPredict = a * point1.Z + b;
                        yPredict = c * point1.Z + d;
                        if (xPredict.Equals(Double.NaN) || yPredict.Equals(Double.NaN))
                        {
                            xPredict = point1.X;
                            yPredict = point1.Y;
                        }
                        XYZ aproximatePoint = new XYZ(xPredict, yPredict, point1.Z);
                        double length = (aproximatePoint - normalFaceEnd2).GetLength();
                        double radius = getFace.get_Radius(1).GetLength();
                        double resultValueAnalysis = length - radius;
                        if (resultValueAnalysis > 1)
                        {

                        }
                    //    if (Math.Abs(point1.X - xPredict) >= captureRange)
                    //    {
                    //        xPredict = 0;
                    //    }
                    //    if(Math.Abs(point1.Y - yPredict) >= captureRange)
                    //    {
                    //        yPredict = 0;
                    //    }
                    //}
                    //else
                    //{
                    //    xPredict = point1.X;
                    //    yPredict = point1.Y;
                    //}
                    listValuesForReport.Add(resultValueAnalysis);
                    //}
                }
                collectionPointCloudPoint.Add(value.Key, listValuesForReport);
            }

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
                UV outUVmax = null;
                // TODO можно проще сделать
                foreach (var toFace in dictionaryFaceAndUV)
                {
                    //var maxValue = collectionPointCloudPoint.OrderByDescending(x => x.Value).First();
                    //var minValue = collectionPointCloudPoint.OrderBy(x => x.Value).First();
                    if (toFace.Key is CylindricalFace)
                    {
                        // плохо работает equals (надо сделать equals через свойства Face)
                        var dictionaryItemCollectionPoint = collectionPointCloudPoint.Where(x => x.Key.Id==toFace.Key.Id).Select(x => x.Value).First();
                        AnalysisResultSchema resultSchema = new AnalysisResultSchema("Диаграмма плоскости" + " " + i.ToString(), "Цветная схема перепада отметок");
                        if (i == 0)
                        {
                            resultSchema.AnalysisDisplayStyleId = analysisDisplay?.First().Id ?? null;
                        }
                        else
                        {
                            resultSchema.AnalysisDisplayStyleId = analysisDisplayHide?.First().Id ?? null;
                        }

                        List<UV> listUVcoord = toFace.Value;
                        int currentIndex= 0;
                        foreach (var UVpoint in listUVcoord)
                        {
                            
                            var resultAnalysisLength = dictionaryItemCollectionPoint[currentIndex];
                            List<double> doubleList = new List<double>();
                            UV outUV = null;
                            double distance;
                            uvpts.Add(UVpoint);
                            if (resultAnalysisLength * 304.8 > 1000)
                            {
                            }


                            doubleList.Add(resultAnalysisLength * 304.8);
                            valList.Add(new ValueAtPoint(doubleList));
                            doubleList.Clear();
                            currentIndex++;
                        }
                        // тут надо подумать
                        //toFace.Key.GetSurface().Project(maxValue.Key, out outUVmax, out double distanceMax);
                        //uvpts.Add(outUVmax);
                        //valList.Add(new ValueAtPoint(new List<double>() { maxValue.Value }));
                        //toFace.Key.GetSurface().Project(maxValue.Key, out outUVmax, out double distanceMin);
                        //uvpts.Add(outUVmax);
                        //valList.Add(new ValueAtPoint(new List<double>() { minValue.Value }));

                        //опять получаю одну и туже ссылку на разные плокости
                        i++;
                        //if (i == 100) break;
                        Reference reference = toFace.Key.Reference;
                        //Reference reference = toFace.Key.Reference;
                        int idx = spf.AddSpatialFieldPrimitive(reference,SpatialFieldPrimitiveHideMode.HideWholeElement);
                        FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvpts);
                        FieldValues vals = new FieldValues(valList);
                        int schemaIndex = spf.RegisterResult(resultSchema);
                        spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
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
            return Result.Succeeded;
        }

        public void AproximateXZ(List<XYZ> pointCollection, out double a, out double b)
        {
            a = 0;
            b = 0;
            double a1 = 0, a2 = 0, a3 = 0, sumx = 0, sumz = 0;
            foreach (var point in pointCollection)
            {
                a1 = a1 + point.X * point.Z;
                sumx = sumx + point.X;
                sumz = sumz + point.Z;
                a2 = a2 + Math.Pow(point.Z, 2);
            }
            a3 = Math.Pow(sumz, 2);
            double a4 = (pointCollection.Count * a1 - sumx * sumz);
            a = (Math.Round((pointCollection.Count * a1 - sumx * sumz), 9) / (Math.Round((pointCollection.Count * a2 - a3), 9)));
            b = Math.Round((a2 * sumx - sumz * a1), 9) / Math.Round((pointCollection.Count * a2 - a3), 9);
        }
        public void AproximateYZ(List<XYZ> pointCollection, out double c, out double d)
        {
            c = 0;
            d = 0;
            double a1 = 0, a2 = 0, a3 = 0, sumz = 0, sumy = 0;
            foreach (var point in pointCollection)
            {
                a1 = a1 + point.Y * point.Z;
                sumy = sumy + point.Y;
                sumz = sumz + point.Z;
                a2 = a2 + Math.Pow(point.Z, 2);
            }
            a3 = Math.Pow(sumz, 2);
            double a4 = (pointCollection.Count * a1 - sumz * sumy);
            c = (Math.Round((pointCollection.Count * a1 - sumz * sumy), 9) / (Math.Round((pointCollection.Count * a2 - a3), 9)));
            d = Math.Round((a2 * sumy - sumz * a1), 9) / Math.Round((pointCollection.Count * a2 - a3), 9);
        }
    }
}