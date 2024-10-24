using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.PointClouds;
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

            //начальные параметры
            stepMin = 0.5;
            captureRange = 2;

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
            PointCloudInstance pcInstance = selectPointCloud as PointCloudInstance;
            Autodesk.Revit.DB.Transform pointTransform = pcInstance.GetTotalTransform();

            #endregion

            Options op = new Options()
            {
                ComputeReferences = true,
                //IncludeNonVisibleObjects  = true,
                //DetailLevel = ViewDetailLevel.Fine,
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
            var planarFace = faceList.Where(x => (x is PlanarFace)).Where(x => Math.Round((x as PlanarFace).FaceNormal.Z,9) > 0)
                .Select(x => x).Cast<Face>().ToList();
            
            var cylindricalFace = faceList.Where(x => x is CylindricalFace).Select(x => (x as CylindricalFace))
                .Where(x=>Math.Round(x.Axis.X,9)>=0 && Math.Round(x.Axis.Y,9)>=0 && Math.Round(x.Axis.Z,9)>=0).Cast<Face>().ToList();

            Dictionary<Face, List<UV>> dictionaryFaceAndUV = new Dictionary<Face, List<UV>>();
            Dictionary<UV, double> collectionPointCloudPoint = new Dictionary<UV, double>();
            List<int> countVariable=new List<int>();
            Dictionary<XYZ, double> sravnitelnaya = new Dictionary<XYZ, double>();
            foreach (Face face in cylindricalFace)
            {
                BoundingBoxUV cylindircalParameterzs = face.GetBoundingBox();
                UV maxParameters = cylindircalParameterzs.Max;
                UV minParameters= cylindircalParameterzs.Min;
                var radius = (face.GetSurface() as CylindricalSurface).Radius;
                var lengthSigment = (maxParameters.U - minParameters.U) * radius;
                var stepSigmentAngle = (maxParameters.U - minParameters.U)/((int)(lengthSigment / stepMin));
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
                foreach (var pointUV in listPointFace)
                {
                    XYZ point1 = face.Evaluate(pointUV);
                    //XYZ normalFaceEnd2 = ((face as CylindricalFace).Origin +
                    //                      (face as CylindricalFace).Axis.Normalize() * pointUV.V);
                    CylindricalFace getFace=face as CylindricalFace;
                    XYZ normalFaceEnd2 = getFace.Axis.Normalize() * pointUV.V+getFace.Origin;
                    XYZ normalToFace=point1-normalFaceEnd2;

                    //UV point2UV = new UV(pointUV.U - stepSigmentAngle / 2, pointUV.V - 1.73/2 * stepMin);
                    //UV point3UV= new UV(pointUV.U + stepSigmentAngle / 2, pointUV.V - 1.73/2 * stepMin);
                    //XYZ point2 = new XYZ(point1.X - stepMin / 2, point1.Y - stepMin*1.73/2, point1.Z);
                    //XYZ point3 = new XYZ(point1.X + stepMin / 2, point2.Y, point1.Z);
                    //Plane startPlane = Plane.CreateByThreePoints();
                    Plane startPlane = Plane.CreateByNormalAndOrigin(normalToFace, point1);
                    BoundingBoxUV bbToTriangle = startPlane.GetBoundingBoxUV();
                    UV minTriangle = bbToTriangle.Min;
                    IList<Plane> faceToPlane = new List<Plane>();
                    #region Работа с треугольником

                    double hight = (Math.Sqrt(3)) * stepMin / 2.0d;
                    //UV pointB = minTriangle;
                    UV pointB=new UV();
                    startPlane.Project(point1,out pointB,out double distance);
                    UV pointA = new UV(pointB.U - stepMin / 2, pointB.V - hight);
                    UV pointC = new UV(pointA.U + stepMin, pointA.V);
                    //TODO математически не обосновано 
                    UV centre=new UV(pointA.U + stepMin/2, pointB.V - hight);
                    XYZ pointBXYZ = startPlane.Origin + pointB.U * startPlane.XVec + pointB.V * startPlane.YVec;
                    XYZ pointAXYZ = startPlane.Origin + pointA.U * startPlane.XVec + pointA.V * startPlane.YVec;
                    XYZ pointCXYZ = startPlane.Origin + pointC.U * startPlane.XVec + pointC.V * startPlane.YVec;
                    XYZ centreStartPlane = startPlane.Origin + centre.U * startPlane.XVec + centre.V * startPlane.YVec;
                    List<XYZ> triangleStart= new List<XYZ>(){pointBXYZ,pointAXYZ,pointCXYZ};

                    #endregion

                    #region Сдвигаем треугольник нааверх

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
                    faceToPlane.Add(minHightTriangle);
                    faceToPlane.Add(maxHightTriangle);
                    faceToPlane.Select(x =>
                    {
                        XYZ reversNormal = x.Origin - centreStartPlane;
                        return Plane.CreateByNormalAndOrigin(reversNormal, x.Origin);
                    });

                    //XYZ faceNormal = startPlane.Normal;
                    //XYZ directionReverseNormal = (faceNormal - startPlane.Origin)*(-1);
                    ////когда верхняя граница через p1, она и есть старт
                    //XYZ ed= directionReverseNormal.Normalize();
                    //XYZ j = captureRange * ed;
                    //XYZ buttomOrigin = captureRange * ed+startPlane.Origin;
                    //double distance = buttomOrigin.DistanceTo(startPlane.Origin);
                    //Plane minHight = Plane.CreateByNormalAndOrigin(startPlane.Normal, buttomOrigin);
                    //XYZ point1MaxHight = point1 + (faceNormal - startPlane.Origin).Normalize() * captureRange;
                    //XYZ point2MaxHight = point1 + (faceNormal - startPlane.Origin).Normalize() * captureRange;
                    //XYZ point3MaxHight = point1 + (faceNormal - startPlane.Origin).Normalize() * captureRange;
                    //Plane maxHight = Plane.CreateByThreePoints(point1MaxHight, point2MaxHight, point3MaxHight);
                    //List<XYZ> listPointMaxHight= new List<XYZ>(){point1MaxHight,point2MaxHight,point3MaxHight,point1MaxHight};
                    //List<Plane> collectionPlane = new List<Plane>();

                    //for (int i=0;i<listPointMaxHight.Count-1;i++)
                    //{
                    //    XYZ bottomPoint = new XYZ();
                    //    bottomPoint = listPointMaxHight[i] + (maxHight.Normal - maxHight.Origin).Normalize()  * captureRange;
                    //    Plane reversPlane = Plane.CreateByThreePoints(listPointMaxHight[i], listPointMaxHight[i+1],bottomPoint);
                    //    collectionPlane.Add(reversPlane);
                    //}
                    //foreach (Plane plane in collectionPlane)
                    //{
                    //    XYZ reversNormalPlane = new XYZ(plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
                    //    Plane reversPlane = Plane.CreateByNormalAndOrigin(reversNormalPlane, plane.Origin);
                    //    faceToPlane.Add(reversPlane);
                    //}
                    //faceToPlane.Add(maxHight);
                    //faceToPlane.Add(minHight);
                    PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(faceToPlane);
                    Autodesk.Revit.DB.PointClouds.PointCollection pointCollection = pcInstance.GetPoints(filter, 0.0001, 4000);// как-будто надо увеличить количество точек
                    var variable = pointCollection.Select(x => pointTransform.OfPoint((XYZ)x)).ToList();
                    countVariable.Add(variable.Count());
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
                    sravnitelnaya.Add(face.Evaluate(pointUV),zPredict);
                    collectionPointCloudPoint.Add(pointUV, zPredict);
                }
                dictionaryFaceAndUV.Add(face,listPointFace);
            }

            //#region Работа с облаком
            //Stopwatch sw1 = new Stopwatch(); // чтобы увидеть время выполнения транзакции
            //sw1.Start();
            
            //foreach (var item in dictionaryFaceAndUV)
            //{
                
            //    foreach (var pointUV in item.Value)
            //    {

            //    }
            //}



            //sw1.Stop();
            //TimeSpan ts1 = sw1.Elapsed;
            //string elapsedTime1 = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts1.Hours, ts1.Minutes, ts1.Seconds,
            //    ts1.Milliseconds / 10);
            //TaskDialog.Show("Время выполнения", "Время выполнения изменения высот субэлементов плиты" + "\n" + elapsedTime1);
            //#endregion


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
                IList<UV> uvpts = new List<UV>();
                IList<ValueAtPoint> valList = new List<ValueAtPoint>();
                // хлам для контроля значений
                List<double> resultCollection = new List<double>();
                List<double> raznicaCollection = new List<double>();
                // TODO можно проще сделать

                foreach (var dictionaryItem in dictionaryFaceAndUV)
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

                    foreach (var UVpoint in dictionaryItem.Value)
                    {
                        var zPredict = collectionPointCloudPoint.Where(x => x.Key.Equals(UVpoint)).Select(x => x.Value).First();
                        List<double> doubleList = new List<double>();
                        UV outUV = null;
                        double distance;
                        var result = 0.0;
                        
                        var zFace = dictionaryItem.Key.Evaluate(UVpoint).Z;
                        var raznica = zFace - zPredict;
                        raznicaCollection.Add(raznica);
                        if (zFace < zPredict) result = zPredict - zFace;
                        if (zFace > zPredict) result = (-1) * Math.Abs(zFace - zPredict);
                        uvpts.Add(UVpoint);
                        doubleList.Add(result*304.8);
                        resultCollection.Add(result*304.8);
                        valList.Add(new ValueAtPoint(doubleList));
                        doubleList.Clear();
                    }
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
    }

}
