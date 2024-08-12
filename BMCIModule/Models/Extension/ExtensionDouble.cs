using System;


namespace BMCIModule.Models.Extension
{
    public static class ExtensionDouble
    {
        public static double DoubleToNumbers(this double ob, int i)
        {
            if (ob == 0) return 0;
            string str = ob.ToString();
            string[] strArray = str.Split(',');
            if (strArray.Length < 2) return ob;
            char[] chr = strArray[1].ToCharArray(0, i);
            string mantisa = new string(chr);
            string strNew = strArray[0] + "," + mantisa;
            return Convert.ToDouble(strNew);
        }
    }
}
