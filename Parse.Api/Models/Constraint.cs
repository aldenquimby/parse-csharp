using System.Collections.Generic;
using Newtonsoft.Json;

namespace Parse.Api.Models
{
    // TODO GeoQueries
    /// <summary>
    /// Constraints are used for constructing precise queries. For usage, see the README.
    /// </summary>
    /// <seealso cref="http://parse.com/docs/rest#queries-constraints"/>
    public class Constraint
    {
        /// <summary>
        /// Used to find Parse objects that are less than the provided argument.
        /// </summary>
        [JsonProperty("$lt", NullValueHandling = NullValueHandling.Ignore)]
        public object LessThan { get; set; }

        /// <summary>
        /// Used to find Parse objects that are less than or equal to the provided argument.
        /// </summary>
        [JsonProperty("$lte", NullValueHandling = NullValueHandling.Ignore)]
        public object LessThanOrEqualTo { get; set; }

        /// <summary>
        /// Used to find Parse objects that are greater than the provided argument.
        /// </summary>
        [JsonProperty("$gt", NullValueHandling = NullValueHandling.Ignore)]
        public object GreaterThan { get; set; }
        
        /// <summary>
        /// Used to find Parse objects that are greater than or equal to the provided argument.
        /// </summary>
        [JsonProperty("$gte", NullValueHandling = NullValueHandling.Ignore)]
        public object GreaterThanOrEqualTo { get; set; }

        /// <summary>
        /// Used to find Parse objects that are not equal to the provided argument.
        /// </summary>
        [JsonProperty("$ne", NullValueHandling = NullValueHandling.Ignore)]
        public object NotEqualTo { get; set; }

        /// <summary>
        /// Used to find Parse objects that contain a value in the provided list of arguments.
        /// </summary>
        [JsonProperty("$in", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> In { get; set; }
            
        /// <summary>
        /// Used to find Parse objects that do not contains values in the provided list of arguments.
        /// </summary>
        [JsonProperty("$nin", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> NotIn { get; set; }

        /// <summary>
        /// Used to find Parse objects with an array field containing each of the values in the provided list of arguments.
        /// </summary>
        [JsonProperty("$all", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> All { get; set; }

        /// <summary>
        /// Used to find Parse objects that have or do not have values for the specified property.
        /// </summary>
        [JsonProperty("$exists", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Exists { get; set; }
        
        /// <summary>
        /// Used to find Parse objects that are related to other objects.
        /// </summary>
        [JsonProperty("$select", NullValueHandling = NullValueHandling.Ignore)]
        public object Select { get; set; }

        /// <summary>
        /// Used to find Parse objects that are related to other objects.
        /// </summary>
        [JsonProperty("$dontSelect", NullValueHandling = NullValueHandling.Ignore)]
        public object DontSelect { get; set; }

        /// <summary>
        /// Used to find Parse objects whose string value matches the provided Perl-based regex string.
        /// </summary>
        [JsonProperty("$regex", NullValueHandling = NullValueHandling.Ignore)]
        public string Regex { get; set; }

        /// <summary>
        /// Options used to control how the regex property matches values. 
        /// Possible values for this include 'i' for a case-insensitive 
        /// search and 'm' to search through multiple lines of text. To 
        /// use both options, simply concatenate them as 'im'.
        /// </summary>
        [JsonProperty("$options", NullValueHandling = NullValueHandling.Ignore)]
        public string RegexOptions { get; set; }
    }
}
