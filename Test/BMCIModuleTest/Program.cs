using System.Collections;

namespace BMCIModuleTest
{
    internal class Program
    {
        public class Department
        {
            public string Name { get; set; }

            public string Tag { get; set; }
            public ICollection<Mark> ListMark { get; set; }

            /*public Department(string name)
            {
                Name = name;
            }

            public Department(string name, string tag)
            {
                Name = name;
                Tag = tag;
            }*/
            public Department() { }

            public Department(string name, string tag, ICollection<Mark> marks)
            {
                Name = name;
                Tag = tag;
                ListMark = marks;
            }

        }
        public class Mark
        {
            public string Name { get; set; }
            public bool Statusmark { get; set; }

            public Mark() { }

            //public Mark(string name)
            //{
            //    Name = name;
            //}

            public Mark(string name, bool statusmark)
            {
                Name = name;
                Statusmark = statusmark;
            }

        }
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
            private Department[] _departmentArray = new Department[8];
            public SetDepartmentBMCI()
            {
                _departmentArray[0] = new Department("Электротехнический отдел", "ЭТО", ETO);
                _departmentArray[1] = new Department("Отдел инженерных изысканий", "ОИИ", ESD);
                _departmentArray[2] = new Department("Архитектурно строительный отдел", "АСО", ACD);
                _departmentArray[3] = new Department("Горный отдел", "ГО", MD);
                _departmentArray[4] = new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ", DGPTT);
                _departmentArray[5] = new Department("Химико-технолгический отдел", "ХТО", CTD);
                _departmentArray[6] = new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС", DACCS);
                _departmentArray[7] = new Department("Отдел водопровода и канализации", "ОВиК", WSSD);
            }

            public IEnumerator GetEnumerator()
            {
                return _departmentArray.GetEnumerator();
            }

            /*public ICollection<Department> departmentList = new List<Department>();
            {
                new Department("Электротехнический отдел","ЭТО",ETO),
                new Department("Отдел инженерных изысканий", "ОИИ",ESD),
                new Department("Архитектурно строительный отдел", "АСО",ACD),
                new Department("Горный отдел", "ГО",MD),
                new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ",DGPTT),
                new Department("Химико-технолгический отдел", "ХТО",CTD),
                new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС",DACCS),
                new Department("Отдел водопровода и канализации", "ОВиК",WSSD)
            };*/

        }
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
        }
    }
}
