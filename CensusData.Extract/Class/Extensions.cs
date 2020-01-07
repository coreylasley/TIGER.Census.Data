using System;
using System.Collections.Generic;

using static CensusData.Extract.Enums;

namespace CensusData
{
    public static class Extensions
    {
        public static string ToStringList(this List<DataTypes> list)
        {
            string dts = "";
            foreach (DataTypes dt in list)
            {
                if (dts != "") dts += ", ";
                dts += dt.ToString("g");
            }

            return dts;
        }

        public static string ToLogFormat(this DateTime dt)
        {
            return dt.ToString("hh:mm:ss.FFFF").PadRight(13, '0') + " ";
        }
    }
}
