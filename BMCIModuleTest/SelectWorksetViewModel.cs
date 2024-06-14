using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModuleTest
{
    internal class SelectWorksetViewModel
    {
        public ObservableCollection<Department> departmentCollections { get; }
        #region Марки
        static ICollection<Mark> ETO = new List<Mark>
        {
            new Mark("ЭМ",false),
            new Mark("ЭC", false),
            new Mark("ЭО", false),
            new Mark("ЭГ", false),
            new Mark("ЭП", false)
        };

        static ICollection<Mark> ACD = new List<Mark>
        {
            new Mark("АР",false),
            new Mark("КМ", false),
        };

        static ICollection<Mark> MD = new List<Mark>
        {
            new Mark("ГМ",false),
            new Mark("ГПР", false),
            new Mark("ГОР", false),
        };

        static ICollection<Mark> DGPTT = new List<Mark>
        {
            new Mark("ГП",false),
            new Mark("ОВ", false),
            new Mark("ГСВ", false),
            new Mark("ХС", false),
            new Mark("ТМ", false),
            new Mark("ГСВ", false),
        };

        static ICollection<Mark> CTD = new List<Mark>
        {
            new Mark("ТХ",false),

        };

        static ICollection<Mark> DACCS = new List<Mark>
        {
            new Mark("АОВ",false),
            new Mark("АВК", false),
            new Mark("СС", false),
            new Mark("ПС", false),
            new Mark("АК", false),
        };

        static ICollection<Mark> WSSD = new List<Mark>
        {
            new Mark("ВК",false),
            new Mark("НК", false),
            new Mark("НВ", false),
            new Mark("ВП", false),
            new Mark("НВК", false),
        };
        static ICollection<Mark> ESD = new List<Mark>
        {
            new Mark("МОК",false),

        };
        #endregion
        public SelectWorksetViewModel()
        {
            departmentCollections = new ObservableCollection<Department>(new Department[]
            {
            new Department("Электротехнический отдел","ЭТО",ETO),
            new Department("Отдел инженерных изысканий", "ОИИ",ESD),
            new Department("Архитектурно строительный отдел", "АСО",ACD),
            new Department("Горный отдел", "ГО",MD),
            new Department("Отдел генплана и теплоэнергетических технологий", "ОГПиТТ",DGPTT),
            new Department("Химико-технолгический отдел", "ХТО",CTD),
            new Department("Отдел автоматизированных систем управления и связи", "ОАСУиС",DACCS),
            new Department("Отдел водопровода и канализации", "ОВиК",WSSD)
            });

        }
    }
}
