using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BMCIModule.Department
{
    public class Department
    {
        string name;
        string tag;
        List<Mark> listMark = new List<Mark>();


        public string Name { get { return Name; } set { Name = value; } }

        public string Tag { get => Tag; set => Tag = value; }
        public List<Mark> ListMark { get => listMark; set => listMark = value; }

        public Department(string name)
        {
            this.name = name;
        }

        public Department(string name, string tag)
        {
            this.name = name;
            this.tag = tag;
        }
        public Department() { }

        public List<Mark> AddItemListMark(List<Mark> mark)
        {
            listMark = mark;
            return listMark;
        }
    }

    public class Mark
    {
        string name;
        bool statusmark;

        public string Name { get => name; set => name = value; }
        public bool Statusmark { get => statusmark; set => statusmark = value; }

        public Mark() { }

        public Mark(string name)
        {
            Name = name;
        }

        public Mark(string name, bool statusmark)
        {
            Name = name;
            Statusmark = statusmark;
        }

    }

    public class SetDepartmentBMCI
    {
        static void Main()
        {
            Department ETO = new Department("Электротехнический отдел", "ЭТО");
            SetMarkETO setMarkETO = new SetMarkETO();
            setMarkETO.AddMarkETO();
            ETO.AddItemListMark(setMarkETO.marks);


            foreach (Mark mark in ETO.ListMark)
            {
                if (mark.Name == "ЭМ")
                {
                    mark.Statusmark = true;
                }
            }

            Department ESD = new Department("Отдел инженерных изысканий", "ОИИ");
            Department ACD = new Department("Архитектурно строительный отдел", "АСО");
            Department MD = new Department("Горный отдел", "ГО");
            Department DGPTT = new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ");
            Department CTD = new Department("Химико-технолгический отдел", "ХТО");
            Department DACCS = new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС");
            Department WSSD = new Department("Отдел водопровода и канализации", "ОВиК");
        }
    }

    public class SetMarkETO
    {

        Mark markETO1 = new Mark("ЭМ", false);
        Mark markETO2 = new Mark("ЭC", false);
        Mark markETO3 = new Mark("ЭО", false);
        Mark markETO4 = new Mark("ЭГ", false);

        public List<Mark> marks = new List<Mark>();
        public void AddMarkETO()
        {
            marks.Add(markETO1);
            marks.Add(markETO2);
            marks.Add(markETO3);
            marks.Add(markETO4);
        }
    }





}
