using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BMCIModule.Infrastructure.Commands;

namespace BMCIModule.Models.FileWork
{
    [Transaction(TransactionMode.Manual)]
    public class ReadFile : IExternalCommand
    {
        string path = @"C:\Users\User\Desktop\Таблица преобразования.csv";
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var listElement = collector.OfCategory(BuiltInCategory.OST_ElectricalEquipment).ToList();

            //DefaultDialogService dialogService = new DefaultDialogService();
            //dialogService.FileExt = ".csv";
            List<Element> listElementToName = new List<Element>();
            using (Transaction tr1 = new Transaction(doc, "изменение"))
            {
                tr1.Start();
                using (StreamReader reader = new StreamReader(path))
                {
                    string input = null;
                    while ((input = reader.ReadLine()) != null)
                    {
                        string[] line = input.Split(';');
                        // ADSK_Название в схеме
                        string guidParam = "95e5eb64-92e1-436b-80d8-f06505defc34";
                        Guid guid = new Guid(guidParam);
                        var elem = listElement.Where(x =>
                        {
                            var eeParameter = x.GetParameters("Имя панели");
                            if (eeParameter.Count() != 0)
                            {
                                var namePanel = eeParameter.First().AsString();
                                if (namePanel != null)
                                {
                                    if (namePanel.Contains(line[0]))
                                    {
                                        return true;
                                    }
                                }
                                   
                            }
                            return false;
                        }).ToList();
                        if (elem != null && elem.Count()!=0)
                        {
                            var elemParam = elem.First().GetParameters("Имя панели").First();
                            elem.First().get_Parameter(guid).Set(string.Concat(line[1], "-XT"));
                            elemParam.Set(string.Concat(line[1], "-XT"));
                        }
                    }
                    tr1.Commit();
                    //
                }

                return Result.Succeeded;
            }
        }
    }
}
