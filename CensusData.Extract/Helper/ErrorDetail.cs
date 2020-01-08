using static Tiger.Helper.Enums;

namespace Tiger.Helper
{
    public class ErrorDetail
    {
        public ErrorTypes ErrorType { get; set; }
        public string Message { get; set; }
        public DataTypes? DataType { get; set; }
        public string URL { get; set; }
        public string LocalFile { get; set; }

        public ErrorDetail(ErrorTypes errorType, DataTypes? dataType, string message, string url, string localFile)
        {
            ErrorType = errorType;
            DataType = dataType;
            Message = message;
            URL = url;
            LocalFile = localFile;
        }
    }
}
