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
            //представление параметра в виде строки и гуид
            string parameterName = "BMCI_Площадь АКЗ";
            Guid guidParameter = new Guid("d4dd75aa-6dbe-47d7-949c-a2824683c019");
            Guid guidParameterGroup = new Guid("3de5f1a4-d560-4fa8-a74f-25d250fb3401");

            #region Проверяем общие параметры на наличие параметра BMCI_Объем АКЗ

            var bindingParameters = doc.ParameterBindings;
            bool isPresent = false;
            var categories = doc.Settings.Categories;
            Category generalModel= categories.get_Item(BuiltInCategory.OST_GenericModel);
            bool IsAllowsBoundParameter = generalModel.AllowsBoundParameters;
            //создаем набор категорий для связки с параметром
            CategorySet categorySet = new CategorySet();
            categorySet.Insert(generalModel);
            //открытие ФОП
            DefinitionFile parametersFile = app.Application.OpenSharedParameterFile();
            //поиск параметра в ФОП
            var getDefinitions = FindDefinition(parameterName, parametersFile, guidParameter);
            var sharedParameters =new FilteredElementCollector(doc)
                .OfClass(typeof(SharedParameterElement))
                .ToElements()
                .Cast<SharedParameterElement>();
            //карта связи параметров и категорий
            var binding = doc.ParameterBindings;
            //связываю по типу
            InstanceBinding typeBinding = new InstanceBinding(categorySet);
            // Создал связь категории и параметра
            using (Transaction tr = new Transaction(doc, "Связь параметра и категории"))
            {
                tr.Start();
                foreach (var parameter in sharedParameters)
                {
                    if (parameter.GuidValue == guidParameter || parameter.Name == parameterName)
                    {
                        isPresent = true;


                        break;
                    }
                }
                if (isPresent == false && IsAllowsBoundParameter==true)
                {
                    doc.ParameterBindings.Insert(getDefinitions, typeBinding,BuiltInParameterGroup.PG_DATA);
                }
                tr.Commit();
            }
            #endregion


            #region Собираем элементы

            FilteredElementCollector collector = new FilteredElementCollector(doc);

            //подправить название группирования
            string groupParameter = "Крепеж";
            IList<Element> setElement = collector.OfCategory(BuiltInCategory.OST_GenericModel)
                                                 .ToElements()
                                                 .ToList();
            List<Element> getElementWithParameter = new List<Element>();
            foreach (var element in setElement)
            {
                var parameters = element.Parameters;
                foreach (Parameter parameter in parameters)
                {
                    if(parameter.IsShared && parameter.GUID==guidParameterGroup)
                    {
                        var stringParameter = parameter.AsString();
                        if(stringParameter.Contains(groupParameter)) getElementWithParameter.Add(element);
                    }
                }
            }

            using (Transaction tr2=new Transaction(doc,"Подсчет площади"))
            {
                tr2.Start();
                foreach (var element in getElementWithParameter)
                {
                    double area = 0.0;
                    var geometryObject = element.get_Geometry(new Options());
                    if (geometryObject.First() is GeometryInstance)
                    {
                        var getInstanceInGeometryObject =
                            (geometryObject.First() as GeometryInstance).GetInstanceGeometry();
                            foreach (Solid solid in getInstanceInGeometryObject)
                            {
                                area = solid.SurfaceArea + area;
                            }
                    }
                    else
                    {
                        if (geometryObject.First() is Solid)
                        {
                            area = (geometryObject.First() as Solid).SurfaceArea + area;
                        }
                    }
                    //перевод величины
                    //var value=UnitUtils.ConvertFromInternalUnits(area, UnitTypeId.Meters);
                    element.get_Parameter(guidParameter).Set(area);
                }
                tr2.Commit();
            }

            #endregion

            return Result.Succeeded;
        }

        private Definition FindDefinition(string parameterName, DefinitionFile parametersFile,Guid guid)
        {
            foreach (var definitionsGroup in parametersFile.Groups)
            {
                foreach (ExternalDefinition definition in definitionsGroup.Definitions)
                {
                    if (definition.GUID == guid || definition.Name == parameterName)
                    {
                        return definition;
                    }
                }
            }
            return null;
        }

        // для всех категорий
        //public static CategorySet CreateFullCategorySet(Document document)
        //{
        //    CategorySet categorySet = new CategorySet();
        //    foreach (Category category in document.Settings.Categories)
        //    {
        //        if (category.AllowsBoundParameters)
        //        {
        //            categorySet.Insert(category);
        //        }
        //        else
        //        {
        //            continue;
        //        }

        //        if (category.SubCategories.IsEmpty == false)
        //        {
        //            foreach (var value in category.SubCategories)
        //            {
        //                if (value is Category subCategory)
        //                {
        //                    if (subCategory.AllowsBoundParameters) categorySet.Insert(subCategory);
        //                }
        //            }
        //        }
        //    }

        //    return categorySet;
        //}
    }
}
