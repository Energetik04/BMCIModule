using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Models.PointCloud
{
    public class Point
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public bool checkoutStatus { get; set; } = false;

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
        public HashSet<Point> pointForHex = new HashSet<Point>();

        public Point(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}

