using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using Microsoft.Identity.Client;
using Point = Autodesk.Revit.DB.Point;

namespace BMCIModule
{
    [Transaction((TransactionMode.Manual))]
    public class CreateFloorCloudPoint:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var app=commandData.Application;// пользовательский интерфейс
            var uidoc = app.ActiveUIDocument;// активный документ
            var doc=uidoc.Document;// сам документ

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
            Mesh triangulate = topFace.Triangulate(1);// триангулировал, получил сетку треугольников грубых
            int numberTriangle = triangulate.NumTriangles;
            // проверить точки входящие в треугольник
            GeneratePointInSurface(topFace, triangulate);

            #endregion



            //Проверить в какую сторону вращается вектора


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
            FaceArray faceSolid = solid.Faces;
            Face topFace = null;
            XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000); // с ветором 1 , для того чтобы вытащить парраллельно оси Z 
            foreach (Face face in faceSolid)
            {
                if ((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString())) //строковое сравнение векторов(надо немного продумать)
                {
                    topFace = face;
                }
            }
            return topFace;
        }

        public bool GetDirection(Face topFace)
        {
            IList<CurveLoop> loop = new List<CurveLoop>(topFace.GetEdgesAsCurveLoops());
            foreach (var line in loop)
            {
                
            }

            return true;
        }

        public bool VisiblePointInRegion(MeshTriangle triangle, XYZ point)
        {
            IList<XYZ> pointLine = new List<XYZ>();
            GraphicsPath graphicsPath = new GraphicsPath();

             graphicsPath.AddLine((float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y, (float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y);
             graphicsPath.AddLine((float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y, (float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y);
             graphicsPath.AddLine((float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y, (float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y);
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }
        public bool VisiblePointInRegion(CurveLoop edgeSolid, Point point)
        {
            IList<XYZ> pointLine = new List<XYZ>();
            GraphicsPath graphicsPath = new GraphicsPath();
            foreach (var line in edgeSolid)
            {
                graphicsPath.AddLine((float)line.GetEndPoint(0).X, (float)line.GetEndPoint(0).Y, (float)line.GetEndPoint(1).X, (float)line.GetEndPoint(1).Y);
            }
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }
        public bool VisiblePointInRegion(MeshTriangle triangle, Point point)
        {
            IList<XYZ> pointLine = new List<XYZ>();
            GraphicsPath graphicsPath = new GraphicsPath();

            graphicsPath.AddLine((float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y, (float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y);
            graphicsPath.AddLine((float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y, (float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y);
            graphicsPath.AddLine((float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y, (float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y);
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }
        public void GeneratePointInSurface(Face topface,Mesh triangulate)
        {
            HashSet<XYZ> pointCurve=new HashSet<XYZ>();
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
            
            const double stepMin=0.65d; // шаг заполнения точками
            /*
            var amountStepX=Math.Abs((xMax.X-xMin.X)/stepMin);
            var amountStepY=Math.Abs((yMax.Y-yMin.Y)/stepMin);

            ICollection<XYZ> points = new List<XYZ>();
            for (int i = 0; i < amountStepX;i++)
            {
                XYZ pointRandom;
                for (int j = 0; j < amountStepY; j++)
                {
                    pointRandom = new XYZ(xMin.X + stepMin * i, yMin.Y + stepMin * j, 0);
                    points.Add(pointRandom);
                }
            }
            foreach (var point in points)
            {
                VisiblePointInRegion(triangulate.get_Triangle(0),point);
            }
            */
            List<BMCIModule.Point> pointGenerate=new List<BMCIModule.Point>();
            double heightTriangle = Math.Sqrt(3) / 2 * stepMin;
            bool offset=false;

            for (int i=0 ; i <(int)(Math.Abs(yMax.Y-yMin.Y)/heightTriangle)+1; i++)
            {
                for (int j = 0; j <(int)(Math.Abs((xMax.X - xMin.X) / stepMin))+1 ; j++)
                {
                    double posX = xMin.X + stepMin*j - (offset ? stepMin / 2 : 0);
                    double posY = yMin.Y + heightTriangle*i;
                    pointGenerate.Add(new Point(posX,posY));
                }
                offset=!offset;
            }

            foreach (var point in pointGenerate)
            {
                ICollection<XYZ> pointRevit=new List<XYZ>();
                IList<CurveLoop> curveLoop = topface.GetEdgesAsCurveLoops();

                if (VisiblePointInRegion(curveLoop[0],point)) pointRevit.Add(new XYZ(point.X, point.Y, 0));

            }
            
        }
    }

    public class Point
    {
        public double X;
        public double Y;

        public Point(double x, double y)
        {
            X=x; Y=y;
        }
    }

    

}
