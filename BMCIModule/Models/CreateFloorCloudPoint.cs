using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using Autodesk.Revit.DB.PointClouds;
using PointCollection = Autodesk.Revit.DB.PointClouds.PointCollection;

namespace BMCIModule
{
    [Transaction((TransactionMode.Manual))]
    public class CreateFloorCloudPoint:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region Document and Interface

            var app = commandData.Application;// пользовательский интерфейс
            var uidoc = app.ActiveUIDocument;// активный документ
            var doc = uidoc.Document;// сам документ
            
            #endregion

            #region SelectFloor

            TaskDialog.Show("Выбор элемента", "Выберите Пол/Перекрытие");
            Selection choiceFloor = uidoc.Selection;
            Reference choiceFloorRef = choiceFloor.PickObject(ObjectType.Element, "Выбор элемента(пол / перекрытие)");
            Element selectFloor = doc.GetElement(choiceFloorRef);
            ElementId selectFloorId = selectFloor.Id;


            #endregion
            
            #region SelectPointCloud

            TaskDialog.Show("Выбор элемента", "Выберите облако точек для построения пола");
            Selection choicePointCloud = uidoc.Selection;
            Reference choicePointCloudRef = choicePointCloud.PickObject(ObjectType.Element, "Выбор элемента");
            Element selectPointCloud = doc.GetElement(choicePointCloudRef);
            
            #endregion
            

            //Получить Верхнюю грань
            Face topFace=GetTopFace(selectFloor, doc);

            #region Триангуляция

            double stepMin = 1d;
            // создаю сетку точек, так же их нужно проверить
            ICollection<XYZ> pointRevit= GeneratePointCollection(topFace,ref stepMin);
            ICollection<XYZ> collection = PointInFigure(pointRevit, topFace,ref stepMin);
            // подумать крепко что делать с коллекцией точе
            #endregion

            #region Визуализация точками

            /*using (Transaction tr = new Transaction(doc,"поехали"))
            {
                tr.Start();
                foreach (XYZ point in collection)
                {
                    //проверка на попадание точки в регион

                    var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));// размещение обобщенной модели точки
                    directShape.SetShape(new List<GeometryObject>(){ Autodesk.Revit.DB.Point.Create(point) });// размещение обощенной модели точки
                }
                tr.Commit();
            }*/
            
            #endregion
            
            TaskDialog.Show("Количяество точек",collection.Count.ToString());
            using (Transaction tr = new Transaction(doc, "Триангуляция поверхности"))
            {
                tr.Start();
                    SlabShapeEditor slabFloor = (selectFloor as Floor).SlabShapeEditor;

                    slabFloor.Enable();// открываем на редактирование
                    Stopwatch sw = new Stopwatch(); // чтобы увидеть время выполнения транзакции
                    sw.Start();
                    foreach (XYZ point in collection)
                    {
                        slabFloor.DrawPoint(point);
                    }
                    double hight = (Math.Sqrt(3)) * stepMin / 2.0d;
                    SlabShapeVertexArray vertexArray = slabFloor.SlabShapeVertices;
                    Dictionary<int, SlabShapeCreaseArray> tableHashSet = new Dictionary<int, SlabShapeCreaseArray>();
                    foreach (SlabShapeVertex vertex in vertexArray)
                    {
                        int count = 0;
                        
                        foreach (var vertexCurrentArray in vertexArray)
                        {
                            var vertexCurrent = vertexCurrentArray as SlabShapeVertex;
                            if ((Math.Abs(vertex.Position.X - vertexCurrent.Position.X)<=stepMin+0.1*stepMin || Math.Abs(vertex.Position.X - vertexCurrent.Position.X)<=stepMin - 0.1 * stepMin)
                                &&((Math.Abs(vertex.Position.Y - vertexCurrent.Position.Y) <= hight + 0.1 * hight || Math.Abs(vertex.Position.Y - vertexCurrent.Position.Y) <= hight - 0.1 * hight || vertex.Position.Y==vertexCurrent.Position.Y))) 
                            {
                                count ++;
                                int sumHashCrease = vertex.GetHashCode() + vertexCurrent.GetHashCode();// очень стремный момент с суммой хэшей
                                if (!tableHashSet.ContainsKey(sumHashCrease))
                                {
                                    SlabShapeCreaseArray drawSplitLine = slabFloor.DrawSplitLine(vertex, vertexCurrent);
                                    tableHashSet.Add(sumHashCrease,drawSplitLine);
                                }
                                if (count == 6)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    sw.Stop();
                   
                    //время выполение
                    TimeSpan ts = sw.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);

                    TaskDialog.Show("Время выполнения", elapsedTime);
                tr.Commit();
            }

            using (Transaction tr2=new Transaction(doc,"Work with CloudPoint"))
            {
                tr2.Start();
                Options op = new Options();// default setting
                List<GeometryObject> list = new List<GeometryObject>();
                Element selectorNewFloor = doc.GetElement(selectFloorId);
                foreach (var instance in selectorNewFloor.get_Geometry(op))
                {
                    if (instance is Solid) list.Add(instance);
                }

                Solid solid = list[0] as Solid;
                FaceArray faceArray = solid.Faces;
                XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000);
                XYZ normalNegative = new XYZ(0.000000000, 0.000000000, -1.000000000);
                List<Face> topFaceArray = new List<Face>();
                foreach (Face face in faceArray)
                {
                    if ((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString())) topFaceArray.Add(face); // не доходят все точки 
                }
                List<Face> bottomFaceArray = new List<Face>();
                double zMin = selectPointCloud.get_BoundingBox(doc.ActiveView).Min.Z;
                double zMax=selectPointCloud.get_BoundingBox(doc.ActiveView).Max.Z;
                PointCloudInstance pcInstance = selectPointCloud as PointCloudInstance;// можно часть данных утратить
                IList<PointCollection> collectionPointCollection = new List<PointCollection>();
                foreach (Face face in topFaceArray)
                {
                    CurveLoop curve = face.GetEdgesAsCurveLoops()[0];
                    List<XYZ> pointMeshMax= new List<XYZ>();
                    List<XYZ> pointMeshMin=new List<XYZ>();
                    foreach (Line getLine in curve)
                    {
                        pointMeshMax.Add(new XYZ(getLine.GetEndPoint(0).X, getLine.GetEndPoint(0).Y, zMax));
                        pointMeshMin.Add(new XYZ(getLine.GetEndPoint(0).X, getLine.GetEndPoint(0).Y, zMin));
                    }
                    //плоскости фигуры по 3 точкам
                    Plane planeMaxHeight = Plane.CreateByThreePoints(pointMeshMax[0], pointMeshMax[1], pointMeshMax[2]);
                    Plane planeMinHeight = Plane.CreateByThreePoints(pointMeshMin[0], pointMeshMin[1], pointMeshMin[2]);
                    Plane figurebokovina1=Plane.CreateByThreePoints(pointMeshMax[0], pointMeshMax[1], pointMeshMin[0]);
                    Plane figurebokovina2 = Plane.CreateByThreePoints(pointMeshMax[1], pointMeshMax[2], pointMeshMin[1]);
                    Plane figurebokovina3 = Plane.CreateByThreePoints(pointMeshMax[2], pointMeshMax[0], pointMeshMin[2]);

                    XYZ reversNormalMaxheight =new XYZ(planeMaxHeight.Normal.X,planeMaxHeight.Normal.Y,planeMaxHeight.Normal.Z*-1);

                    XYZ reversNormalFigureBokovina1=new XYZ(figurebokovina1.Normal.X,figurebokovina1.Normal.Y,-1*figurebokovina1.Normal.Z);
                    XYZ reversNormalFigureBokovina2 = new XYZ(figurebokovina2.Normal.X, figurebokovina2.Normal.Y, -1 * figurebokovina2.Normal.Z);
                    XYZ reversNormalFigureBokovina3 = new XYZ(figurebokovina3.Normal.X, figurebokovina3.Normal.Y, -1 * figurebokovina3.Normal.Z);

                    Plane reversFigureBokovina1=Plane.CreateByNormalAndOrigin(reversNormalFigureBokovina1,figurebokovina1.Origin);
                    Plane reversFigureBokovina2 = Plane.CreateByNormalAndOrigin(reversNormalFigureBokovina2, figurebokovina2.Origin);
                    Plane reversFigureBokovina3 = Plane.CreateByNormalAndOrigin(reversNormalFigureBokovina3, figurebokovina3.Origin);

                    Plane reversPlaneMaxHeight = Plane.CreateByNormalAndOrigin(reversNormalMaxheight, planeMaxHeight.Origin);
                    IList<Plane> faceToPlane= new List<Plane>();
                    faceToPlane.Add(reversPlaneMaxHeight);
                    faceToPlane.Add(planeMinHeight);
                    faceToPlane.Add(reversFigureBokovina1);
                    faceToPlane.Add(reversFigureBokovina2);
                    faceToPlane.Add(reversFigureBokovina3);
                    PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(faceToPlane);
                    PointCollection pointCollection = pcInstance.GetPoints(filter, 0.001, 200);
                    collectionPointCollection.Add(pointCollection);
                }

                /*//нерабочая дрысня
                foreach (Face remainsFace in faceArray)
                {
                        if (topFaceArray.Contains(remainsFace))
                        {
                            continue;
                        }
                        else
                        {
                            if ((remainsFace as PlanarFace).FaceNormal.ToString().Equals(normalNegative.ToString()))
                            {
                                bottomFaceArray.Add(remainsFace);
                            }
                        }
                }
                Dictionary<Face,Face> dictionaryFace= new Dictionary<Face,Face>();
                foreach (var getTopFace in topFaceArray)
                {
                    foreach (var getBottomFace in bottomFaceArray)
                    {
                        int count = 0;
                        if ((((getTopFace as PlanarFace).Origin.X == (getBottomFace as PlanarFace).Origin.X) &&
                             ((getTopFace as PlanarFace).Origin.Y == (getBottomFace as PlanarFace).Origin.Y)))
                        {
                            var topFaceCurveLoop = getTopFace.GetEdgesAsCurveLoops();
                            var bottomFaceCurveLoop= getBottomFace.GetEdgesAsCurveLoops();
                            var topFaceGetCurveLoop = topFaceCurveLoop[0];
                            var bottomFaceGetCurveLoop = bottomFaceCurveLoop[0];
                            foreach (Line topLine in topFaceGetCurveLoop)
                            {
                                foreach (Line bottomLine in bottomFaceGetCurveLoop)
                                {
                                    if (((topLine.Origin.X==bottomLine.Origin.X))&&((topLine.Direction.X==bottomLine.Direction.X) && (topLine.Direction.Y==bottomLine.Direction.Y)))
                                    {
                                        count++;
                                    }
                                }
                                if (count == topFaceGetCurveLoop.Count())
                                {
                                    dictionaryFace.Add(getTopFace,getBottomFace);
                                }
                            }
                        }
                    }
                }*/
                // Надо словарь юзать и сопоставить значение Face и Plane
                tr2.Commit();
            }
            return Result.Succeeded;
        }

        //получение верхней грани, только если пол плоский
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
            FaceArray faceSolid = solid.Faces;
            Face topFace = null;
            XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000); // с ветором 1 , для того чтобы вытащить парраллельно оси Z 
            foreach (Face face in faceSolid)
            {
                //строковое сравнение векторов(надо немного продумать)
                if ((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString())) {topFace = face;}
            }
            return topFace;
        }

        public bool VisiblePointInRegion(MeshTriangle triangle, XYZ point)//переписать проверку попадания в треугольник, из-за беконечной линии есть фантом точки за треугольником. 
        {
            GraphicsPath graphicsPath = new GraphicsPath();

             graphicsPath.AddLine((float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y, (float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y);//очень тупой мув из double В float перегонять
             graphicsPath.AddLine((float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y, (float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y);
             graphicsPath.AddLine((float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y, (float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y);
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }

        /*public bool VisiblePointInRegion(ref Face topFace, ref XYZ point)
        {
            return false;
        }*/
        
        public ICollection<XYZ> PointInFigure(ICollection<XYZ> points, Face topFace,ref double stepMin)
        {
            List<XYZ> pointReturn=new List<XYZ>();
            Mesh trMesh = topFace.Triangulate(1);
            int numberTriangle = trMesh.NumTriangles;
            int countMiss = 0;
            foreach (var point in points)
            {
                for (int i = 0; i < numberTriangle; i++)
                {
                    if (countMiss < numberTriangle)
                    {
                        MeshTriangle triangleMEsh = trMesh.get_Triangle(i);
                        if (VisiblePointInRegion(triangleMEsh, point) == true)
                        {
                            pointReturn.Add(point);
                            break;
                        }
                        else
                        {
                            countMiss++;
                        }
                    }
                }
                countMiss = 0;
            }
            return pointReturn;
        }
        public ICollection<XYZ> GeneratePointInSurface(Face topface,double stepMin) // Высоту прописать
        {
            ICollection<XYZ> pointCurve=new Collection<XYZ>();
            XYZ xMin, yMin;
            XYZ xMax, yMax;
            xMax = new XYZ(double.MinValue, 0, 0);
            yMax = new XYZ(0, double.MinValue, 0);
            xMin = new XYZ(double.MaxValue, 0, 0);
            yMin = new XYZ(0, double.MaxValue, 0);
            foreach (var curve in topface.GetEdgesAsCurveLoops())
            {
                foreach (var line in curve)
                {
                    pointCurve.Add(line.GetEndPoint(0));
                }
            }
            //координаты минимума и максимума 
            foreach (var point in pointCurve)
            {
                if (point.X > xMax.X) xMax = point;
                if (point.Y > yMax.Y) yMax = point;
                if (point.X < xMin.X) xMin = point;
                if (point.Y < yMin.Y) yMin = point;
            }
            List<XYZ> pointGenerate=new List<XYZ>();
            double heightTriangle = (Math.Sqrt(3)) * stepMin/2.0d;
            bool offset=false;
            for (int i=0 ; i <=(int)(Math.Abs(yMax.Y-yMin.Y)/heightTriangle); i++)
            {
                for (int j = 0; j <=(int)(Math.Abs((xMax.X - xMin.X) / stepMin)); j++)
                {
                    double posX = xMin.X + stepMin*j - (offset ? stepMin / 2d : 0);
                    double posY = yMin.Y + heightTriangle*i;
                    pointGenerate.Add(new XYZ(posX,posY,(topface as PlanarFace).Origin.Z));
                }
                offset=!offset;
            }
            return pointGenerate;
        }

        public ICollection<XYZ> GeneratePointCollection(Face topface,ref double stepmin)
        {
            ICollection<XYZ> pointCurve = new Collection<XYZ>();
            XYZ xMin, yMin;
            XYZ xMax, yMax;
            xMax = new XYZ(double.MinValue, 0, 0);
            yMax = new XYZ(0, double.MinValue, 0);
            xMin = new XYZ(double.MaxValue, 0, 0);
            yMin = new XYZ(0, double.MaxValue, 0);
            foreach (var curve in topface.GetEdgesAsCurveLoops())
            {
                foreach (var line in curve)
                {
                    pointCurve.Add(line.GetEndPoint(0));
                }
            }
            //координаты минимума и максимума 
            foreach (var point in pointCurve)
            {
                if (point.X > xMax.X) xMax = point;
                if (point.Y > yMax.Y) yMax = point;
                if (point.X < xMin.X) xMin = point;
                if (point.Y < yMin.Y) yMin = point;
            }
            List<XYZ> pointGenerate = new List<XYZ>();
            double heightTriangle = (Math.Sqrt(3)) * stepmin / 2.0d;
            bool offset = false;
            for (int i = 0; i <= (int)(Math.Abs(yMax.Y - yMin.Y) / heightTriangle); i++)
            {
                for (int j = 0; j <= (int)(Math.Abs((xMax.X - xMin.X) / stepmin)); j++)
                {
                    double posX = xMin.X + stepmin * j + (offset ? stepmin / 2 : 0);
                    double posY = yMin.Y + heightTriangle * i;
                    pointGenerate.Add(new XYZ(posX, posY, (topface as PlanarFace).Origin.Z));
                }
                offset = !offset;
            }
            return pointGenerate;
        }

        public class Point : XYZ
        {
            public double X { get; set ; }
            public double Y { get; set; }
            public double Z { get; set; }
            public int _enumPoint { get; set; }
            public XYZ point { get; set; }

            public ICollection<Point> pointForHex;

            public Point(double x, double y, double z)
            {
                point=new XYZ(x, y, z);
            }
            public enum isInFigureWithBorders:int
            {
                inBorders=0,
                iNFigure=1,
                outFigure=2
            }

            //проверка попадания точек в фигуру


            public XYZ PointTransfer(XYZ transferCoordination)
            {
                XYZ transferPoint = new XYZ(transferCoordination.X,transferCoordination.Y,transferCoordination.Z);
                return transferPoint;
            }
        }

























        //попытка соединить внутрянку
        /*public Point addOrGetPoint(Dictionary<int, Point> graph, int value)
        {
            if (value == -1) return null;
            Point pointGraph;
            if (graph.TryGetValue(value,out pointGraph)) return pointGraph;
            Point addPoint = new Point(value);
            graph.Add(value,addPoint);
            return addPoint;
        }

        //генерация графа
        public Dictionary<int, Point> createGraph(int[][] graphData)
        {
            Dictionary<int, Point> graph = new Dictionary<int, Point>();
            foreach (var VARIABLE in graphData)
            {
                Point getPoint = addOrGetPoint(graph, VARIABLE[0]);
                Point adjacentPoint = addOrGetPoint(graph, VARIABLE[1]);
                if(adjacentPoint==null) continue;
                Edge edge = new Edge(adjacentPoint, VARIABLE[2]);
                getPoint.edges.Add(edge);
                adjacentPoint.parenst.Add(getPoint,edge);
            }

            return graph;
        }
    }


    public class Point:XYZ
    {
        public int value { get; set; }// ключ узла может быть любой тип данных
        public Point(double x,double y,double z)//создание узка из точки
        {
            new XYZ(x,y,z);
        }
        public HashSet<Edge> edges=new HashSet<Edge>();//ребра из узла
        public Dictionary<Point,Edge> parenst=new Dictionary<Point,Edge>();//словарь который содержит список родителей 
        public Point(int value)
        {
            this.value=value;
        }
    }

    public class Edge
    {
        public Point pointEdge;
        public int stepMin;

        public Edge(Point pointEdge, int stepMin)
        {
            this.pointEdge = pointEdge;
            this.stepMin = stepMin;
        }*/
    }
}
