using System;

namespace Parse.Api.Attributes
{
    /// <summary>
    /// Marks a property that should be ignored when JSON serializing.
    /// JSON.NET's JsonIgnoreAttribute ignores properties for both serialization and deserialization.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class JsonIgnoreForSerializationAttribute : Attribute
    {}
}
