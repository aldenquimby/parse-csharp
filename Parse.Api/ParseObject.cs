using System;

namespace Parse.Api
{
    public interface IParseObject
    {
        DateTime CreatedAt { get; set; }
        DateTime UpdatedAt { get; set; }
        string ObjectId { get; set; }
    }

    public class ParseObject : IParseObject
    {
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string ObjectId { get; set; }

        public static string GetClassName(Type type)
        {
            if (type == typeof (UserBase))
            {
                return "_User";
            }

            return type.Name;
        }
    }
}