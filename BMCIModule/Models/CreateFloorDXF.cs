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
            // хранилище выбранного элемента
            Selection choiceFloor =uidoc.Selection;
            // ссылка на объект
            Reference choiceFloorRef = choiceFloor.PickObject(ObjectType.Element, "Выбор элемента (пол/перекрытие)");
            // поиск элемента по ссылке
            Element selectFloor =doc.GetElement(choiceFloorRef);

            TaskDialog.Show("Выбор элемента", "Выберите DXF файл");

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
                IList<XYZ> verticesDXF =gerMesh.Vertices;// вершиы DXF, не пугаться
                IList<XYZ> IsInterior = new List<XYZ>();
                if (selectFloor != null && selectTopography!=null)
                {
                    foreach (XYZ point in verticesDXF)
                    {
                        foreach (CurveLoop edgeSolid in edgeArraySolid)
                        {
                            if(VisiblePointInRegion(edgeSolid, point)==true) IsInterior.Add(point);
                        }
                    }

                    
                    SlabShapeEditor slabFloor = (selectFloor as Floor).SlabShapeEditor;
                    slabFloor.Enable();// тут что-то не так
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    //int count = 0;
                    foreach (XYZ point in IsInterior)
                    {
                        //count++;
                        slabFloor.DrawPoint(point);
                        //if(count==1000) break;
                    }

                    sw.Stop();
                    TimeSpan ts = sw.Elapsed;
                    string elapsedTime=string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                    TaskDialog.Show("Время выполнения", elapsedTime);
                }

                CreateAnalysisResult(selectFloor.GetAnalyticalModel(),doc);
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

        public void CreateAnalysisResult(AnalyticalModel model,Document document)
        {
            SpatialFieldManager spm=SpatialFieldManager.CreateSpatialFieldManager(document.ActiveView,1105);


        }
    }
}
