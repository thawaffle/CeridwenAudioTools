using System;
using System.Collections.Generic;
using System.Linq;

namespace com.ceridwen.audio 
{ 

    public class AudioDeviceNameComparer : IComparer<String> 
    { 

        #region Public Methods
        public int Compare(String s1, String s2)
        {
            int n1, n2;
            char[] delim = { ' ', '-' };

            try
            {
                n1 = int.Parse(s1.Split(delim).First());
            }
            catch (Exception)
            {
                n1 = 0;
            }
            try
            {
                n2 = int.Parse(s2.Split(delim).First());
            }
            catch (Exception)
            {
                n2 = 0;
            }

            if (n1 == n2)
                return String.Compare(s1, s2);
            else if (n1 > n2)
                return 1;
            else
                return -1;
        }

        #endregion

    }
}
