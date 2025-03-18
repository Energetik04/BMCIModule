using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BMCIModule.Models.PointCloud
{
    [Transaction(TransactionMode.Manual)]
    public class CreateAnalysisFace:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите экземпляры плоскостей");
            Selection choiceElement = uidoc.Selection;
            IList<Reference> choiceElementRef = choiceElement.PickObjects(ObjectType.Face, "Выбор плоскостей");

            #endregion

            Options op = new Options()
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true,
                DetailLevel = ViewDetailLevel.Fine,
            };

            List<Element> faceList = new List<Element>();
            choiceElementRef.Select(x =>
                {
                    faceList.Add(doc.GetElement(x));
                    return x;
                }
            );
            return Result.Succeeded;
        }
    }
}