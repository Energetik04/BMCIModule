using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class ChangeGridStatus:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet element)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            FilteredElementCollector collectorGrid = new FilteredElementCollector(doc,doc.ActiveView.Id);
            var listGrid = collectorGrid.OfClass(typeof(Autodesk.Revit.DB.Grid)).Cast<Grid>().ToList();
            
            using (Transaction tr1=new Transaction(doc,"Изменение режима осей"))
            {
                try
                {
                    tr1.Start();
                    foreach (Grid currentGrid in listGrid)
                    {
                        if (currentGrid.GetDatumExtentTypeInView(DatumEnds.End0, doc.ActiveView) == DatumExtentType.Model)
                        {
                            currentGrid.SetDatumExtentType(DatumEnds.End0, doc.ActiveView, DatumExtentType.ViewSpecific);
                        }
                        else
                        {
                            currentGrid.SetDatumExtentType(DatumEnds.End0, doc.ActiveView, DatumExtentType.Model);
                        }

                        if (currentGrid.GetDatumExtentTypeInView(DatumEnds.End1, doc.ActiveView) == DatumExtentType.Model)
                        {
                            currentGrid.SetDatumExtentType(DatumEnds.End1, doc.ActiveView, DatumExtentType.ViewSpecific);
                        }
                        else
                        {
                            currentGrid.SetDatumExtentType(DatumEnds.End1, doc.ActiveView, DatumExtentType.Model);
                        }
                    }
                    tr1.Commit();
                }
                catch (Exception e)
                {
                    message = "Необходимо проверить наличие осей на плане";
                    return Result.Cancelled;
                }
            }
            return Result.Succeeded;
        }
    }
}
