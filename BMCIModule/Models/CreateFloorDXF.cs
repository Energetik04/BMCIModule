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

            Reference choiceTopographyRef = choiceFloor.PickObject(ObjectType.Element, "Выбор элемента");
            Element selectTopography=doc.GetElement(choiceTopographyRef);

            Options op=doc.Application.Create.NewGeometryOptions();// Определить Options для объекта
            GeometryElement topFace=selectFloor.get_Geometry(op);
                List<GeometryObject> list= new List<GeometryObject>();
                foreach (GeometryObject geometryObject in topFace)// вытянул перечислением фигуры которые внутри
                {
                    if(geometryObject is Solid) list.Add(geometryObject);// потом подумать и разобраться с количеством (но тут 1 фигура)
                }
                // модумать над сложными полами и переборке этих полов 
                Solid solid = list[0] as Solid;
                Face topFaceSolid=GetTopFace(solid);
                IList<CurveLoop> edgeArraySolid = topFaceSolid.GetEdgesAsCurveLoops(); // ребра грани
                GeometryElement geo = selectTopography.get_Geometry(new Options());
                GeometryInstance geoInstance=null;
                foreach(var element in geo)
                {
                    if (element is GeometryInstance)
                    {
                        geoInstance = (GeometryInstance)element;
                    break;
                    }
                }
                GeometryElement geoInstanceInstace=geoInstance.GetInstanceGeometry();
                Mesh gerMesh=null;
                foreach(var element in geoInstanceInstace) 
                { if (element is Mesh)  gerMesh=(Mesh)element; }
                IList<XYZ> verticesDXF =gerMesh.Vertices;// вершиы вытащить получилось 



                //IList<XYZ> boundaryPointSurface = (selectTopography as TopographySurface).GetBoundaryPoints(); // точки границы топоповерхности
                //IList<XYZ> interiorPoint = (selectTopography as TopographySurface).GetInteriorPoints(); //точки входящие внутрь поверхности
                //IList<XYZ> allPoints = (selectTopography as TopographySurface).GetPoints();//точки все=сумма двух листов точек выше
                IList<XYZ> IsInterior = new List<XYZ>();
                if (selectFloor != null && selectTopography!=null)
                {
                    List<double> SquareTriangle = new List<double>();
                    foreach (XYZ point in verticesDXF)
                    {
                        XYZ pointProjection = new XYZ(point.X, point.Y, 0.000000000);// надо тут посмотреть от разных поверхностей
                        foreach (CurveLoop edgeSolid in edgeArraySolid)
                        {
                            foreach (Line lineEdge in edgeSolid)
                            {
                                if (lineEdge.IsBound == true)
                                {
                                   SquareTriangle.Add(GetTriangle(lineEdge, pointProjection));
                                }
                            }
                        }
                        double sumSquareTriangle=0;
                        for(int i = 0; i < SquareTriangle.Count; i++)
                        {
                            sumSquareTriangle+= SquareTriangle[i];
                        }
                        if (sumSquareTriangle == topFaceSolid.Area) IsInterior.Add(point);
                        SquareTriangle.Clear();
                    }
                    /*SlabShapeEditor slabFloor = (selectFloor as Floor).SlabShapeEditor;
                    slabFloor.Enable();// тут что-то не так 
                    foreach (XYZ point in boundaryPointSurface)
                    {
                        slabFloor.DrawPoint(point);
                    }*/
                }
                TaskDialog.Show("Количество точек", IsInterior.Count.ToString());
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
            pointTriangle.Add(new XYZ(edge.GetEndPoint(0).X, edge.GetEndPoint(0).Y, edge.GetEndPoint(0).Z));
            pointTriangle.Add(new XYZ(edge.GetEndPoint(1).X, edge.GetEndPoint(1).Y, edge.GetEndPoint(1).Z));
            pointTriangle.Add(point);
            XYZ pointA = pointTriangle[0];
            XYZ pointB = pointTriangle[1];
            XYZ pointC=pointTriangle[2];
            double SquareTriangle;
            SquareTriangle = (pointA.X*(pointB.Y-pointC.Y)+pointB.X*(pointC.Y-pointA.Y)+pointC.X*(pointA.Y-pointB.Y))/2;
            return SquareTriangle;
        }
    }
}
