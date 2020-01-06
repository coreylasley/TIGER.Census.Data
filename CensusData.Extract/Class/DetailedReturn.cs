using System.Collections.Generic;

namespace CensusData.Extract
{
    public class DetailedReturn
    {
        public long TotalRecordsInFile { get; set; }
        public long TotalRecordsAlreadyInDB { get; set; }
        public long TotalRecordsImported { get; set; }
        public bool CompletedSuccessfully { get; set; }
        public List<string> Errors { get; set; }

        public DetailedReturn()
        {
            Errors = new List<string>();
        }
    }
}
