using System;
using Newtonsoft.Json;

namespace Parse.Api.Models.Internal
{
    /// <summary>
    /// Parse data type for DateTime and DateTimeOffset
    /// </summary>
    internal class ParseDate
    {
        public ParseDate(DateTime? utcDate)
        {
            Iso = utcDate == null ? null : utcDate.Value.ToString(DATE_FMT);
        }

        public ParseDate(DateTimeOffset? utcOffset)
        {
            Iso = utcOffset == null ? null : utcOffset.Value.ToString(DATE_FMT);
        }

        internal const string DATE_FMT = "yyyy-MM-ddTHH:mm:ss.fffZ";
        internal const string PARSE_TYPE = "Date";

        [JsonProperty(ParseObject.TYPE_PROPERTY)]
        internal readonly string Type = PARSE_TYPE;

        [JsonProperty("iso")]
        public string Iso { get; set; }
    }
}