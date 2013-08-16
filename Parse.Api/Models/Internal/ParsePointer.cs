using Newtonsoft.Json;

namespace Parse.Api.Models.Internal
{
    /// <summary>
    /// Parse data type for an object reference to a ParseObject
    /// </summary>
    internal class ParsePointer
    {
        public ParsePointer(ParseObject obj)
        {
            if (obj != null)
            {
                ObjectId = obj.ObjectId;
                ClassName = ParseObject.GetClassName(obj.GetType());
            }
        }

        internal const string PARSE_TYPE = "Pointer";

        [JsonProperty(ParseObject.TYPE_PROPERTY)]
        internal readonly string Type = PARSE_TYPE;

        [JsonProperty("className")]
        public string ClassName { get; set; }

        [JsonProperty("objectId")]
        public string ObjectId { get; set; }
    }
}