using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BMCIModule.Models
{
    internal class RevitUtils
    {
        public SelectWorkset SelectWorkset {  get; set; }

        public void CloseWindow()
        {
            SelectWorkset.Close();
        }

    }
}
