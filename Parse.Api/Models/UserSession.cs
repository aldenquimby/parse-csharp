namespace Parse.Api.Models
{
    /// <summary>
    /// Users receive a session token after signing up or logging in.
    /// The session token is required to update user information.
    /// </summary>
    public class UserSession<T> where T : ParseUser
    {
        public T User { get; set; }
        public string SessionToken { get; set; }
    }
}