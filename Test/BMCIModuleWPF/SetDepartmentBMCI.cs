using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModuleWPF
{
    public class SetDepartmentBMCI : IEnumerable
    {
        public static List<Mark> ETO = new List<Mark>
        {
            new Mark("ЭМ",false),
            new Mark("ЭC", false),
            new Mark("ЭО", false),
            new Mark("ЭГ", false),
            new Mark("ЭП", false)
        };

        public static List<Mark> ACD = new List<Mark>
        {
            new Mark("АР",false),
            new Mark("КМ", false),
        };

        public static List<Mark> MD = new List<Mark>
        {
            new Mark("ГМ",false),
            new Mark("ГПР", false),
            new Mark("ГОР", false),
        };

        public static List<Mark> DGPTT = new List<Mark>
        {
            new Mark("ГП",false),
            new Mark("ОВ", false),
            new Mark("ГСВ", false),
            new Mark("ХС", false),
            new Mark("ТМ", false),
            new Mark("ГСВ", false),
        };

        public static List<Mark> CTD = new List<Mark>
        {
            new Mark("ТХ",false),

        };

        public static List<Mark> DACCS = new List<Mark>
        {
            new Mark("АОВ",false),
            new Mark("АВК", false),
            new Mark("СС", false),
            new Mark("ПС", false),
            new Mark("АК", false),
        };

        public static List<Mark> WSSD = new List<Mark>
        {
            new Mark("ВК",false),
            new Mark("НК", false),
            new Mark("НВ", false),
            new Mark("ВП", false),
            new Mark("НВК", false),
        };
        public static List<Mark> ESD = new List<Mark>
        {
            new Mark("МОК",false),

        };
        private Department[] departmentArray = new Department[8];
        public SetDepartmentBMCI()
        {
            DepartmentArray[0] = new Department("Архитектурно строительный отдел", "АСО", ACD);
            DepartmentArray[1] = new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС", DACCS);
            DepartmentArray[2] = new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ", DGPTT);
            DepartmentArray[3] = new Department("Отдел водопровода и канализации", "ОВиК", WSSD);
            DepartmentArray[4] = new Department("Химико-технолгический отдел", "ХТО", CTD);
            DepartmentArray[5] = new Department("Электротехнический отдел", "ЭТО", ETO);
            DepartmentArray[6] = new Department("Отдел инженерных изысканий", "ОИИ", ESD);
            DepartmentArray[7] = new Department("Горный отдел", "ГО", MD);
        }

        public Department[] DepartmentArray { get => departmentArray; set => departmentArray = value; }

        public IEnumerator GetEnumerator()
        {
            return departmentArray.GetEnumerator();
        }
    }
}
