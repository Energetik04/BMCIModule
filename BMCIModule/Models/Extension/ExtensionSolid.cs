using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace BMCIModule.Models.Extension
{
    public static class ExtensionSolid
    {
        public static List<Face> GetTopFace(this Solid solid)
        {
            List<Face> topFace = new List<Face>();
            FaceArray faceArray = solid.Faces;
            foreach (Face getFace in faceArray)
            {
                if (getFace is PlanarFace)
                {
                    topFace.Add(getFace);
                }
            }
            foreach (var face in topFace)
            {
                XYZ normalFace = (face as PlanarFace).FaceNormal;
                XYZ vertexZ = new XYZ(0, 0, normalFace.Z);
            }



            return topFace;
        }
    }
}
