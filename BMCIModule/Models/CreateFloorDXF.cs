using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;

namespace BMCIModule.Models
{
    public class CreateFloorDXF:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc=commandData.Application.ActiveUIDocument.Document;

            FilteredElementCollector collectorTopography=new FilteredElementCollector(doc);

            IList<Element> topographySurfaces = collectorTopography.WhereElementIsElementType()
            .OfCategory(BuiltInCategory.OST_TopographySurface)
            .ToElements();
            UIDocument uidoc=new UIDocument(doc);
            Selection choice=uidoc.Selection;
            foreach (Element element in choice.GetElementIds().Select(e => doc.GetElement(e)))
            {
                IList<XYZ> boundaryPointSurface = (element as TopographySurface).GetBoundaryPoints();
                Floor floor=element as Floor;
                if(floor != null)
                {
                    SlabShapeEditor slabShapeEditor=floor.SlabShapeEditor;
                    
                }

            }
            
            
            

            return Result.Succeeded;
        }

    }
}
