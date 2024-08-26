using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

namespace BMCIModule.Models.Extension
{
    public static class ExtensionFloor
    {
        public static List<Face> GetListTopFaceArray(this Floor ob, Document doc)
        {
            #region Options

            Options op = doc.Application.Create.NewGeometryOptions();// Определить Options для объекта заредачить
            op.ComputeReferences = true;
            op.DetailLevel = ViewDetailLevel.Undefined;
            op.IncludeNonVisibleObjects = false;

            #endregion

            GeometryElement getGeometryElement = ob.get_Geometry(op);
            List<GeometryObject> list = new List<GeometryObject>();
            foreach (GeometryObject geometryObject in getGeometryElement)// вытянул перечислением фигуры которые внутри
            {
                if (geometryObject is Solid) list.Add(geometryObject);// потом подумать и разобраться с количеством
            }
            // модумать над сложными полами и переборке этих полов 
            Solid solid = list[0] as Solid;
            FaceArray faceSolid = solid.Faces;
            List<Face> topFace = new List<Face>();
            XYZ normal = new XYZ(0.000000000, 0.000000000, 1.000000000); // с вектором 1 , для того чтобы вытащить парраллельно оси Z 
            foreach (Face face in faceSolid)
            {
                if (face is PlanarFace)
                {
                    //тупая хуйня, которая работает онли с полами над 0
                    if ((face as PlanarFace).FaceNormal.ToString().Equals(normal.ToString()) || (face as PlanarFace).FaceNormal.Z > 0) { topFace.Add(face); }
                }
            }
            return topFace;
        }

    }
}
