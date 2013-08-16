using Newtonsoft.Json;

namespace Parse.Api.Models.Internal
{
    /// <summary>
    /// Parse data type for a one-to-many or many-to-many relationship bewtween ParseObjects
    /// </summary>
    internal class ParseRelation
    {
        public ParseRelation(ParseObject obj)
        {
            if (obj != null)
            {
                ClassName = ParseObject.GetClassName(obj.GetType());
            }
        }

        internal const string PARSE_TYPE = "Relation";

        [JsonProperty(ParseObject.TYPE_PROPERTY)]
        internal readonly string Type = PARSE_TYPE;
        
        [JsonProperty("className")]
        public string ClassName { get; set; }
    }
}