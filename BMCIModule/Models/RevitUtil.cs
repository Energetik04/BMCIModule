using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Models
{
    internal class RevitUtil
    {
        public SelectWorkset SelectWorkset { get; set; }

        public void CloseWindow()
        {
            SelectWorkset.Close();
        }
    }
}
