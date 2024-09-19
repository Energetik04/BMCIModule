using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using UIFramework.Widget.CustomControls;

namespace BMCIModule.Models.Extension
{
    public static class ExtensionXYZ
    {
        public static void Visualisation(this XYZ pointXYZ, Document doc)
        {
            #region Визуализация точками
            using (Transaction tr = new Transaction(doc, "Визуализация точками"))
            {
                tr.Start();
                    var directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));// размещение обобщенной модели точки
                    directShape.SetShape(new List<GeometryObject>() { Autodesk.Revit.DB.Point.Create(pointXYZ) });// размещение обощенной модели точки
                tr.Commit();
            }
            #endregion
        }
    }
}


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