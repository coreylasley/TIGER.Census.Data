using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.Helper
{
    public class Enums
    {
        public const string BaseURL = "https://www2.census.gov/geo/tiger/";
        public const string BaseYearPath = "TIGER";
        public const string DataFileType = "dbf";

        public enum DataTypes
        {
            ADDR,
            ADDRFEAT,
            ADDRFN,
            AIANNH,
            AITSN,
            ANRC,
            AREALM,
            AREAWATER,
            BG,
            CBSA,
            CD,
            CNECTA,
            COASTLINE,
            CONCITY,
            COUNTY,
            COUSUB,
            CSA,
            EDGES,
            ELSD,
            ESTATE,
            FACES,
            FACESAH,
            FACESAL,
            FACEMIL,
            FEATNAMES,
            LINEARWATER,
            METDIV,
            MIL,
            NECTA,
            NECTADIV,
            PLACE,
            POINTLM,
            PRIMARYROADS,
            PRISECROADS,
            PUMA,
            RAILS,
            ROADS,
            SCSD,
            SLDL,
            SLDU,
            STATE,
            SUBMCD,
            TABBLOCK,
            TBG,
            TRACT,
            TTRACT,
            UAC,
            UNSD,
            ZCTA5
        }

        public enum ErrorTypes
        {
            Exception,
            BadOrMissingLocalFile,
            CouldNotDownloadFile,
            FailedToUnzip,
            FailedToDelete
        }
    }
}
