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
                if (getFace != null && getFace is PlanarFace)
                {
                    XYZ normalFace = (getFace as PlanarFace).FaceNormal;
                    if (normalFace.Z > 0) topFace.Add(getFace);
                }
            }
            return topFace;
        }
    }
}
