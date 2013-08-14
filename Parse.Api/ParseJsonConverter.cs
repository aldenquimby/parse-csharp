using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Deserializers;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace Parse.Api
{
    public abstract class ParseJsonConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(T);
        }

        public abstract T ReadJson(JsonReader reader, Type objectType);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return ReadJson(reader, objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class ParseBytesConverter : ParseJsonConverter<byte[]>
    {
        public override byte[] ReadJson(JsonReader reader, Type objectType)
        {
            var jObject = JObject.Load(reader);

            if (jObject["__type"].Value<string>() != "Bytes")
            {
                throw new JsonException("Failed to parse bytes from: " + jObject);
            }

            var base64 = jObject["base64"].Value<string>();
            return Convert.FromBase64String(base64);
        }
    }

    public class ParseDateConverter : ParseJsonConverter<DateTime>
    {
        public override DateTime ReadJson(JsonReader reader, Type objectType)
        {
            if (reader.ValueType == typeof(DateTime))
            {
                return (DateTime)reader.Value;
            }

            var jObject = JObject.Load(reader);

            if (jObject["__type"].Value<string>() != "Date")
            {
                throw new JsonException("Failed to parse date from: " + jObject);
            }

            return jObject["iso"].Value<DateTime>();
        }
    }

    public class ParseJsonDeserializer : IDeserializer
    {
        public T Deserialize<T>(IRestResponse response)
        {
            return JsonConvert.DeserializeObject<T>(response.Content, new JsonSerializerSettings
            {
                DateFormatString = DateFormat,
                Converters = new List<JsonConverter> { new ParseBytesConverter(), new ParseDateConverter() },
            });
        }

        public string RootElement { get; set; }
        public string Namespace { get; set; }
        public string DateFormat { get; set; }
    }
}