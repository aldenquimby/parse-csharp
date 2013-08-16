using System;
using Newtonsoft.Json;
using Parse.Api.Attributes;

namespace Parse.Api.Models
{
    /// <summary>
    /// Default Parse User, should be inherited for custom User classes (i.e. if "phoneNumber" is added)
    /// </summary>
    public class ParseUser : ParseObject
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonIgnoreForSerialization]   
        public bool? EmailVerified { get; set; }

        [JsonIgnoreForSerialization]   
        public AuthData AuthData { get; set; }
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