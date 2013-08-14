using System.Collections.Generic;

namespace Parse.Api
{
    public class UserSession<T> where T : User
    {
        public T User { get; set; }
        public string SessionToken { get; set; }
    }

    public class QueryResult<T>
    {
        public List<T> Results { get; set; }
        public int Count { get; set; }
    }
}