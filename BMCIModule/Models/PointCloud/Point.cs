using System.Collections.Generic;

using Autodesk.Revit.DB;

namespace BMCIModule.Models.PointCloud
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public bool checkoutStatus { get; set; }

        PointInFigureWithBorders GetPointPosition = PointInFigureWithBorders.notDetected;

        public PointInFigureWithBorders getPointPosition
        {
            get
            {
                return GetPointPosition;
            }
            set
            {
                GetPointPosition = value;
            }

        }
        public XYZ CreateXYZFromPoint()
        {
            return new XYZ(X, Y, Z);
        }
        public HashSet<Point> pointForHex = new HashSet<Point>();

        private List<Edge> _listEdgeForPoint=new List<Edge>();

        public void GetListEdgeForPoint()
        {
            if (pointForHex != null || pointForHex.Count != 0)
            {
                foreach (Point point in pointForHex)
                {
                    XYZ pointHex = point.CreateXYZFromPoint();
                    XYZ currentPoint = new XYZ(X, Y, Z);
                    _listEdgeForPoint.Add(new Edge(currentPoint,pointHex));
                }
            }
        }
        
        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}

