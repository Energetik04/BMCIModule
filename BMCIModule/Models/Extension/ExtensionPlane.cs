using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.DB;

namespace BMCIModule.Models.Extension
{
    public static class ExtensionPlane
    {
        public static bool EqualsPlane (this Plane currentPlane, Plane plane)
        {
            if (ReferenceEquals(currentPlane, plane)) return true;
            if(plane == null || currentPlane==null) return false;
            if(currentPlane.Normal.IsAlmostEqualTo(plane.Normal) &&
                currentPlane.Origin.IsAlmostEqualTo(plane.Origin) &&
                currentPlane.XVec.IsAlmostEqualTo(plane.XVec)&&
                currentPlane.YVec.IsAlmostEqualTo(plane.XVec))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
