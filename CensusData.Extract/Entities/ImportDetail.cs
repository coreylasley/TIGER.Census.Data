namespace Tiger.Entities
{
    public class ImportDetail
    {
        public int ID { get; set; }
        public string FileType { get; set; }
        public string URL { get; set; }
        public string LocalFile { get; set; }
        public int LastRecordNum { get; set; }
        public bool IsComplete { get; set; }
    }
}
