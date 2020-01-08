using System.Collections.Generic;

namespace Tiger.Helper
{
    public class DetailedReturn
    {
        public long TotalRecordsInFile { get; set; }
        public long TotalRecordsAlreadyInDB { get; set; }
        public long TotalRecordsImported { get; set; }
        public bool CompletedSuccessfully { get; set; }
        public List<ErrorDetail> Errors { get; set; }

        public DetailedReturn()
        {
            Errors = new List<ErrorDetail>();
        }
    }
}
