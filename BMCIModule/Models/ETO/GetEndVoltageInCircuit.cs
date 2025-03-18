using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Windows.Documents;

namespace BMCIModule.Models.ETO
{
    [Transaction(TransactionMode.Manual)]
    public class GetVoltageDrop:IExternalCommand
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

            return Result.Succeeded;
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
        DFS(startElement, _document);
    }

    private void DFS(Element element, Document doc)
    {
        ElementId elementId = element.Id;

        // Если уже посещен, вернуть
        if (_visited.Contains(elementId)) return;

        // Обработка текущего элемента (например, вывод его имени)
        _visited.Add(elementId);
        //TaskDialog.Show("Element", $"Visited: {element.Name}");

        // Получение связанных элементов (например, проводов, соединений и т.д.)
        var connections = GetConnectedElements(element,doc);

        foreach (var connectedElement in connections)
        {
            DFS(connectedElement, _document);
        }
    }

    private List<Element> GetConnectedElements(Element element, Document doc)
    {
        var connectedElements = new List<Element>();
        var mepModel = (element as FamilyInstance).MEPModel as ElectricalEquipment;
        ISet<ElectricalSystem> electricalSystems = (element as FamilyInstance).MEPModel.GetAssignedElectricalSystems();
        foreach (var connect in electricalSystems)
        {
            IList<Parameter> listparameterVoltageStart = connect.GetParameters("Uнач.");
            IList<Parameter> listparameterVoltageEnd = connect.GetParameters("Uконч.");
            Parameter parameterVoltageEnd = listparameterVoltageEnd.First();
            //Parameter parameterVoltageEnd = connect.get_Parameter(new Guid("b8cb8db8-9f6b-42f9-9473-8e140ca40f7b"));

            double endVoltageValue = 0;
            
            ElectricalSystem circuitIn = null;

            var circuitOut = mepModel.GetAssignedElectricalSystems();
            var allCurcuit = mepModel.GetElectricalSystems();
            foreach (var currentCircuit in allCurcuit)
            {
                var list = circuitOut.Where(x => x.Id == currentCircuit.Id).ToList();
                if (list.Count == 0)
                {
                    circuitIn = currentCircuit;
                    break;
                }
            }

            if (listparameterVoltageStart[0].HasValue == false || listparameterVoltageStart[0].AsDouble()==0)
            {
                
                endVoltageValue=circuitIn.GetParameters("Uконч.").First().AsDouble();
                using (Transaction tr1 = new Transaction(doc, "Uконч."))
                {
                    tr1.Start();
                    listparameterVoltageStart[0].Set(endVoltageValue);
                    tr1.Commit();
                }
            }
            else
            {
                if (circuitIn != null)
                {
                    if (listparameterVoltageStart[0].AsDouble() != circuitIn.GetParameters("Uконч.").First().AsDouble())
                    {
                        endVoltageValue = circuitIn.GetParameters("Uконч.").First().AsDouble();
                        using (Transaction tr1 = new Transaction(doc, "Uконч."))
                        {
                            tr1.Start();
                            listparameterVoltageStart[0].Set(endVoltageValue);
                            tr1.Commit();
                        }
                    }
                }
            }
            
            endVoltageValue = GetVoltageEnd(connect);
            using (Transaction tr1=new Transaction(doc,"Uконч."))
            {
                tr1.Start();
                parameterVoltageEnd.Set(endVoltageValue);
                tr1.Commit();
            }
            //VoltageStartElement = circuitIn.GetParameters("Uкон.").First().AsDouble();

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

    private double GetVoltageEnd(ElectricalSystem el)
    {
        IList<Parameter> listparameterVoltageStart = el.GetParameters("Uнач.");
        IList<Parameter> listparameterPolus = el.GetParameters("Жильность");
        IList<Parameter> listParameterR = el.GetParameters("r уд");
        IList<Parameter> listParameterX = el.GetParameters("x уд");
              
        double voltageEnd = 0;
        double cos=el.GetParameters("Коэффициент мощности руч.").First().AsDouble();
        double length=el.Length;//В футах
        double lengthEnd = (length + 2) * 1.02*304/1000;
        double voltageStart = listparameterVoltageStart.First().AsDouble();
        
        int polus=listparameterPolus.First().AsInteger();
        double deltaR=listParameterR.First().AsDouble();
        double deltaX=listParameterX.First().AsDouble();
        double I = 0;
        string s = el.GetParameters("Полная установленная мощность").First().AsValueString();
        double P = double.Parse(s);

        if (polus > 3)
        {
            I = P*cos/1.7385/0.4;
        }
        else
        {
            I = P * cos / 0.23;
        }

        if(voltageStart>190 & voltageStart < 250)
        {
            voltageEnd = voltageStart - 2 * I * lengthEnd / 1000 * (deltaR * cos + deltaX * Math.Pow(1 - cos * cos, 0.5));
        }
        else
        {
            if (polus > 3)
            {
                voltageEnd = voltageStart - 1.7385 * lengthEnd / 1000 * I * (deltaR * cos + deltaX * Math.Pow(1 - cos * cos, 0.5));
            }
            else
            {
                voltageEnd = voltageStart/1.7385 - 2 * lengthEnd / 1000 * I * (deltaR * cos + deltaX * Math.Pow(1 - cos * cos, 0.5));
            }
        }
        if (voltageEnd == double.NaN)
        {

        }
        return voltageEnd;
    }
}
