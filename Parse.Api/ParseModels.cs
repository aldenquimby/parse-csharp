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

    public class User : ParseObject
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public bool? EmailVerified { get; set; }
        public AuthData AuthData { get; set; }
    }

    public class AuthData
    {
        public FacebookAuthData Facebook { get; set; }
        public TwitterAuthData Twitter { get; set; }
        public AnonAuthData Anonymous { get; set; }
    }

    public class FacebookAuthData
    {
        public string Id { get; set; }
        public string AccessToken { get; set; }
        public DateTime ExpirationDate { get; set; }
    }

    public class TwitterAuthData
    {
        public string Id { get; set; }
        public string ScreenName { get; set; }
        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string AuthToken { get; set; }
        public string AuthTokenSecret { get; set; }
    }

    public class AnonAuthData
    {
        public string Id { get; set; }
    }
}