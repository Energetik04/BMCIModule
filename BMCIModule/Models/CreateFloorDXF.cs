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

            Options op=doc.Application.Create.NewGeometryOptions();// Определить Options для объекта
            GeometryElement topFace=selectFloor.get_Geometry(op);
                List<GeometryObject> list= new List<GeometryObject>();
                foreach (GeometryObject geometryObject in topFace)// вытянул перечислением фигуры которые внутри
                {
                    if(geometryObject is Solid)
                    {
                        list.Add(geometryObject);// потом подумать и разобраться с количеством (но тут 1 фигура)
                    }
                    else
                    {
                        continue;
                    }
                }
                // модумать над сложными полами и переборке этих полов 
                Solid solid = list[0] as Solid;
                Face topFaceSolid=GetTopFace(solid);
                IList<CurveLoop> edgeArraySolid = topFaceSolid.GetEdgesAsCurveLoops(); // ребра грани


            if(selectFloor != null && selectTopography!=null)
            {

                    IList<XYZ> boundaryPointSurface=(selectTopography as TopographySurface).GetBoundaryPoints(); // точки границы топоповерхности
                    IList<XYZ> interiorPoint= (selectTopography as TopographySurface).GetInteriorPoints(); //точки входящие внутрь поверхности
                    IList<XYZ> allPoints = (selectTopography as TopographySurface).GetPoints();//точки все=сумма двух листов точек выше
                    foreach(XYZ point in interiorPoint)
                    {
                        foreach(CurveLoop edgeSolid in edgeArraySolid)
                        {
                            List<double> SquareTriangle = new List<double>();
                            foreach (Line lineEdge in edgeSolid)
                            {
                                if (lineEdge.IsBound == true)
                                {
                                   SquareTriangle.Add(GetTriangle(lineEdge, point));
                                }
                            }
                        }
                    }
                    
                    /*SlabShapeEditor slabFloor = (selectFloor as Floor).SlabShapeEditor;
                    slabFloor.Enable();// тут что-то не так 
                    foreach (XYZ point in boundaryPointSurface)
                    {
                        slabFloor.DrawPoint(point);
                    }*/

            }
            tr.Commit();
            }
            
            return Result.Succeeded;
        }


        public Face GetTopFace(Solid solid)
        {
            FaceArray faceSolid=solid.Faces;
            Face topFace=null;
            XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000);
            foreach (Face face in faceSolid)
            {
                
                if((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString())) //строковое сравнение векторов(надо немного продумать)
                {
                    topFace = face;
                }
            }
            return topFace;
        }

        public double GetTriangle(Line edge, XYZ point) // ошибка тут, замыкание в foreach
        {
            List<XYZ> pointTriangle= new List<XYZ>();
            foreach(XYZ getPointTriangle in pointTriangle)
            {
                pointTriangle.Add(getPointTriangle);
            }
            pointTriangle.Add(point);
            XYZ pointA = pointTriangle[0];
            XYZ pointB = pointTriangle[1];
            XYZ pointC=pointTriangle[2];

            double SquareTriangle;
            SquareTriangle = 0.5*(pointA.X*(pointB.Y-pointC.Y)+pointB.X*(pointC.Y-pointA.Y)+pointC.X*(pointA.Y-pointB.Y));
            return SquareTriangle;
        }
    }
}
