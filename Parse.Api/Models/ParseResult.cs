using System.Collections.Generic;
using Newtonsoft.Json;

namespace Parse.Api.Models
{
    /// <summary>
    /// Base class for result from Parse API request.
    /// </summary>
    public class ParseResult
    {
        internal string Content { get; set; }
        public ParseException Exception { get; set; }
    }

    /// <summary>
    /// Base class for result from Parse API request.
    /// </summary>
    public class ParseResult<T> : ParseResult
    {
        public T Result { get; set; }
    }

    /// <summary>
    /// The results of a query, including the total count, which is useful if the results were limited.
    /// </summary>
    public class QueryResult<T>
    {
        public ParseException Exception { get; set; }
        public List<T> Results { get; set; }

        [JsonProperty("count")]
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// The results of a cloud function that returns a collection
    /// </summary>
    public class CloudFunctionResult<T>
    {
        public ParseException Exception { get; set; }
        [JsonProperty("result")]
        public List<T> Results { get; set; }
    }

    /// <summary>
    /// Users receive a session token after signing up or logging in.
    /// The session token is required to update user information.
    /// </summary>
    public class UserResult<T> where T : ParseUser
    {
        public ParseException Exception { get; set; }
        public T User { get; set; }
        public string SessionToken { get; set; }
    }
}
