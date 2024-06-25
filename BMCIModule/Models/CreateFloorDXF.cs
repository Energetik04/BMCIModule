using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.DB.Structure;
using System.Windows.Controls;

namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CreateFloorDXF:IExternalCommand // редактирование под выбор сначала топоповерхности, потом пола
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc=commandData.Application.ActiveUIDocument.Document;

            //FilteredElementCollector collectorTopography=new FilteredElementCollector(doc);

            // Интерфейс пользователя
            UIDocument uidoc = new UIDocument(doc);

            #region SelectFloor

            TaskDialog.Show("Выбор элемента", "Выберите Пол/Перекрытие");
            // хранилище выбранного элемента
            Selection choiceFloor = uidoc.Selection;
            // ссылка на объект
            Reference choiceFloorRef = choiceFloor.PickObject(ObjectType.Element, "Выбор элемента (пол/перекрытие)");
            // поиск элемента по ссылке
            Element selectFloor = doc.GetElement(choiceFloorRef);

            #endregion

            #region SelectPointCloud (Полностью переписать)

            TaskDialog.Show("Выбор элемента", "Выберите DXF файл");

            Reference choiceTopographyRef = choiceFloor.PickObject(ObjectType.Element, "Выбор элемента");
            Element selectTopography = doc.GetElement(choiceTopographyRef);

            #endregion

            using (Transaction tr=new Transaction(doc,"Редактивроание плиты")) 
            {
                tr.Start();
                // находим верхнюю грань
                Face getTopFace=GetTopFace(selectFloor, doc);

                IList<CurveLoop> edgeArraySolid = getTopFace.GetEdgesAsCurveLoops(); // ребра грани

                /* Не нужный кусок работы с DXF
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
                IList<XYZ> verticesDXF =gerMesh.Vertices;// вершиы DXF, не пугаться
                IList<XYZ> IsInterior = new List<XYZ>();
                */



                if (selectFloor != null)
                {
                    foreach (XYZ point in verticesDXF)
                    {
                        foreach (CurveLoop edgeSolid in edgeArraySolid)
                        {
                            if(VisiblePointInRegion(edgeSolid, point)==true) IsInterior.Add(point);
                        }
                    }
                    // редактирование плиты, но много времени занимает выполнение
                    SlabShapeEditor slabFloor = (selectFloor as Floor).SlabShapeEditor;
                    slabFloor.Enable();// открываем на редактирование
                    Stopwatch sw = new Stopwatch(); // чтобы увидеть время выполнения транзакции
                    sw.Start();
                    foreach (XYZ point in IsInterior)
                    {
                        slabFloor.DrawPoint(point);
                    }
                    sw.Stop();
                    TimeSpan ts = sw.Elapsed;
                    string elapsedTime=string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                    TaskDialog.Show("Время выполнения", elapsedTime);
                }
                //TaskDialog.Show("Количество точек", IsInterior.Count.ToString());
                tr.Commit();
            }
            return Result.Succeeded;
        }

        public Face GetTopFace(Element selectFloor, Document doc)
        {
            #region Options

            Options op = doc.Application.Create.NewGeometryOptions();// Определить Options для объекта заредачить
            op.ComputeReferences = false;
            op.DetailLevel = ViewDetailLevel.Undefined;
            op.IncludeNonVisibleObjects = false;

            #endregion


            GeometryElement getGeometryElement = selectFloor.get_Geometry(op);

            List<GeometryObject> list = new List<GeometryObject>();

            foreach (GeometryObject geometryObject in getGeometryElement)// вытянул перечислением фигуры которые внутри
            {
                if (geometryObject is Solid) list.Add(geometryObject);// потом подумать и разобраться с количеством
            }

            // модумать над сложными полами и переборке этих полов 
            Solid solid = list[0] as Solid;
            FaceArray faceSolid=solid.Faces;
            Face topFace=null;
            XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000); // с ветором 1 , для того чтобы вытащить парраллельно оси Z 
            foreach (Face face in faceSolid)
            {
                if((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString())) //строковое сравнение векторов(надо немного продумать)
                {
                    topFace = face;
                }
            }
            return topFace;
        }

       // пролбуме через регион
        public bool VisiblePointInRegion(CurveLoop edgeSolid, XYZ point)
        {
            IList<XYZ> pointLine = new List<XYZ>();
            GraphicsPath graphicsPath = new GraphicsPath();
            foreach (var line in edgeSolid)
            {
                graphicsPath.AddLine((float) line.GetEndPoint(0).X, (float) line.GetEndPoint(0).Y, (float) line.GetEndPoint(1).X, (float) line.GetEndPoint(1).Y);
            }
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }

        
    }
}
