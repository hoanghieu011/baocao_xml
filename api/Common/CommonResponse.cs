using System.Net;

namespace API.Common
{
    public class CommonResponse
    {
        public Boolean isError { get; set; }
        public string message { get; set; }
    }

    public class ImportExcelResponse : CommonResponse
    {
        public int? affectedRows { get; set; }
        public string table { get; set; }

    }

    public class ImportXMLResponse : CommonResponse
    {
        public int? countXML1 { get; set; }
        public int? countXML2 { get; set; }
        public int? countXML3 { get; set; }
    }
}
