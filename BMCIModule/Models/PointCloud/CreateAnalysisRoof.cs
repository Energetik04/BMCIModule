using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace BMCIModule.Models.PointCloud
{
    [Transaction(TransactionMode.Manual)]
    public class CreateAnalysisRoof : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elments)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            double stepMin, captureRange;
            int numberPoint;

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null) sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);

            #region Выбор поверзности

            TaskDialog.Show("Выбор элемента", "Выберите поверхности (пол/крыша)");
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


            return Result.Succeeded;
        }
    }

}
