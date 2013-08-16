using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parse.Api.Models.Internal;

namespace Parse.Api.Converters
{
    /// <summary>
    /// Handles deserialization of ParseDate into DateTime or DateTimeOffset.
    /// </summary>
    internal class ParseDateConverter : ParseJsonConverter<DateTime>
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof (DateTime) || objectType == typeof (DateTime?) ||
                   (objectType == typeof (DateTimeOffset) || objectType == typeof (DateTimeOffset?));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.ValueType != null)
            {
                return reader.Value;
            }

            var jObject = JObject.Load(reader);

            if (GetParseType(jObject) != ParseDate.PARSE_TYPE)
            {
                throw new JsonException("Failed to parse date from: " + jObject);
            }

            return jObject["iso"].ToObject(objectType);
        }
    }
}