using System;

namespace Parse.Api
{
    public class ParseDate
    {
        public ParseDate(DateTime utcDate)
        {
            iso = utcDate.ToString(DATE_FMT);
        }

        public const string DATE_FMT = "yyyy-MM-ddTHH:mm:ss.fffZ";

        public readonly string __type = "Date";
        public string iso { get; set; }
    }

    public class ParseGeoPoint
    {
        public ParseGeoPoint(double latitude, double longitude)
        {
            this.latitude = latitude;
            this.longitude = longitude;
        }

        public readonly string __type = "GeoPoint";
        public double latitude { get; set; }
        public double longitude { get; set; }
    }

    public class ParseBytes
    {
        public ParseBytes(byte[] bytes)
        {
            base64 = bytes == null ? null : Convert.ToBase64String(bytes);
        }

        public readonly string __type = "Bytes";
        public string base64 { get; set; }
    }

    public class ParsePointer
    {
        public ParsePointer(IParseObject obj)
        {
            if (obj != null)
            {
                objectId = obj.ObjectId;
                className = ParseObject.GetClassName(obj.GetType());
            }
        }

        public readonly string __type = "Pointer";
        public string className { get; set; }
        public string objectId { get; set; }
    }

    public class ParseRelation
    {
        public ParseRelation(IParseObject obj)
        {
            if (obj != null)
            {
                className = ParseObject.GetClassName(obj.GetType());
            }
        }

        public readonly string __type = "Relation";
        public string className { get; set; }
    }

    public abstract class UserBase : ParseObject
    {
        public string username { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public bool? emailVerified { get; set; }
        public AuthData authData { get; set; }
    }

    public class AuthData
    {
        public FacebookAuthData facebook { get; set; }
        public TwitterAuthData twitter { get; set; }
        public AnonAuthData anonymous { get; set; }
    }

    public class FacebookAuthData
    {
        public string id { get; set; }
        public string accessToken { get; set; }
        public DateTime expirationDate { get; set; }
    }

    public class TwitterAuthData
    {
        public string id { get; set; }
        public string screenName { get; set; }
        public string consumerKey { get; set; }
        public string consumerSecret { get; set; }
        public string authToken { get; set; }
        public string authTokenSecret { get; set; }
    }

    public class AnonAuthData
    {
        public string id { get; set; }
    }
}