using System.Collections.Generic;
using Newtonsoft.Json;

namespace Parse.Api
{
    // TODO GeoQueries
    /// <summary>
    /// Constraints are used for constructing more precise queries. The property names follow those listed on the 
    /// Parse REST API Constraints page, available at https://parse.com/docs/rest#queries-constraints. Usage can
    /// be found in the main readme.md file of this repository.
    /// </summary>
    public class Constraint
    {
        /// <summary>
        /// Constructor containing all possible permutations of potential query arguments. Arguments can be set as needed. 
        /// Arguments can also be set by using their property accessors if need be with no constructor arguments.
        /// </summary>
        /// <param name="lessThan">Used to find Parse objects that are less than the provided argument.</param>
        /// <param name="lessThanOrEqualTo">Used to find Parse objects that are less than or equal to the provided argument.</param>
        /// <param name="greaterThan">Used to find Parse objects that are greater than the provided argument.</param>
        /// <param name="greaterThanOrEqualTo">Used to find Parse objects that are greater than or equal to the provided argument.</param>
        /// <param name="notEqualTo">Used to find Parse objects that are not equal to the provided argument.</param>
        /// <param name="in">Used to find Parse objects that contain a value in the provided list of arguments.</param>
        /// <param name="notIn">Used to find Parse objects that do not contains values in the provided list of arguments.</param>
        /// <param name="all">Used to find Parse objects with an array field containing each of the values in the provided list of arguments.</param>
        /// <param name="exists">Used to find Parse objects that have or do not have values for the specified property.</param>
        /// <param name="select">Used to find Parse objects that are not related to other objects.</param>
        /// <param name="dontSelect">Used to find Parse objects that are related to other objects.</param>
        /// <param name="regex">Used to find Parse objects that match the provided Perl-based regex string.</param>
        /// <param name="regexOptions">Options used to control how the regex property matches values.</param>
        public Constraint(object lessThan = null,
            object lessThanOrEqualTo = null,
            object greaterThan = null,
            object greaterThanOrEqualTo = null,
            object notEqualTo = null,
            List<object> @in = null,
            List<object> notIn = null,
            List<object> all = null, 
            bool? exists = null,
            object select = null,
            object dontSelect = null,
            string regex = null,
            string regexOptions = null)
        {

            LessThan = lessThan;
            LessThanOrEqualTo = lessThanOrEqualTo;
            GreaterThan = greaterThan;
            GreaterThanOrEqualTo = greaterThanOrEqualTo;
            NotEqualTo = notEqualTo;
            In = @in;
            NotIn = notIn;
            All = all;
            Exists = exists;
            Select = select;
            DontSelect = dontSelect;
            Regex = regex;
            RegexOptions = regexOptions;
        }

        /// <summary>
        /// Used to find Parse objects that are less than the provided argument.
        /// </summary>
        [JsonProperty(PropertyName = "$lt", NullValueHandling = NullValueHandling.Ignore)]
        public object LessThan { get; set; }

        /// <summary>
        /// Used to find Parse objects that are less than or equal to the provided argument.
        /// </summary>
        [JsonProperty(PropertyName = "$lte", NullValueHandling = NullValueHandling.Ignore)]
        public object LessThanOrEqualTo { get; set; }

        /// <summary>
        /// Used to find Parse objects that are greater than the provided argument.
        /// </summary>
        [JsonProperty(PropertyName = "$gt", NullValueHandling = NullValueHandling.Ignore)]
        public object GreaterThan { get; set; }
        
        /// <summary>
        /// Used to find Parse objects that are greater than or equal to the provided argument.
        /// </summary>
        [JsonProperty(PropertyName = "$gte", NullValueHandling = NullValueHandling.Ignore)]
        public object GreaterThanOrEqualTo { get; set; }

        /// <summary>
        /// Used to find Parse objects that are not equal to the provided argument.
        /// </summary>
        [JsonProperty(PropertyName = "$ne", NullValueHandling = NullValueHandling.Ignore)]
        public object NotEqualTo { get; set; }

        /// <summary>
        /// Used to find Parse objects that contain a value in the provided list of arguments.
        /// </summary>
        [JsonProperty(PropertyName = "$in", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> In { get; set; }
            
        /// <summary>
        /// Used to find Parse objects that do not contains values in the provided list of arguments.
        /// </summary>
        [JsonProperty(PropertyName = "$nin", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> NotIn { get; set; }

        /// <summary>
        /// Used to find Parse objects with an array field containing each of the values in the provided list of arguments.
        /// </summary>
        [JsonProperty(PropertyName = "$all", NullValueHandling = NullValueHandling.Ignore)]
        public List<object> All { get; set; }

        /// <summary>
        /// Used to find Parse objects that have or do not have values for the specified property.
        /// </summary>
        [JsonProperty(PropertyName = "$exists", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Exists { get; set; }
        
        /// <summary>
        /// Used to find Parse objects that are related to other objects.
        /// </summary>
        [JsonProperty(PropertyName = "$select", NullValueHandling = NullValueHandling.Ignore)]
        public object Select { get; set; }

        /// <summary>
        /// Used to find Parse objects that are related to other objects.
        /// </summary>
        [JsonProperty(PropertyName = "$dontSelect", NullValueHandling = NullValueHandling.Ignore)]
        public object DontSelect { get; set; }

        /// <summary>
        /// Used to find Parse objects whose string value matches the provided Perl-based regex string.
        /// </summary>
        [JsonProperty(PropertyName = "$regex", NullValueHandling = NullValueHandling.Ignore)]
        public string Regex { get; set; }

        /// <summary>
        /// Options used to control how the regex property matches values. 
        /// Possible values for this include 'i' for a case-insensitive 
        /// search and 'm' to search through multiple lines of text. To 
        /// use both options, simply concatenate them as 'im'.
        /// </summary>
        [JsonProperty(PropertyName = "$options", NullValueHandling = NullValueHandling.Ignore)]
        public string RegexOptions { get; set; }
    }
}
