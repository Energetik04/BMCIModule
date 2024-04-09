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
        public Department ETO = new Department("Электротехнический отдел", "ЭТО");
        public Department ESD = new Department("Отдел инженерных изысканий", "ОИИ");
        public Department ACD = new Department("Архитектурно строительный отдел", "АСО");
        public Department MD = new Department("Горный отдел", "ГО");
        public Department DGPTT = new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ");
        public Department CTD = new Department("Химико-технолгический отдел", "ХТО");
        public Department DACCS = new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС");
        public Department WSSD = new Department("Отдел водопровода и канализации", "ОВиК");

        public void CreateSetMark()
        {            
            SetMark setMarkETO = new SetMark("ЭТО");
            SetMark setMarkACD = new SetMark("АСО");
            SetMark setMarkCTD = new SetMark("ХТО");
            SetMark setMarkWSSD = new SetMark("ОВиК");
            ETO.AddItemListMark(setMarkETO.marks);
            ACD.AddItemListMark(setMarkACD.marks);
            CTD.AddItemListMark(setMarkCTD.marks);
            WSSD.AddItemListMark(setMarkWSSD.marks);

        }
    }
    

}
