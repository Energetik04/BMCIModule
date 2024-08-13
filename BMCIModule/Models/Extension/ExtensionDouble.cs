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
            if (strArray[1].Length < i)
            {
                char[] mantisaInsertZero = new char[i];
                char[] charArrayString= strArray[1].ToCharArray();
                for (int j = 0; j < strArray[1].Length; j++)
                {
                    mantisaInsertZero[j] = charArrayString[j];
                }
                for (int k = strArray[1].Length ; k < mantisaInsertZero.Length; k++)
                {
                    mantisaInsertZero[k]='0';
                }
                string mantisaWithZero = new string(mantisaInsertZero);
                string stringWithZero = strArray[0] + "," + mantisaWithZero;
                return Convert.ToDouble(stringWithZero);
            }
            char[] chr = strArray[1].ToCharArray(0, i);
            string mantisa = new string(chr);
            string strNew = strArray[0] + "," + mantisa;
            return Convert.ToDouble(strNew);
        }
    }
}
