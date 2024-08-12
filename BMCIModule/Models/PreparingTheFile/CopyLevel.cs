using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace BMCIModule
{
    [Transaction(TransactionMode.Manual)]
    public class CopyLevel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elementSet)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            FilteredElementCollector collectorLevelProject = new FilteredElementCollector(doc);
            FilteredElementCollector collectorRvtLink = new FilteredElementCollector(doc);

            IList<Element> levelProject = collectorLevelProject.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Levels).ToElements();
            IList<Element> rvtLink = collectorRvtLink.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_RvtLinks).ToElements();
            try
            {
                foreach (Element element1 in rvtLink)
                {
                    if (element1.Name.Contains("Координационный"))
                    {
                        using (Transaction tCopyGrid = new Transaction(doc, "Копирование уровеней"))
                        {
                            tCopyGrid.Start();

                            RevitLinkInstance revitLinkInstance = element1 as RevitLinkInstance;
                            FilteredElementCollector collectorElementInstance = new FilteredElementCollector(revitLinkInstance.GetLinkDocument());
                            ICollection<Element> linkLevels = collectorElementInstance.WhereElementIsNotElementType().OfCategory(BuiltInCategory.OST_Levels).ToElements();
                            int count = 0;

                            FilteredWorksetCollector collectorWorkset = new FilteredWorksetCollector(doc);
                            //фильтр рабочих наборов и перечень
                            WorksetKindFilter setFilter = new WorksetKindFilter(WorksetKind.UserWorkset);
                            IList<Workset> setWorkset = collectorWorkset.WherePasses(setFilter).ToWorksets();
                            //делегирование поиска и создание рабочего набора
                            SearchWorkset getWorkset = new SearchWorkset(CreateWorkset.GetWorkset);
                            Workset workSetLevelAndGrid=getWorkset(setWorkset, doc);


                            foreach (Element getLevelLink in linkLevels)
                            {
                                if (count < levelProject.Count())
                                {
                                    levelProject[count].Name = getLevelLink.Name;
                                    Parameter getElevationNewLevel = (levelProject[count] as Level).get_Parameter((BuiltInParameter.LEVEL_ELEV));
                                    getElevationNewLevel.Set(getLevelLink.get_Parameter((BuiltInParameter.LEVEL_ELEV)).AsDouble());
                                    levelProject[count].get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).Set(workSetLevelAndGrid.Id.IntegerValue);
                                    (levelProject[count] as Level).Pinned = true;
                                    count++;
                                }
                                else
                                {
                                    Parameter setElevationNewLevel = getLevelLink.get_Parameter((BuiltInParameter.LEVEL_ELEV));
                                    Level createNewLevel = Level.Create(doc, setElevationNewLevel.AsDouble());
                                    createNewLevel.Name = getLevelLink.Name;
                                    createNewLevel.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM).Set(workSetLevelAndGrid.Id.IntegerValue);
                                    createNewLevel.Pinned = true;
                                }
                            }
                            // Добавить выбор семейства для марки внизу                        
                            tCopyGrid.Commit();
                        }
                    }
                    else
                    {
                        throw new FileNotFoundException();
                    }
                   
                }
            }catch (FileNotFoundException)
            {
                message = "Нету связанной модели с именем Координационный. Проверьте диспетчер связей";
                return Result.Cancelled;
            }
                return Result.Succeeded;
            }
        }
    }
