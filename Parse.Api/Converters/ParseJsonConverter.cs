using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parse.Api.Models;

namespace Parse.Api.Converters
{
    /// <summary>
    /// Base class for handling deserialization of Parse data types.
    /// </summary>
    internal abstract class ParseJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        protected string GetParseType(JObject jObject)
        {
            return jObject[ParseObject.TYPE_PROPERTY].Value<string>();
        }
    }
}