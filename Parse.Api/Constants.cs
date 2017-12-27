namespace Parse.Api
{
    internal static class ParseUrls
    {
        // POST to create, GET to query
        public const string CLASS = "classes/{0}";

        // PUT to update, GET to retreive, DELETE to delete
        public const string CLASS_OBJECT = "classes/{0}/{1}";

        // POST to sign up, GET to query
        public const string USER = "users";

        // PUT to update, GET to retreive, DELETE to delete
        public const string USER_OBJECT = "users/{0}";

        // GET to log in
        public const string LOGIN = "login";

        // POST to request password reset
        public const string PASSWORD_RESET = "requestPasswordReset";  // TODO

        // POST to create, GET to query
        public const string ROLE = "roles";// TODO

        // PUT to update, GET to retreive, DELETE to delete
        public const string ROLE_OBJECT = "roles/{0}";// TODO

        // POST to upload
        public const string FILE = "files/{FileName}";// TODO

        // POST to track analytics
        public const string APP_OPENED = "events/AppOpened";

        // POST to send push
        public const string PUSH = "push";// TODO

        // POST to upload, GET to query
        public const string INSTALLATION = "installations";// TODO

        // PUT to update, GET to retreive, DELETE to delete
        public const string INSTALLATION_OBJECT = "installations/{0}";// TODO

        // POST to call cloud function
        public const string FUNCTION = "functions/{0}";
    }

    internal static class ParseHeaders
    {
        public const string APP_ID = "X-Parse-Application-Id";
        public const string REST_API_KEY = "X-Parse-REST-API-Key";
        public const string SESSION_TOKEN = "X-Parse-Session-Token";
    }
}