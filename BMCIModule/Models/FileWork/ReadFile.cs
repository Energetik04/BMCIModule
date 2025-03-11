using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.Expando;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BMCIModule.Infrastructure.Commands;
using Microsoft.Win32;

namespace BMCIModule.Models.FileWork
{
    [Transaction(TransactionMode.Manual)]
    public class ReadFile:IExternalCommand
    {
        string path= @"C:\Users\User\Desktop\Таблица преобразования.csv";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            FilteredElementCollector collector=new FilteredElementCollector(doc);
            var listElement = collector.OfCategory(BuiltInCategory.OST_MechanicalEquipment).ToList();

            DefaultDialogService dialogService = new DefaultDialogService();
            dialogService.FileExt = ".csv";
            List<Element> listElementToName = new List<Element>();
            using(Transaction tr1=new Transaction(doc, "изменение"))
            {
                tr1.Start();
                using (StreamReader reader = new StreamReader(path))
                {
                    string input = null;
                    while ((input = reader.ReadLine()) != null)
                    {
                        string[] line = input.Split(';');
                        string guidParam = "95e5eb64-92e1-436b-80d8-f06505defc34";
                        Guid guid = new Guid(guidParam);
                        var elem = listElement.Where(x => x.get_Parameter(guid).AsString() == line[0]).First();
                        elem.get_Parameter(guid).Set(line[1]);
                    }
                }
                tr1.Commit();                
            }

            return Result.Succeeded;
        }
    }
}
