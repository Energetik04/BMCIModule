
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Windows;
using BMCIModule.Models.Extension;
using TaskDialog = Autodesk.Revit.UI.TaskDialog;

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

            #endregion


            //SpatialFieldManager sfm=SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            //if(sfm==null) sfm=SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView,1);

            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите поверхности (пол/крыша)");
            Selection choiceElement = uidoc.Selection;
            Reference choiceFloorRef = choiceElement.PickObject(ObjectType.Element, "Выбор элемента(пол / перекрытие)");
            Element selectElement = doc.GetElement(choiceFloorRef);
            ElementId selectElementId = selectElement.Id;

            #endregion
            Options op = new Options()
            {
                ComputeReferences = true
            };

            //найти минимум и максимум
            SlabShapeVertexArray vertexArray=null;
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

            List<Face> topFaces = ((doc.GetElement(selectElementId) as Floor).get_Geometry(op).First() as Solid).GetTopFace();
            
            //Создание типа легенды и стиля отображения
            using (Transaction tr = new Transaction(doc, "Создание легенды и типа результатов анализа"))
            {
                tr.Start();
                AnalysisDisplayStyle analysisDisplay = null;
                AnalysisDisplayStyle analysisDisplayHide = null;
                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                ICollection<Element> collection = collector1.OfClass(typeof(AnalysisDisplayStyle)).ToElements();
                var displayStyle = collection.Where(x => x.Name == "Фактические конструкции").Select(x => x);
                if (displayStyle.Count() == 0)
                {
                    AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings =
                        new AnalysisDisplayColoredSurfaceSettings();
                   
                    AnalysisDisplayColorSettings coloreSettings = new AnalysisDisplayColorSettings();
                    Color red = new Color(255, 0, 0);
                    Color blue = new Color(0, 0, 255);

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
                ICollection<Element> collection = collector1.OfClass(typeof(AnalysisDisplayStyle)).ToElements();
                var analysisDisplay = collection.Where(x => x.Name == "Фактические конструкции").Select(x => x);
                var analysisDisplayHide = collection.Where(x => x.Name == "Фактические конструкции (скрытый)").Select(x => x);
                int i = 0;
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
                    //опять получаю одну и туже ссылку на разные плокости
                    Reference reference = face.Reference;
                    int idx = spf.AddSpatialFieldPrimitive(reference);
                    IList<UV> uvpts = new List<UV>();
                    BoundingBoxUV bb = face.GetBoundingBox();
                    UV min = bb.Min;
                    UV max = bb.Max;
                    uvpts.Add(new UV(min.U, min.V));
                    uvpts.Add(new UV(max.U, max.V));
                    FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvpts);
                    List<double> doubleList = new List<double>();
                    IList<ValueAtPoint> valList = new List<ValueAtPoint>();
                    doubleList.Add(-5);
                    valList.Add(new ValueAtPoint(doubleList));
                    doubleList.Clear();
                    doubleList.Add(10);
                    valList.Add(new ValueAtPoint(doubleList));
                    FieldValues vals = new FieldValues(valList);
                    int schemaIndex = spf.RegisterResult(resultSchema);
                    spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
                    i++;
                    if (i == 10) break;
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
    }
}
