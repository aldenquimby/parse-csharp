using System.Net;

namespace Parse.Api.Models
{
    /// <summary>
    /// An exception from the Parse API.
    /// </summary>
    /// <seealso cref="http://www.parse.com/docs/dotnet/api/html/T_Parse_ParseException_ErrorCode.htm"/>
    public class ParseException
    {
        public int Code { get; set; }
        public string Error { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}