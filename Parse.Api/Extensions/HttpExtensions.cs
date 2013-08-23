using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Parse.Api.Attributes;
using Parse.Api.Models;
using Parse.Api.Models.Internal;

namespace Parse.Api.Extensions
{
    internal static class HttpExtensions
    {
        public static void AddParseBody(this HttpWebRequest request, ParseObject body)
        {
            var dict = new Dictionary<string, object>();

            foreach (var prop in body.GetType().GetProperties())
            {
                var value = prop.GetValue(body, null);

                if (prop.PropertyType == typeof(DateTime))
                {
                    value = new ParseDate((DateTime)value);
                }
                else if (prop.PropertyType == typeof(byte[]))
                {
                    value = new ParseBytes((byte[])value);
                }
                else if (typeof(ParseObject).IsAssignableFrom(prop.PropertyType))
                {
                    if (value != null)
                    {
                        value = new ParsePointer((ParseObject)value);
                    }
                }
                else if (prop.PropertyType.IsGenericType && value is IList && typeof(ParseObject).IsAssignableFrom(prop.PropertyType.GetGenericArguments()[0]))
                {
                    // explicity skip relations, need to be dealt with manually
                    continue;

                    // var pointers = ((IList) value).Cast<ParseObject>().Select(x => new ParsePointer(x)).ToList();
                    // value = pointers.Count == 0 ? null : new {__op = "AddRelation", objects = pointers};
                }

                var attrs = prop.GetCustomAttributes(true);
                JsonIgnoreForSerializationAttribute jsonIgnore = null;
                JsonPropertyAttribute jsonProp = null;
                foreach (var attr in attrs)
                {
                    var tmp1 = attr as JsonPropertyAttribute;
                    if (tmp1 != null)
                    {
                        jsonProp = tmp1;
                    }
                    var tmp2 = attr as JsonIgnoreForSerializationAttribute;
                    if (tmp2 != null)
                    {
                        jsonIgnore = tmp2;
                    }
                }
                if (jsonIgnore != null)
                {
                    continue;
                }

                if (jsonProp != null && !string.IsNullOrEmpty(jsonProp.PropertyName))
                {
                    dict[jsonProp.PropertyName] = value;
                }
                else
                {
                    dict[prop.Name] = value;
                }
            }

            request.AddBody(dict);
        }

        public static void AddBody(this HttpWebRequest request, object body)
        {
            var serializedBody = JsonConvert.SerializeObject(body);
            request.ContentType = "application/json";

            var done = new ManualResetEvent(false);

            request.BeginGetRequestStream(ar =>
            {
                var request1 = (HttpWebRequest) ar.AsyncState;
                using (var postStream = request1.EndGetRequestStream(ar))
                {
                    var byteArray = Encoding.UTF8.GetBytes(serializedBody);
                    postStream.Write(byteArray, 0, byteArray.Length);
                }
                done.Set();
            }, request);

            done.WaitOne();
        }

        public static void Add(this WebHeaderCollection headers, string key, string value)
        {
            headers[key] = value;
        }
    }
}