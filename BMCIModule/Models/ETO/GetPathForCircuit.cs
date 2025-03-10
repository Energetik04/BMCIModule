using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace BMCIModule.Models.ETO
{
    [Transaction(TransactionMode.Manual)]
    public class GetPathForCircuit : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            Element selectElement = null;
            ElementId selectElementId = null;

            #region SelectPathCircuit
            TaskDialog.Show("Выбор элемента", "Выберите щит, в цепях которого прописано Uнач.");
            Selection choicePC = uidoc.Selection;
            IList<Reference> choicePCRef = choicePC.PickObjects(ObjectType.Element, "Путь трассы");
            #endregion


            throw new NotImplementedException();
        }
    }
}
