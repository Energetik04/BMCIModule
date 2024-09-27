using System;
using System.Security.RightsManagement;
using Autodesk.Revit.DB;

namespace BMCIModule.Models.PointCloud
{
    public class Edge
    {
        public XYZ startPoint { get; set; } = null;
        public XYZ endPoint { get; set; } = null;

        public Edge()
        {

        }

        public Edge(XYZ startPoint, XYZ endPoint)
        {
            this.startPoint = startPoint;
            this.endPoint = endPoint;
        }
        public override bool Equals(Object ob)
        {
            Edge edge=ob as Edge;

            if ((edge.startPoint.IsAlmostEqualTo(this.startPoint) && edge.endPoint.IsAlmostEqualTo(this.endPoint)) ||
                (edge.endPoint.IsAlmostEqualTo(this.startPoint) && edge.startPoint.IsAlmostEqualTo(this.endPoint)))
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