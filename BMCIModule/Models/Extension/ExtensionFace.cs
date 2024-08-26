using System.Collections.Generic;
using System.Windows.Controls;

using Autodesk.Revit.DB;

namespace BMCIModule.Models.Extension
{
    public static class ExtensionFace
    {
        public static List<XYZ> GetListPointInFace(this Face ob)
        {
            List<XYZ> listPointInFace=new List<XYZ>();
            var curveLoop = ob.GetEdgesAsCurveLoops();
            foreach (var loop in curveLoop)
            {
                foreach (var curve in loop)
                {
                    listPointInFace.Add(curve.GetEndPoint(0));
                }
            }
            return listPointInFace;
        }

    }
}
