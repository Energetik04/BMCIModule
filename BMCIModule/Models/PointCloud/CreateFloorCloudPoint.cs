using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using Autodesk.Revit.DB.PointClouds;
using BMCIModule.Models;
using BMCIModule.Models.Extension;
using BMCIModule.ViewModel;
using BMCIModule.Views.Window;
using POINT=BMCIModule.Models.PointCloud;

namespace BMCIModule
{
    [Transaction((TransactionMode.Manual))]
    public class CreateFloorCloudPoint:IExternalCommand
    {
        CreateFloorCloudPointVM createFloorCloudPointVm = new CreateFloorCloudPointVM();
        CreateFloorCloudPointWindow createFloorCloudPoint=new CreateFloorCloudPointWindow();

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region Document and Interface

            var app = commandData.Application;// пользовательский интерфейс
            var uidoc = app.ActiveUIDocument;// активный документ
            var doc = uidoc.Document;// сам документ
            double stepMin,captureRange;
            int numberPoint;
            #endregion
            createFloorCloudPoint.DataContext = createFloorCloudPointVm;
            createFloorCloudPointVm.Close = createFloorCloudPoint.Close;
            
            if (createFloorCloudPoint.ShowDialog() == false)
            {
                return Result.Succeeded;
            }
            else
            {
                try
                {
                    //установил свойства из Window в VM 
                    createFloorCloudPointVm.Step = double.Parse(createFloorCloudPoint.stepMinWindow.Text);
                    createFloorCloudPointVm.NumberPoint = int.Parse(createFloorCloudPoint.numberPoint.Text);
                    createFloorCloudPointVm.CaptureRange=double.Parse(createFloorCloudPoint.captureRange.Text);
                    // передал свойства в класс
                    captureRange=createFloorCloudPointVm.CaptureRange*0.003281d; // 0,003281 перевод в футы
                    stepMin = createFloorCloudPointVm.Step * 0.003281d;
                    numberPoint = createFloorCloudPointVm.NumberPoint;
                }
                catch (NullReferenceException)
                {
                    TaskDialog.Show("Ошибка ввода", "Ошибка ввода данных в поля");
                    return Result.Succeeded;
                }
            }

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
            
            // создаю сетку точек, так же их нужно проверить
            ICollection<POINT.Point> pointRevit= GeneratePointCollection(ref topFace,ref stepMin);
            ICollection<POINT.Point> collection = IsInFigureWithBordersFace(pointRevit, topFace,ref stepMin);
            MovieOutFigurePointInBorders(ref collection,ref topFace);
            #endregion

            #region Визуализация точками
            /*using (Transaction tr = new Transaction(doc,"Визуализация точками"))
            {
                tr.Start();
                foreach (POINT.Point point in pointRevit)
                {
                    //if (point.getPointPosition == PointInFigureWithBorders.deleteThisPoint)
                    //{
                    //    continue;
                    //}
                    //else
                    //{
                        //проверка на попадание точки в регион
                        XYZ convertPoint = new XYZ(point.X, point.Y, point.Z);
                        var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));// размещение обобщенной модели точки
                        directShape.SetShape(new List<GeometryObject>() { Autodesk.Revit.DB.Point.Create(convertPoint) });// размещение обощенной модели точки
                    //}
                    
                }
                tr.Commit();
            }*/
            #endregion
            
            TaskDialog.Show("Количество точек","Количество точек триангуляции попавших в верхнюю грань пола "+collection.Count.ToString()+"шт.");

            #region Построение ребер на поверхности

            Dictionary<XYZ,POINT.Point> dictionaryVertexAndPoint=new Dictionary<XYZ,POINT.Point>();
            using (Transaction tr = new Transaction(doc, "Триангуляция поверхности"))
            {
                tr.Start();
                SlabShapeEditor slabFloor = (selectFloor as Floor).SlabShapeEditor;
                //slabFloor.Enable();// открываем на редактирование
                var face=GetTopFace(selectFloor, doc);
                var list = face.GetListPointInFace();
                Stopwatch sw = new Stopwatch(); // чтобы увидеть время выполнения транзакции
                sw.Start();
                foreach (POINT.Point point in collection)
                {
                    if (point.getPointPosition == PointInFigureWithBorders.deleteThisPoint)
                    {
                        continue;
                    }
                    //XYZ convertPoint = point.CreateXYZFromPoint();
                    XYZ convertPoint = point.CreateXYZFromPoint();
                    SlabShapeVertex vertex = slabFloor.DrawPoint(convertPoint);
                    //SlabShapeVertex vertex = slabFloor.DrawPoint(convertPoint); // вернет null если точки не существует на верхней грании фигуры
                    if (vertex == null)
                    {
                        continue;
                    }
                    //if (vertex.Position.Z != convertPoint.Z)
                    //{
                    //    if(vertex.Position.Z>convertPoint.Z) slabFloor.ModifySubElement(vertex,-1*(Math.Abs(vertex.Position.Z-convertPoint.Z)));
                    //    if(vertex.Position.Z<convertPoint.Z) slabFloor.ModifySubElement(vertex,(convertPoint.Z-vertex.Position.Z));
                    //}
                    dictionaryVertexAndPoint.Add(vertex.Position,point);
                }
                double hight = (Math.Sqrt(3)) * stepMin / 2.0d;
                // хер с ним, переберем все vertex  и поставить высоту
                SlabShapeVertexArray vertexArray = slabFloor.SlabShapeVertices;
                
                List<SlabShapeVertex> vertexList=new List<SlabShapeVertex>();
                foreach (SlabShapeVertex vertex in vertexArray)
                {
                    vertexList.Add(vertex);
                }

                foreach (var vertex in vertexList)
                {
                    if (vertex.VertexType == SlabShapeVertexType.Corner)
                    {
                        POINT.Point cornerPoint = new POINT.Point(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                        cornerPoint.getPointPosition = PointInFigureWithBorders.cornerPoint;
                        dictionaryVertexAndPoint.Add(vertex.Position,cornerPoint);
                    }
                    else
                    {
                        if (vertex.VertexType == SlabShapeVertexType.Edge)
                        {
                            POINT.Point edgePoint = new POINT.Point(vertex.Position.X, vertex.Position.Y, vertex.Position.Z);
                            edgePoint.getPointPosition = PointInFigureWithBorders.inBorders;
                            dictionaryVertexAndPoint.Add(vertex.Position, edgePoint);
                        }
                        else
                        {
                            if (vertex.VertexType == SlabShapeVertexType.Interior)
                            {
                            }
                        }
                    }
                }
                Dictionary<int, SlabShapeCreaseArray> tableHashSet = new Dictionary<int, SlabShapeCreaseArray>();
                foreach (SlabShapeVertex vertex in vertexArray)
                {
                    int count = 0;
                    foreach (var vertexCurrentArray in vertexArray)
                    {
                        var vertexCurrent = vertexCurrentArray as SlabShapeVertex;
                        //5 мм погрешность по поиску соседней точке 
                        if ((Math.Abs(vertex.Position.X - vertexCurrent.Position.X) <= stepMin + 0.016 || Math.Abs(vertex.Position.X - vertexCurrent.Position.X) <= stepMin - 0.016)
                            && ((Math.Abs(vertex.Position.Y - vertexCurrent.Position.Y) <= hight + 0.016 || Math.Abs(vertex.Position.Y - vertexCurrent.Position.Y) <= hight - 0.016 || vertex.Position.Y == vertexCurrent.Position.Y)))
                        {
                            count++;
                            int sumHashCrease = vertex.GetHashCode() + vertexCurrent.GetHashCode();// очень стремный момент с суммой хэшей
                            if (!tableHashSet.ContainsKey(sumHashCrease))
                            {
                                SlabShapeCreaseArray drawSplitLine = slabFloor.DrawSplitLine(vertex, vertexCurrent);
                                tableHashSet.Add(sumHashCrease, drawSplitLine);
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
                TaskDialog.Show("Время выполнения", "Время выполнения триангуляции и построения ребер:"+"\n"+elapsedTime);
                tr.Commit();
            }
            #endregion


            using (Transaction tr2=new Transaction(doc,"Work with CloudPoint"))
            {
                tr2.Start();
                Stopwatch sw = new Stopwatch(); // чтобы увидеть время выполнения транзакции
                sw.Start();
                SlabShapeEditor shapeEditor = (selectFloor as Floor).SlabShapeEditor;
                Options op = new Options();// default setting
                Element selectorNewFloor = doc.GetElement(selectFloorId);
                List<Face> topFaceArray = new List<Face>();
                //обработка облака вне зависимости от фигуры перекрытия
                double thicknesFloor = 0;
                Dictionary<Face,List<XYZ>> collectionPointCloudInTriangle = GetDictionaryFaceAndCloudPoint(selectorNewFloor, selectPointCloud,ref doc,ref topFaceArray,numberPoint, captureRange, ref thicknesFloor);
                SlabShapeVertexArray vertexToCheck = (selectorNewFloor as Floor).SlabShapeEditor.SlabShapeVertices;
                Dictionary<Face,List<XYZ>> dictionaryFaceAndPointFace=new Dictionary<Face,List<XYZ>>(); //точки плоскость и точки ограничивающие плоскость
                foreach (var face in topFaceArray)
                {
                    List<XYZ> listPointFace=face.GetListPointInFace();
                    dictionaryFaceAndPointFace.Add(face,listPointFace);
                }
                bool allCheckPoint=false;
                int countPointTrue = 0;
                List<int> idInsertFace=new List<int>();
                Dictionary<Face,bool> fullCheckOutFace=new Dictionary<Face, bool>();
                List<double> determination=new List<double>();
                List<SlabShapeVertex> vertexList=new List<SlabShapeVertex>();
                // опять как-то надо логику посмотреть 
                while (allCheckPoint==false)
                {
                    if (countPointTrue >= dictionaryVertexAndPoint.Count)
                    {
                        allCheckPoint=true;
                    }
                    else
                    {
                        foreach (var faceAndPointInFace in dictionaryFaceAndPointFace)
                        {
                            foreach (var pointInFace in faceAndPointInFace.Value)
                            {
                                foreach (var vertexAndPoint in dictionaryVertexAndPoint)
                                {
                                    if (pointInFace.IsAlmostEqualTo(vertexAndPoint.Value.CreateXYZFromPoint()) && vertexAndPoint.Value.checkoutStatus==true)
                                    {
                                        break;
                                    }
                                    else
                                    {
                                        if (pointInFace.IsAlmostEqualTo(vertexAndPoint.Value.CreateXYZFromPoint()) &&
                                            vertexAndPoint.Value.checkoutStatus == false)
                                        {
                                            var trianglePoint = collectionPointCloudInTriangle
                                                .Where(x => x.Key.Id == faceAndPointInFace.Key.Id);
                                            foreach (SlabShapeVertex vertex in vertexToCheck)
                                            {
                                                if (vertex.Position.IsAlmostEqualTo(pointInFace) &&
                                                    collectionPointCloudInTriangle[trianglePoint.First().Key].Count != 0)
                                                {
                                                    AproximateXZ(collectionPointCloudInTriangle[trianglePoint.First().Key], out double a, out double b);
                                                    double zPredict = a * vertex.Position.X + b;

                                                    // поиск коэффициента детерминации 
                                                    double midleZ = 0;
                                                    foreach (var pointCloud in collectionPointCloudInTriangle[trianglePoint.First().Key])
                                                    {
                                                        midleZ = midleZ + pointCloud.Z;
                                                    }
                                                    midleZ = midleZ / collectionPointCloudInTriangle[trianglePoint.First().Key]
                                                                 .Count;
                                                    double sumChis = 0;
                                                    double sumZnam = 0;
                                                    foreach (var variable in collectionPointCloudInTriangle[trianglePoint.First().Key])
                                                    {
                                                        sumChis=sumChis + Math.Pow((variable.Z-midleZ),2);
                                                        sumZnam = sumZnam + Math.Pow(variable.Z -a*variable.X+b,2);
                                                    }
                                                    double oshibka = 1 - sumChis / sumZnam;
                                                    determination.Add(oshibka);
                                                    // завершил поиск коэффициента детерминации

                                                    double deltaVertexAndPredict = 0;
                                                    if (Math.Abs(vertex.Position.Z-zPredict)>=captureRange || Math.Abs(vertex.Position.Z-zPredict-thicknesFloor)>=captureRange) 
                                                    {
                                                        deltaVertexAndPredict = 0;
                                                    }
                                                    else
                                                    {
                                                        if(vertex.Position.Z<zPredict) deltaVertexAndPredict=zPredict-vertex.Position.Z;
                                                        if(vertex.Position.Z>zPredict) deltaVertexAndPredict=(-1)*Math.Abs(vertex.Position.Z-zPredict);
                                                    }
                                                    shapeEditor.ModifySubElement(vertex, deltaVertexAndPredict);
                                                    vertexAndPoint.Value.checkoutStatus = true;
                                                    vertexAndPoint.Value.X = vertex.Position.X;
                                                    vertexAndPoint.Value.Y = vertex.Position.Y;
                                                    vertexAndPoint.Value.Z = vertex.Position.Z;
                                                    vertexList.Add(vertex);
                                                    break;
                                                }
                                                else
                                                {
                                                    // тут что-то неcуразное
                                                    if (vertex.Position.IsAlmostEqualTo(pointInFace) &&
                                                        collectionPointCloudInTriangle[trianglePoint.First().Key].Count == 0)
                                                    {
                                                        vertexAndPoint.Value.checkoutStatus = true;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    foreach (var checkTrue in dictionaryVertexAndPoint)
                    {
                        if (checkTrue.Value.checkoutStatus == true)
                        {
                            countPointTrue++;
                        }
                    }
                }
                sw.Stop();
                TimeSpan ts = sw.Elapsed;
                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}:{3:00}", ts.Hours, ts.Minutes, ts.Seconds,
                    ts.Milliseconds / 10);
                TaskDialog.Show("Время выполнения", "Время выполнения изменения высот субэлементов плиты" + "\n" + elapsedTime);
                //нашел верхнюю и нижнюю точку среди vertex в плоскости пола
                var vertexMin = vertexList.OrderBy(x => x.Position.Z).Select(x=>x).First();
                var vertexMax = vertexList.OrderByDescending(x => x.Position.Z).First();
                var bendMax=new XYZ(vertexMax.Position.X + 1, vertexMax.Position.Y+1, vertexMax.Position.Z);
                var bendMin = new XYZ(vertexMin.Position.X + 1, vertexMin.Position.Y + 1, vertexMin.Position.Z);
                var bendMinEnd= new XYZ(vertexMin.Position.X + 1, vertexMin.Position.Y + 2, vertexMin.Position.Z);

                View view = doc.ActiveView;
                Reference refffa=new Reference(selectFloor);

                var dimension=doc.Create.NewSpotElevation(doc.ActiveView,choiceFloorRef,vertexMin.Position,bendMin,bendMinEnd,vertexMin.Position,true);
                tr2.Commit();
            }
            return Result.Succeeded;
        }

        public Dictionary<Face, List<XYZ>> GetDictionaryFaceAndCloudPoint(Element selectorNewFloor, Element selectPointCloud,ref Document doc,ref List<Face> topFaceArray,int numberPoint, double captureRage, ref double thicknesFloor)
        {
            //толщина пола
            var floorParameters = selectorNewFloor.Parameters;
            foreach (Parameter parameter in floorParameters)
            {
                //Высота пола
                ElementId searchElementId = new ElementId(-1001900);
                if (parameter.Id == searchElementId)
                {
                    thicknesFloor = parameter.AsDouble();
                    break;
                }
            }
            Options op = new Options();
            List<GeometryObject> list = new List<GeometryObject>();
            //
            foreach (var instance in selectorNewFloor.get_Geometry(op))
            {
                if (instance is Solid) list.Add(instance);
            }
            Solid solid = list[0] as Solid;
            FaceArray faceArray = solid.Faces;
            XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000);
            foreach (Face face in faceArray)
            {
                if ((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString())) topFaceArray.Add(face); // не доходят все точки 
            }
            
            double zMin = (topFaceArray[0] as PlanarFace).Origin.Z-thicknesFloor-captureRage;
            double zMax = (topFaceArray[0] as PlanarFace).Origin.Z + captureRage;
            //double zMin = selectPointCloud.get_BoundingBox(doc.ActiveView).Min.Z;
            //double zMax = selectPointCloud.get_BoundingBox(doc.ActiveView).Max.Z;
            PointCloudInstance pcInstance = selectPointCloud as PointCloudInstance;// можно часть данных утратить
            Dictionary<Face, List<XYZ>> collectionPointCloudInTriangle = new Dictionary<Face, List<XYZ>>();

            foreach (Face face in topFaceArray)
            {
                var listPointFace = face.GetListPointInFace();
                listPointFace.Add(listPointFace[0]);
                List<Plane> planeFace = new List<Plane>();
                // плоскости из точек ограничивающих плоскость topFace
                for (int i = 0; i < listPointFace.Count - 1; i++)
                {
                    XYZ newPointInBottomFace = new XYZ(listPointFace[i].X, listPointFace[i].Y, zMin);
                    Plane getPlaneFace = Plane.CreateByThreePoints(listPointFace[i], listPointFace[i + 1], newPointInBottomFace);
                    planeFace.Add(getPlaneFace);
                }
                IList<Plane> faceToPlane = new List<Plane>();
                foreach (Plane plane in planeFace)
                {
                    XYZ reversNormalPlane = new XYZ(plane.Normal.X, plane.Normal.Y, plane.Normal.Z);
                    Plane reversPlane = Plane.CreateByNormalAndOrigin(reversNormalPlane, plane.Origin);
                    faceToPlane.Add(reversPlane);
                }

                Plane planeMinHeight = Plane.CreateByThreePoints(
                    new XYZ(listPointFace[0].X, listPointFace[0].Y, zMin),
                    new XYZ(listPointFace[1].X, listPointFace[1].Y, zMin),
                    new XYZ(listPointFace[2].X, listPointFace[2].Y, zMin));
                Plane planeMaxHeight = Plane.CreateByThreePoints(new XYZ(listPointFace[0].X, listPointFace[0].Y, zMax),
                    new XYZ(listPointFace[1].X, listPointFace[1].Y, zMax),
                    new XYZ(listPointFace[2].X, listPointFace[2].Y, zMax));
                XYZ reversNormalMaxheight = new XYZ(planeMaxHeight.Normal.X, planeMaxHeight.Normal.Y, planeMaxHeight.Normal.Z * (-1));
                Plane reversPlaneMaxHeight = Plane.CreateByNormalAndOrigin(reversNormalMaxheight, planeMaxHeight.Origin);
                faceToPlane.Add(reversPlaneMaxHeight);
                faceToPlane.Add(planeMinHeight);
                PointCloudFilter filter = PointCloudFilterFactory.CreateMultiPlaneFilter(faceToPlane);
                Autodesk.Revit.DB.Transform pointTransform = pcInstance.GetTotalTransform();

                PointCollection pointCollection = pcInstance.GetPoints(filter, 0.0001, numberPoint);// как-будто надо увеличить количество точек
                var variable = pointCollection.Select(x => pointTransform.OfPoint((XYZ)x)).ToList()/*OrderBy(x=>x.X).ToList()*/;
                collectionPointCloudInTriangle.Add(face, variable);
            }
            return collectionPointCloudInTriangle;
        }

        public void AproximateXZ(List<XYZ> pointCollection,out double a,out double b)
        {
            //line function z=a*x+b
            //a=(a1-sumx*sumy)/(a2-a3)
            //b=(a2*sumy-sumx*a1)/a2-a3
            a = 0;
            b= 0;
            double  a1=0, a2=0, a3=0, sumx=0, sumy = 0;
            foreach (var point in pointCollection)
            {
                a1 = a1 + point.X * point.Z;
                sumx = sumx + point.X;
                sumy = sumy + point.Z;
                a2 = a2 + Math.Pow(point.X, 2);
            }
            a3 = Math.Pow(sumx, 2);
            a=(pointCollection.Count*a1-sumx*sumy)/(pointCollection.Count*a2-a3);
            b = (a2 * sumy - sumx * a1) / (pointCollection.Count * a2 - a3);
        }

        public void AproximateXY(List<XYZ> pointCollection, out double a, out double b)
        {
            //line function z=a*x+b
            //a=(a1-sumx*sumy)/(a2-a3)
            //b=(a2*sumy-sumx*a1)/a2-a3
            a = 0;
            b = 0;
            double a1 = 0, a2 = 0, a3 = 0, sumx = 0, sumy = 0;
            foreach (var point in pointCollection)
            {
                a1 = a1 + point.X * point.Y;
                sumx = sumx + point.X;
                sumy = sumy + point.Y;
                a2 = a2 + Math.Pow(point.X, 2);
            }
            a3 = Math.Pow(sumx, 2);
            a = (pointCollection.Count * a1 - sumx * sumy) / (pointCollection.Count * a2 - a3);
            b = (a2 * sumy - sumx * a1) / (pointCollection.Count * a2 - a3);
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

        //скорее всего не мусор
        public bool VisiblePointInRegion(MeshTriangle triangle, POINT.Point point)//переписать проверку попадания в треугольник, из-за беконечной линии есть фантом точки за треугольником. 
        {
            GraphicsPath graphicsPath = new GraphicsPath();

             graphicsPath.AddLine((float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y, (float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y);//очень тупой мув из double В float перегонять
             graphicsPath.AddLine((float)triangle.get_Vertex(1).X, (float)triangle.get_Vertex(1).Y, (float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y);
             graphicsPath.AddLine((float)triangle.get_Vertex(2).X, (float)triangle.get_Vertex(2).Y, (float)triangle.get_Vertex(0).X, (float)triangle.get_Vertex(0).Y);
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }
        // перегрузка метода 
        public bool VisiblePointInRegion(List<XYZ> listPointInTopFace, POINT.Point point)//переписать проверку попадания в треугольник, из-за беконечной линии есть фантом точки за треугольником. 
        {
            GraphicsPath graphicsPath = new GraphicsPath();
            listPointInTopFace.Add(listPointInTopFace[0]);
            for (int i = 0; i < listPointInTopFace.Count-1; i++)
            {
                graphicsPath.AddLine((float)listPointInTopFace[i].X, (float)listPointInTopFace[i].Y, (float)listPointInTopFace[i + 1].X, (float)listPointInTopFace[i+1].Y);
            }
            Region region = new Region(graphicsPath);
            if (region.IsVisible((float)point.X, (float)point.Y) == true) return true;
            return false;
        }

        public ICollection<POINT.Point> IsInFigureWithBordersFace(ICollection<POINT.Point> points, Face topFace, ref double stepMin)
        {
            var pointInFace = topFace.GetListPointInFace();
            foreach (var point in points)
            {
                if (VisiblePointInRegion(pointInFace, point) == true)
                {
                    point.getPointPosition = PointInFigureWithBorders.inFigure;
                    continue;
                }
                else
                {
                    point.getPointPosition = PointInFigureWithBorders.outFigure;
                }
            }
            foreach (var point in points)
            {
                point.getPointPosition = ChangeStatusPoint(point, ref topFace);
            }
            return points;
        }

        // уже не нужный метод, нету необходимости  триангулировать 
        public ICollection<POINT.Point> IsInFigureWithBorders(ICollection<POINT.Point> points, Face topFace,ref double stepMin)
        {
            var pointInFace = topFace.GetListPointInFace();
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
            foreach (var point in points)
            {
                point.getPointPosition = ChangeStatusPoint(point, ref topFace);
            }
            return points;
        }


        public PointInFigureWithBorders ChangeStatusPoint(POINT.Point point, ref Face topFace)
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
        public ICollection<POINT.Point> GeneratePointCollection(ref Face topface,ref double stepmin)
        {
            var pointTopFace = topface.GetListPointInFace(); // точки верхней грани
            XYZ xMin, yMin;
            XYZ xMax, yMax;
            //Z координата верхней грани
            xMax = new XYZ(double.MinValue, 0, pointTopFace[0].Z);
            yMax = new XYZ(0, double.MinValue, pointTopFace[0].Z);
            xMin = new XYZ(double.MaxValue, 0, pointTopFace[0].Z);
            yMin = new XYZ(0, double.MaxValue, pointTopFace[0].Z);
            //координаты минимума и максимума 
            foreach (var point in pointTopFace)
            {
                if (point.X >= xMax.X) xMax = point;
                if (point.Y >= yMax.Y) yMax = point;
                if (point.X <= xMin.X) xMin = point;
                if (point.Y <= yMin.Y) yMin = point;
            }
            List<POINT.Point> pointGenerate = new List<POINT.Point>();
            double heightTriangle = (Math.Sqrt(3)) * stepmin / 2.0d;
            bool offset = false;
            int huita= (int)(Math.Abs(yMax.Y - yMin.Y) / heightTriangle)+1;
            for (int i = 0; i <= ((int)(Math.Abs(yMax.Y - yMin.Y)/ heightTriangle))+1; i++)
            {
                for (int j = 0; j <=(int)(Math.Abs((xMax.X - xMin.X)/ stepmin))+1; j++)
                {
                    double posX = xMin.X + stepmin * j + (offset ? stepmin / 2 : 0);
                    double posY = yMin.Y + heightTriangle * i;
                    pointGenerate.Add(new POINT.Point(posX.DoubleToNumbers(3), posY.DoubleToNumbers(3), pointTopFace[0].Z));
                }
                offset = !offset;
            }
            double stepMinForLinq = stepmin;
            foreach (var point in pointGenerate)
            {
                var pointCollection = pointGenerate
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

        public void MovieOutFigurePointInBorders(ref ICollection<POINT.Point> pointCollection, ref Face topFace)
        {
            var pointTopface = topFace.GetListPointInFace();
            int countPointInFigure = 0;
            // центр масс
            foreach (var point in pointCollection)
            {
                if (point.getPointPosition == PointInFigureWithBorders.inFigure || point.getPointPosition == PointInFigureWithBorders.inBorders)
                {
                    continue;
                }
                else
                {
                    double superPositionX = 0;
                    double superPositionY = 0;
                    double superPositionZ = pointTopface[0].Z;
                    int countPointInFigureHex = 0;
                    foreach (var pointToHex in point.pointForHex)
                    {

                        if (pointToHex.getPointPosition == PointInFigureWithBorders.inFigure)
                        {
                            countPointInFigureHex++;
                            superPositionX = superPositionX + pointToHex.X;
                            superPositionY = superPositionY + pointToHex.Y;
                        }
                    }
                    if (countPointInFigureHex == 0)
                    {
                        point.getPointPosition = PointInFigureWithBorders.deleteThisPoint;
                        continue;
                    }
                    superPositionX = (superPositionX / countPointInFigureHex).DoubleToNumbers(3);
                    superPositionY = (superPositionY / countPointInFigureHex).DoubleToNumbers(3);
                    if (point.getPointPosition == PointInFigureWithBorders.outFigure)
                    {
                        XYZ origin = new XYZ(superPositionX, superPositionY, superPositionZ);
                        XYZ direction = new XYZ((point.X - origin.X), (point.Y - origin.Y), superPositionZ);
                        //метод полная херумпула, потому что непонятно что выдает
                        var line = (Line.CreateBound(point.CreateXYZFromPoint(), origin) as Curve);
                        line.MakeUnbound();
                        List<XYZ> pointCross = new List<XYZ>();
                        foreach (var curveLoop in topFace.GetEdgesAsCurveLoops())
                        {
                            foreach (var curve in curveLoop)
                            {
                                //разобрать  с результатом
                                var result = curve.Intersect(line, out IntersectionResultArray resultArray);
                                //List<double> distanceIntersectPoint=new List<double>();
                                if (result == SetComparisonResult.Overlap)
                                {
                                    foreach (var getResult in resultArray)
                                    {
                                        var getResultIntersectionResult = getResult as IntersectionResult;
                                        var intersectPoint = getResultIntersectionResult.XYZPoint;
                                        pointCross.Add(intersectPoint);
                                        if (intersectPoint == null)
                                        {
                                            continue;
                                        }
                                    }
                                }
                            }
                        }
                        XYZ convertPoint = new XYZ(point.X, point.Y, superPositionZ);
                        XYZ tempPoint = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                        foreach (var intersectPoint in pointCross)
                        {
                            if (intersectPoint.DistanceTo(convertPoint) <= convertPoint.DistanceTo(tempPoint))
                            {
                                tempPoint = intersectPoint;
                            }
                        }
                        if (pointCross.Count == 0)
                        {
                            //динамически изменить коллекцию
                            point.getPointPosition = PointInFigureWithBorders.deleteThisPoint;
                        }
                        else
                        {
                            point.X = tempPoint.X;
                            point.Y = tempPoint.Y;
                            point.Z = superPositionZ;
                            point.getPointPosition = PointInFigureWithBorders.moviePointFromOutFigure;
                        }
                    }
                }
            }
        }
    }
}
