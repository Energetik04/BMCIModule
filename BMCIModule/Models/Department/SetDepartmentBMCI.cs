
using System.Collections.Generic;



namespace BMCIModule.Department
{
    public class SetDepartmentBMCI { 
        static List<Mark> ETO = new List<Mark>
        {
            new Mark("ЭМ",false),
            new Mark("ЭC", false),
            new Mark("ЭО", false),
            new Mark("ЭГ", false),
            new Mark("ЭП", false)
        };

         static List<Mark> ACD = new List<Mark>
        {
            new Mark("АР",false),
            new Mark("КМ", false),
        };

        static List<Mark> MD = new List<Mark>
        {
            new Mark("ГМ",false),
            new Mark("ГПР", false),
            new Mark("ГОР", false),
        };

        static List<Mark> DGPTT = new List<Mark>
        {
            new Mark("ГП",false),
            new Mark("ОВ", false),
            new Mark("ГСВ", false),
            new Mark("ХС", false),
            new Mark("ТМ", false),
            new Mark("ГСВ", false),
        };

        static List<Mark> CTD = new List<Mark>
        {
            new Mark("ТХ",false),

        };

        static List<Mark> DACCS = new List<Mark>
        {
            new Mark("АОВ",false),
            new Mark("АВК", false),
            new Mark("СС", false),
            new Mark("ПС", false),
            new Mark("АК", false),
        };

        static List<Mark> WSSD = new List<Mark>
        {
            new Mark("ВК",false),
            new Mark("НК", false),
            new Mark("НВ", false),
            new Mark("ВП", false),
            new Mark("НВК", false),
        };
        static List<Mark> ESD = new List<Mark>
        {
            new Mark("МОК",false),

        };

        public static List<Department> departmentArray = new List<Department> 
        {
            //new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС", DACCS),
            new Department("Архитектурно строительный отдел", "АСО", ACD),
            new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС", DACCS),
            new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ", DGPTT),
            new Department("Отдел водопровода и канализации", "ОВиК", WSSD),
            new Department("Химико-технолгический отдел", "ХТО", CTD),
            new Department("Электротехнический отдел", "ЭТО", ETO),
            new Department("Отдел инженерных обследований", "ОИО", ESD),
            new Department("Горный отдел", "ГО", MD),
        };
        
        public SetDepartmentBMCI()
        {

        }
        public List<Department> DepartmentArray {get;set;}

    }


}
