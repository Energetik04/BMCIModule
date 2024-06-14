using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CreateFloorDXF:IExternalCommand // редактирование под выбор сначала топоповерхности, потом пола
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc=commandData.Application.ActiveUIDocument.Document;

            //FilteredElementCollector collectorTopography=new FilteredElementCollector(doc);

            UIDocument uidoc = new UIDocument(doc);
            using(Transaction tr=new Transaction(doc,"Редактивроание плиты")) 
            {
                tr.Start();
            TaskDialog.Show("Выбор элемента","Выберите Пол/Перекрытие");
            // хринилище выбранного элемента
            Selection choiceFloor =uidoc.Selection;
            // ссылка на объект
            Reference choiceFloorRef = choiceFloor.PickObject(ObjectType.Element, "Выбор элемента (пол/перекрытие)");
            // поиск элемента по ссылке
            Element selectFloor =doc.GetElement(choiceFloorRef);

            TaskDialog.Show("Выбор элемента", "Выберите Топоповерхность");

            Reference choiceTopographyRef = choiceFloor.PickObject(ObjectType.Element, "Выбор элемента (топоповерхность)");
            Element selectTopography=doc.GetElement(choiceTopographyRef);

            if(selectFloor != null && selectTopography!=null)
            {
                    IList<XYZ> boundaryPointSurface=(selectTopography as TopographySurface).GetBoundaryPoints(); // точки границы топоповерхности
                    IList<XYZ> interiorPoint= (selectTopography as TopographySurface).GetInteriorPoints(); //точки входящие внутрь поверхности
                    IList<XYZ> allPoints = (selectTopography as TopographySurface).GetPoints();//точки все=сумма двух листов точек выше
                    SlabShapeEditor slabFloor = (selectFloor as Floor).SlabShapeEditor;
                    slabFloor.Enable();// тут что-то не так 
                    foreach (XYZ point in boundaryPointSurface)
                    {
                        slabFloor.DrawPoint(point);
                    }
            }
            tr.Commit();
            }
            
            return Result.Succeeded;
        }

    }
}
