using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Department
{
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

        public SetMarkETO() {}
    }

}
