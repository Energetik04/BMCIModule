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
using System.Windows.Media;

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


            #endregion
            /*
            #region SelectPointCloud

            TaskDialog.Show("Выбор элемента", "Выберите облако точек для построения пола");
            Selection choicePointCloud = uidoc.Selection;
            Reference choicePointCloudRef = choicePointCloud.PickObject(ObjectType.Element, "Выбор элемента");
            Element selectPointCloud = doc.GetElement(choicePointCloudRef);
            
            #endregion
            */

            //Получить Верхнюю грань
            Face topFace=GetTopFace(selectFloor, doc);
            #region Триангуляция

            double stepMin = 3.28d;
            // создаю сетку точек, так же их нужно проверить
            ICollection<XYZ> pointRevit=GeneratePointInSurface(topFace,stepMin);
            ICollection<XYZ> collection = PointInFigure(pointRevit, topFace);

            #endregion
            using (Transaction tr = new Transaction(doc,"поехали"))
            {
                tr.Start();
                foreach (XYZ point in collection)
                {
                    //проверка на попадание точки в регион

                    var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));// размещение обобщенной модели точки 
                    directShape.SetShape(new List<GeometryObject>(){ Autodesk.Revit.DB.Point.Create(point) });// размещение обощенной модели точки 
                }
                tr.Commit();
            }

            TaskDialog.Show("Количяество точек",collection.Count.ToString());
            using (Transaction tr = new Transaction(doc, "поехали в плиту"))
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
                    //
                    SlabShapeVertexArray vertexArray = slabFloor.SlabShapeVertices;
                    /*SlabShapeVertexArrayIterator arrayIterator = vertexArray.ForwardIterator();
                    foreach (SlabShapeVertex vertex in vertexArray)
                    {
                        while (arrayIterator.Current!=null)
                        {
                            if ((vertex.Position.X == (arrayIterator.Current as SlabShapeVertex).Position.X + stepMin)|| (vertex.Position.Y == (arrayIterator.Current as SlabShapeVertex).Position.Y + stepMin/2))
                            {
                                slabFloor.DrawSplitLine((arrayIterator.Current as SlabShapeVertex), vertex);
                                arrayIterator.MoveNext();
                            }
                            else
                            {
                                arrayIterator.MoveNext();
                            }
                        }
                    }*/
                    
                        //
                    sw.Stop();
                    TimeSpan ts = sw.Elapsed;
                    string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    TaskDialog.Show("Время выполнения", elapsedTime);
                tr.Commit();
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

        /*public bool VisiblePointInRegion(MeshTriangle triangle, XYZ point)// с треугольником пока не получается
        {
            var t1 = (triangle.get_Vertex(0).X - point.X) * (triangle.get_Vertex(0).Y - point.Y) - (triangle.get_Vertex(1).X - point.X) * (triangle.get_Vertex(1).Y - point.Y);
            var t2 = (triangle.get_Vertex(1).X - point.X) * (triangle.get_Vertex(1).Y - point.Y) - (triangle.get_Vertex(2).X - point.X) * (triangle.get_Vertex(2).Y - point.Y);
            var t3 = (triangle.get_Vertex(2).X - point.X) * (triangle.get_Vertex(2).Y - point.Y) - (triangle.get_Vertex(0).X - point.X) * (triangle.get_Vertex(0).Y - point.Y);
            var t12= (triangle.get_Vertex(0).X - triangle.get_Vertex(2).X) * (triangle.get_Vertex(0).Y - triangle.get_Vertex(2).Y) - (triangle.get_Vertex(1).X - triangle.get_Vertex(2).X) * (triangle.get_Vertex(1).Y - triangle.get_Vertex(2).Y);
            var t22= (triangle.get_Vertex(1).X - triangle.get_Vertex(0).X) * (triangle.get_Vertex(1).Y - triangle.get_Vertex(0).Y) - (triangle.get_Vertex(2).X - triangle.get_Vertex(0).X) * (triangle.get_Vertex(2).Y - triangle.get_Vertex(0).Y);
            var t32= (triangle.get_Vertex(2).X - triangle.get_Vertex(1).X) * (triangle.get_Vertex(2).Y - triangle.get_Vertex(1).Y) - (triangle.get_Vertex(0).X - triangle.get_Vertex(1).X) * (triangle.get_Vertex(0).Y - triangle.get_Vertex(1).Y);

            if ((t1>0&&t12>0)||(t1<0&&t12<0))
            {
                if ((t2 > 0 && t22 > 0) || (t2 < 0 && t22 < 0))
                {
                    if ((t3 > 0 && t32 > 0) || (t3 < 0 && t32 < 0))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

        }*/

        
        public ICollection<XYZ> PointInFigure(ICollection<XYZ> points, Face topFace)
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
                    double posX = xMin.X + stepMin*j + (offset ? stepMin / 2 : 0);
                    double posY = yMin.Y + heightTriangle*i;
                    pointGenerate.Add(new XYZ(posX,posY,(topface as PlanarFace).Origin.Z));
                }
                offset=!offset;
            }
            return pointGenerate;
        }

        public ICollection<XYZ> GeneratePointCollection(Face topface)
        {
            ICollection<XYZ> pointCurve = new Collection<XYZ>();
            double stepMin=1.25d;
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
            double heightTriangle = (Math.Sqrt(3)) * stepMin / 2.0d;
            bool offset = false;

            for (int i = 0; i <= (int)(Math.Abs(yMax.Y - yMin.Y) / heightTriangle); i++)
            {
                for (int j = 0; j <= (int)(Math.Abs((xMax.X - xMin.X) / stepMin)); j++)
                {
                    double posX = xMin.X + stepMin * j + (offset ? stepMin / 2 : 0);
                    double posY = yMin.Y + heightTriangle * i;
                    pointGenerate.Add(new XYZ(posX, posY, (topface as PlanarFace).Origin.Z));
                }
                offset = !offset;
            }

            return pointGenerate;
        }

        public void isInFigureWithBorders(){}
    }
    // почистить класс
    /*public class Point:XYZ // переписать поинт, сделать наследуемым от XYZ и расписать по теории графов 
    {
        

        public Point() { }


        public Point(double x, double y, double z)
        {
            X= x;
            Y= y;
            Z= z;
        }
    }*/
}
