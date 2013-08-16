using System.Collections.Generic;
using Newtonsoft.Json;

namespace Parse.Api.Models
{
    /// <summary>
    /// The results of a query, including the total count, which is useful if the results were limited.
    /// </summary>
    public class QueryResult<T>
    {
        public List<T> Results { get; set; }

        [JsonProperty("count")]
        public int TotalCount { get; set; }
    }
}