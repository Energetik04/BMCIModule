using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace BMCIModule.Models.Extension
{
    public static class ExtensionFace
    {
        public static IList<XYZ> GetListPointInFace(this Face ob)
        {
            IList<XYZ> listPointInFace=new List<XYZ>();
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
