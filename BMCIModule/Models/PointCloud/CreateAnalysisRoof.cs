using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

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

            //начальные параметры
            stepMin = 0.5;

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
            //var topFaceList=new List<Face>();

            //TODO: херовина с нормалью, она мождет быть приближенной к 0 и выше, т.е. отрицательной
            var planarFace = faceList.Where(x => (x is PlanarFace)).Where(x => Math.Round((x as PlanarFace).FaceNormal.Z,9) > 0)
                .Select(x => x).Cast<Face>().ToList();
            
            var cylindricalFace = faceList.Where(x => x is CylindricalFace).Select(x => (x as CylindricalFace))
                .Where(x=>Math.Round(x.Axis.X,9)>=0 && Math.Round(x.Axis.Y,9)>=0 && Math.Round(x.Axis.Z,9)>=0).Cast<Face>().ToList();
            Dictionary<Face, List<UV>> dictionaryFaceAndUV = new Dictionary<Face, List<UV>>();
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
                dictionaryFaceAndUV.Add(face,listPointFace);
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
                        List<double> doubleList = new List<double>();
                        UV outUV = null;
                        double distance;
                        uvpts.Add(UVpoint);
                        doubleList.Add(1);
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
    }

}
