
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
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

            List<Face> topFaces = ((doc.GetElement(selectElementId) as Floor).get_Geometry(op).First() as Solid).GetTopFace();
            // три строчки мусора
            bool analyticalModel=selectElement.CanHaveAnalyticalModel();
            if (analyticalModel == true && analyticalModel != null)
            {
                AnalyticalModel model = selectElement.GetAnalyticalModel();

            }
            //Создание типа легенды и стиля отображения
            using (Transaction tr = new Transaction(doc, "что-то"))
            {
                tr.Start();
                AnalysisDisplayStyle analysisDisplay = null;
                FilteredElementCollector collector1 = new FilteredElementCollector(doc);
                ICollection<Element> collection = collector1.OfClass(typeof(AnalysisDisplayStyle)).ToElements();
                var displayStyle = collection.Where(x => x.Name == "Display Style 1").Select(x => x);
                if (displayStyle.Count() == 0)
                {
                    AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings =
                        new AnalysisDisplayColoredSurfaceSettings();
                    coloredSurfaceSettings.ShowGridLines = true;
                    AnalysisDisplayColorSettings coloreSettings = new AnalysisDisplayColorSettings();
                    Color orange = new Color(255, 205, 0);
                    Color purple = new Color(200, 0, 200);
                    coloreSettings.MaxColor = orange;
                    coloreSettings.MinColor = purple;

                    AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings();
                    legendSettings.NumberOfSteps = 10;
                    legendSettings.Rounding = 0.05;
                    legendSettings.ShowDataDescription = false;
                    legendSettings.ShowLegend = true;

                    FilteredElementCollector collector2 = new FilteredElementCollector(doc);
                    ICollection<Element> elementCollection = collector2.OfClass(typeof(TextNoteType)).ToElements();
                    var textElements = collector2.Where(x => x.Name == "LegendText").Select(x => x);
                    if (textElements.Count() > 0)
                    {
                        TextNoteType textType = textElements.Cast<TextNoteType>().ElementAt<TextNoteType>(0);
                    }

                    analysisDisplay = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, "Display Style 1",
                        coloredSurfaceSettings, coloreSettings, legendSettings);
                }
                else
                {
                    analysisDisplay = displayStyle.Cast<AnalysisDisplayStyle>().ElementAt<AnalysisDisplayStyle>(0);
                }

                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplay.Id;
                //    int idx = sfm.AddSpatialFieldPrimitive(reference);// индекс емкости массива данных точки 
                //    IList<UV> uvPts = new List<UV>();
                //    BoundingBoxUV bb = face.GetBoundingBox();

                //    uvPts.Add(new UV(min.U,min.V));
                //    uvPts.Add(new UV(max.U,max.V));
                //    FieldDomainPointsByUV pnts = new FieldDomainPointsByUV(uvPts);
                //    List<double> doubeList=new List<double>();
                //    IList<ValueAtPoint> valList=new List<ValueAtPoint>();
                //    doubeList.Add(0);
                //    valList.Add(new ValueAtPoint(doubeList));
                //    doubeList.Clear();
                //    doubeList.Add(10);
                //    valList.Add(new ValueAtPoint(doubeList));
                //    FieldValues vals = new FieldValues(valList);
                //    AnalysisResultSchema resultSchema = new AnalysisResultSchema("Schema Name", "Description");
                //    int schemaIndex = sfm.RegisterResult(resultSchema);
                //    sfm.UpdateSpatialFieldPrimitive(idx,pnts,vals,schemaIndex);
                tr.Commit();
            }

            using (Transaction tr1=new Transaction(doc,"чего-то"))
            {
                tr1.Start();
                SpatialFieldManager spf = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
                if (spf == null) spf = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
                int i = 0;
               
                foreach (var face in topFaces)
                {
                    AnalysisResultSchema resultSchema = new AnalysisResultSchema("Display Style 1", "opisanie");
                    //опять получаю одну и туже ссылку на разные плокости
                    Reference reference = (face as Face).Reference;
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
                    doubleList.Add(0);
                    valList.Add(new ValueAtPoint(doubleList));
                    doubleList.Clear();
                    doubleList.Add(10);
                    valList.Add(new ValueAtPoint(doubleList));
                    FieldValues vals = new FieldValues(valList);
                    AnalysisResultSchema result = new AnalysisResultSchema(resultSchema);
                    int schemaIndex = spf.RegisterResult(result);
                    spf.UpdateSpatialFieldPrimitive(idx, pnts, vals, schemaIndex);
                    i++;
                    if (i == 10) break;
                }
                tr1.Commit();
            }
            return Result.Succeeded;
        }
    }
}
