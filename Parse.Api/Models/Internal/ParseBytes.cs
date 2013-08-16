using System;
using Newtonsoft.Json;

namespace Parse.Api.Models.Internal
{
    /// <summary>
    /// Parse data type for byte[]
    /// </summary>
    internal class ParseBytes
    {
        public ParseBytes(byte[] bytes)
        {
            Base64 = bytes == null ? null : Convert.ToBase64String(bytes);
        }

        internal const string PARSE_TYPE = "Bytes";

        [JsonProperty(ParseObject.TYPE_PROPERTY)]
        internal readonly string Type = PARSE_TYPE;

        [JsonProperty("base64")]
        public string Base64 { get; set; }
    }
}