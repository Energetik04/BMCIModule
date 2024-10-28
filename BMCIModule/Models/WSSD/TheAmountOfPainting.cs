using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace BMCIModule.Models.WSSD
{
    [Transaction(TransactionMode.Manual)]
    public class TheAmountOfPainting:IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            #region Получение документа и интерфейса
            var app = commandData.Application;// пользовательский интерфейс
            var uidoc = app.ActiveUIDocument;// активный документ
            var doc = uidoc.Document;// сам документ
            #endregion

            #region Проверяем общие параметры на наличие параметра BMCI_Объем АКЗ

            var bindingParameters = doc.ParameterBindings;
            bool isPresent = false;
            foreach (var definition in bindingParameters)
            {
                //Definition def = new Definition();
                //var item = (definition as BindingMap).get_Item();
            }

            #endregion

            #region Собираем элементы

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            Guid guidParameterGroup = new Guid("GUID: 3de5f1a4-d560-4fa8-a74f-25d250fb3401");
            IList<Element> setElement = collector.OfCategory(BuiltInCategory.OST_GenericModel)
                                                 .ToElements()
                                                 .Where(x => x.get_Parameter(guidParameterGroup).AsString().Contains("Ебушки-воробушк"))
                                                 .ToList();

            

            #endregion

            return Result.Succeeded;
        }
    }
}
