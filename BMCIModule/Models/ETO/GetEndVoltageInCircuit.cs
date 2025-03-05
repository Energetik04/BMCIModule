using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;

namespace BMCIModule.Models.ETO
{
    internal class GetEndVoltageInCircuit
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiapp = commandData.Application;
            var uidoc = uiapp.ActiveUIDocument;
            var doc = uidoc.Document;
            Element selectElement = null;
            ElementId selectElementId = null;


            #region SelectElectricalEquipment
            TaskDialog.Show("Выбор элемента", "Выберите щит, в цепях которого прописано Uнач.");
            Selection choiceEE = uidoc.Selection;
            Reference choiceEERef = choiceEE.PickObject(ObjectType.Element, "Выбор элемента");
            selectElement = doc.GetElement(choiceEERef);
            selectElementId = selectElement.Id;
            #endregion

            ElectricCircuitDFS dfs = new ElectricCircuitDFS(doc);
            dfs.PerformDFS(selectElement);

            throw new NotImplementedException();
        }
    }
}

// расписать дерево 
public class ElectricCircuitDFS
{
    private readonly Document _document;
    private readonly HashSet<ElementId> _visited;

    public ElectricCircuitDFS(Document document)
    {
        _document = document;
        _visited = new HashSet<ElementId>();
    }

    public void PerformDFS(Element startElement)
    {
        if (startElement == null) throw new ArgumentNullException(nameof(startElement));
        DFS(startElement);
    }

    private void DFS(Element element)
    {
        ElementId elementId = element.Id;

        // Если уже посещен, вернуть
        if (_visited.Contains(elementId)) return;

        // Обработка текущего элемента (например, вывод его имени)
        _visited.Add(elementId);
        //TaskDialog.Show("Element", $"Visited: {element.Name}");

        // Получение связанных элементов (например, проводов, соединений и т.д.)
        var connections = GetConnectedElements(element);

        foreach (var connectedElement in connections)
        {
            DFS(connectedElement);
        }
    }

    private List<Element> GetConnectedElements(Element element)
    {
        var connectedElements = new List<Element>();
        var mepModel = (element as FamilyInstance).MEPModel as ElectricalEquipment;
        ISet<ElectricalSystem> electricalSystems = (element as FamilyInstance).MEPModel.GetAssignedElectricalSystems();
        foreach (var connect in electricalSystems)
        {
            IList<Parameter> listparameter = connect.GetParameters("Uнач.");
            double VoltageStartElement = 0;
            ElectricalSystem circuitIn = null;
            if (listparameter[0].HasValue == null)
            {
                var circuitOut = mepModel.GetAssignedElectricalSystems();
                var allCurcuit = mepModel.GetElectricalSystems();
                foreach (var currentCircuit in circuitOut)
                {
                    if (!allCurcuit.Contains(currentCircuit))
                    {
                        circuitIn = currentCircuit;
                        break;
                    }
                }
            }
            VoltageStartElement = circuitIn.GetParameters("Uкон.").First().AsDouble();


            var elements = connect.Elements;
            Element currentElem = null;
            foreach (Element elementEE in elements)
            {
                currentElem = elementEE;
                if (currentElem != null) connectedElements.Add(currentElem);
            }
        }
        // Здесь вы можете использовать логику для нахождения связанных элементов
        // Например, через параметры или через графическую базу данных
        // Это место должно быть заполнено вашим кодом для поиска связанных цепей

        var lineTypeIds = new List<ElementId>();
        // Получить провода или соединения по вашему критерию
        return connectedElements;
    }
}
