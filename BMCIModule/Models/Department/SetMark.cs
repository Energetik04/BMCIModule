using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Department
{
    public class SetMark
    {
        public List<Mark> marks = new List<Mark>();
        
        public SetMark(string tag) 
        {
            if (tag == "ЭТО")
            {
                void AddMark()
                {
                    marks.Add(new Mark("ЭМ", false));
                    marks.Add(new Mark("ЭC", false));
                    marks.Add(new Mark("ЭО", false));
                    marks.Add(new Mark("ЭГ", false));
                    marks.Add(new Mark("ЭП", false));
                }
            }
            else
            {
                if (tag == "АСО")
                {
                    void AddMark()
                    {
                        marks.Add(new Mark("АР", false));
                        marks.Add(new Mark("КМ", false));
                        marks.Add(new Mark("КЖ", false));
                    }
                }
                else
                {
                    if(tag == "ОВиК")
                    {
                        void AddMark()
                        {
                            marks.Add(new Mark("ВК", false));
                            
                        }
                    }
                    else
                    {
                        if(tag== "ХТО")
                        {
                            void AddMark()
                            {
                                marks.Add(new Mark("ТХ", false));

                            }
                        }
                    }
                }
            }
           
        }

    }

}
