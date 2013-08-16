using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parse.Api.Models.Internal;

namespace Parse.Api.Converters
{
    /// <summary>
    /// Handles deserialization of ParseBytes into byte[].
    /// </summary>
    internal class ParseBytesConverter : ParseJsonConverter<byte[]>
    {
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            if (GetParseType(jObject) != ParseBytes.PARSE_TYPE)
            {
                throw new JsonException("Failed to parse bytes from: " + jObject);
            }

            var base64 = jObject["base64"].Value<string>();
            return Convert.FromBase64String(base64);
        }
    }
}