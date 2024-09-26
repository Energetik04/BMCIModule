

namespace BMCIModule.Models
{
    public enum PointInFigureWithBorders:int
    {
        notDetected = 0, // не определена
        inFigure = 1,   //внутри фигуры
        outFigure = 2,  //вне фигуры
        inBorders = 3,  //на границе фигуры
        moviePointFromOutFigure=4,  //сдвинул точку
        deleteThisPoint= 5, //удалить точку
        cornerPoint=6   //точки образующие плоскость основную
    }
}
