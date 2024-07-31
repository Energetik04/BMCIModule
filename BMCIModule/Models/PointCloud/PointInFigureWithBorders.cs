using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Models
{
    public enum PointInFigureWithBorders:int
    {
        notDetected = 0,
        inFigure = 1,
        outFigure = 2,
        inBorders = 3,
        moviePointFromOutFigure=4,
        deleteThisPoint= 5
    }
}
