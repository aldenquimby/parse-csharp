using Newtonsoft.Json;

namespace Parse.Api.Models
{
    /// <summary>
    /// Parse data type for a geographic point (lat + lon)
    /// </summary>
    public class ParseGeoPoint
    {
        public ParseGeoPoint(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        internal const string PARSE_TYPE = "GeoPoint";

        [JsonProperty(ParseObject.TYPE_PROPERTY)]
        internal readonly string Type = PARSE_TYPE;

        [JsonProperty("latitude")]
        public double Latitude { get; set; }

        [JsonProperty("longitude")]
        public double Longitude { get; set; }
    }
}