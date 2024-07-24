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
using System.Linq;
using Autodesk.Revit.DB.PointClouds;
using BMCIModule.Models;

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
            ICollection<Point> pointRevit= GeneratePointCollection(topFace,ref stepMin);
            ICollection<Point> collection = IsInFigureWithBorders(pointRevit, topFace,ref stepMin);

            // теперь нужно перебрать все точки по HEX и начать проверку - потом перенос + точка супер-позиции
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
                    /*foreach (XYZ point in collection)
                    {
                        slabFloor.DrawPoint(point);
                    }*/
                    double hight = (Math.Sqrt(3)) * stepMin / 2.0d;
                    SlabShapeVertexArray vertexArray = slabFloor.SlabShapeVertices;
                    Dictionary<int, SlabShapeCreaseArray> tableHashSet = new Dictionary<int, SlabShapeCreaseArray>();
                    foreach (SlabShapeVertex vertex in vertexArray)
                    {
                        int count = 0;
                        
                        foreach (var vertexCurrentArray in vertexArray)
                        {
                            var vertexCurrent = vertexCurrentArray as SlabShapeVertex;
                            //5 мм погрешность по поиску соседней точке 
                            if ((Math.Abs(vertex.Position.X - vertexCurrent.Position.X)<=stepMin+0.016 || Math.Abs(vertex.Position.X - vertexCurrent.Position.X)<=stepMin - 0.016)
                                &&((Math.Abs(vertex.Position.Y - vertexCurrent.Position.Y) <= hight + 0.016 || Math.Abs(vertex.Position.Y - vertexCurrent.Position.Y) <= hight - 0.016 || vertex.Position.Y==vertexCurrent.Position.Y))) 
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
                Stopwatch sw = new Stopwatch(); // чтобы увидеть время выполнения транзакции
                sw.Start();
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
                IList<List<XYZ>> collectionPointCollection = new List<List<XYZ>>();
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
                    Transform pointTransform = pcInstance.GetTotalTransform();

                    PointCollection pointCollection = pcInstance.GetPoints(filter, 0.001,500);// как-будто надо увеличить количество точек
                    var variable=pointCollection.Select(x => pointTransform.OfPoint((XYZ) x)).ToList();
                    collectionPointCollection.Add(variable);
                }
                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);

                TaskDialog.Show("Время выполнения", elapsedTime);
                
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
            XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000); // с вектором 1 , для того чтобы вытащить парраллельно оси Z 
            foreach (Face face in faceSolid)
            {
                //строковое сравнение векторов(надо немного продумать)
                if ((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString())) {topFace = face;}
            }
            return topFace;
        }


        //скорее всего мусор
        public bool VisiblePointInRegion(MeshTriangle triangle, Point point)//переписать проверку попадания в треугольник, из-за беконечной линии есть фантом точки за треугольником. 
        {
            GraphicsPath graphicsPath = new GraphicsPath();

             graphicsPath.AddLine((float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y, (float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y);//очень тупой мув из double В float перегонять
             graphicsPath.AddLine((float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y, (float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y);
             graphicsPath.AddLine((float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y, (float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y);
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }
        public ICollection<Point> IsInFigureWithBorders(ICollection<Point> points, Face topFace,ref double stepMin)
        {
            List<Point> pointReturn=new List<Point>();
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
                            point.getPointPosition = PointInFigureWithBorders.inFigure;
                            break;
                        }
                        else
                        {
                            point.getPointPosition = PointInFigureWithBorders.outFigure;
                            countMiss++;
                        }
                    }
                }
                countMiss = 0;
            }
            //points.Select(x => x.getPointPosition=ChangeStatusPoint(x,ref topFace)); Линк запрос сделал слепок, а его надо быдо бы отдельно переработать
            foreach (var point in points)
            {
                point.getPointPosition = ChangeStatusPoint(point, ref topFace);
            }
            return pointReturn;
        }

        public PointInFigureWithBorders ChangeStatusPoint(Point point, ref Face topFace)
        {
            IList<CurveLoop> curveArray = topFace.GetEdgesAsCurveLoops();
            foreach (var curveLoop in curveArray)
            {
                foreach (var curve  in curveLoop)
                {
                    XYZ pointA = curve.GetEndPoint(0);
                    XYZ pointB = curve.GetEndPoint(1);
                    double vecScalyar = (point.X - curve.GetEndPoint(0).X.DoubleToNumbers(3)) *
                                        (curve.GetEndPoint(1).Y.DoubleToNumbers(3) -
                                         curve.GetEndPoint(0).Y.DoubleToNumbers(3)) -
                                        ((curve.GetEndPoint(1).X.DoubleToNumbers(3) -
                                          curve.GetEndPoint(0).X.DoubleToNumbers(3)) *
                                         (point.Y - curve.GetEndPoint(0).Y.DoubleToNumbers(3)));
                    if (vecScalyar == 0)
                    {
                        return PointInFigureWithBorders.inBorders;
                    }
                    else
                    {
                        continue;
                    }
                }
            }

            return point.getPointPosition;
        }
        
        public ICollection<Point> GeneratePointCollection(Face topface,ref double stepmin)
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
            List<Point> pointGenerate = new List<Point>();
            double heightTriangle = (Math.Sqrt(3)) * stepmin / 2.0d;
            bool offset = false;
            for (int i = 0; i <= (int)(Math.Abs(yMax.Y - yMin.Y) / heightTriangle); i++)
            {
                for (int j = 0; j <= (int)(Math.Abs((xMax.X - xMin.X) / stepmin)); j++)
                {
                    double posX = xMin.X + stepmin * j + (offset ? stepmin / 2 : 0);
                    double posY = yMin.Y + heightTriangle * i;
                    pointGenerate.Add(new Point(posX.DoubleToNumbers(3), posY.DoubleToNumbers(3), (topface as PlanarFace).Origin.Z));
                }
                offset = !offset;
            }
            double stepMinForLinq = stepmin;
            foreach (var point in pointGenerate)
            {
                var pointCollection = pointGenerate
                    //.Where(X=>((X.X+ stepMinForLinq == point.X)||(X.X- stepMinForLinq == point.X))&&((X.Y+ heightTriangle==point.Y)||(X.Y - heightTriangle==point.Y)));
                    .Where(x => ((Math.Abs(x.X - point.X) <= stepMinForLinq + 0.01 * stepMinForLinq) ||
                                 (Math.Abs(x.X - point.X) <= stepMinForLinq - 0.01 * stepMinForLinq))
                                && ((Math.Abs(x.Y - point.Y) <= heightTriangle + 0.01 * heightTriangle) ||
                                    (Math.Abs(x.Y - point.Y) <= heightTriangle - 0.01 * heightTriangle)||(x.Y==point.Y))&&(x.Equals(point)==false));
                foreach (var pointToHex in pointCollection)
                {
                    point.pointForHex.Add(pointToHex);
                }
            }
            return pointGenerate;
        }

        public class Point
        {
            public double X { get; set ; }
            public double Y { get; set; }
            public double Z { get; set; }

            PointInFigureWithBorders GetPointPosition=PointInFigureWithBorders.notDetected;

            public PointInFigureWithBorders getPointPosition 
            {
                get
                {
                    return GetPointPosition;
                }
                set
                {
                    GetPointPosition=value;
                }

            }
            public HashSet<Point> pointForHex=new HashSet<Point>();

            public Point(double x, double y, double z)
            {
                X =x;
                Y = y;
                Z = z;
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
    public static class ExtensionDouble
    {
        public static double DoubleToNumbers(this double ob, int i)
        {
            string str=ob.ToString();
            string[] strArray=str.Split(',');
            char[] chr = strArray[1].ToCharArray(0,i);
            string mantisa = new string(chr);
            string strNew = strArray[0] + "," +mantisa;
            return Convert.ToDouble(strNew);
        }
    }
}
