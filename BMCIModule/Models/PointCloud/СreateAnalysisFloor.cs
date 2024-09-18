
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
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

            //FaceArray faceArray = ((selectFloor as Floor).get_Geometry(op).First() as Solid).Faces;
            //var topFaceArray = (selectFloor as Floor).GetListTopFaceArray(doc);
            //foreach (Face face in topFaceArray)
            //{
            //    Reference reference = face.Reference;
            //    int idx = sfm.AddSpatialFieldPrimitive(reference);// индекс емкости массива данных точки 
            //    IList<UV> uvPts = new List<UV>();
            //    BoundingBoxUV bb = face.GetBoundingBox();
            //    UV min=bb.Min;
            //    UV max=bb.Max;
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
            //}



            return Result.Succeeded;
        }
    }
}
