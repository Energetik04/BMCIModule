using BMCIModule.Department;
using BMCIModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BMCIModule.Department
{
    public class SetDepartmentBMCI
    {
        Department ETO = new Department("Электротехнический отдел", "ЭТО");
        Department ESD = new Department("Отдел инженерных изысканий", "ОИИ");
        Department ACD = new Department("Архитектурно строительный отдел", "АСО");
        Department MD = new Department("Горный отдел", "ГО");
        Department DGPTT = new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ");
        Department CTD = new Department("Химико-технолгический отдел", "ХТО");
        Department DACCS = new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС");
        Department WSSD = new Department("Отдел водопровода и канализации", "ОВиК");

        void CreateSetMark()
        {
            
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
            SelectWorkset selectWorkset = new SelectWorkset(setMarkETO.marks);
        }
    }
    

}
