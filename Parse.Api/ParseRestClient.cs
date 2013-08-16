using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Parse.Api.Attributes;
using Parse.Api.Converters;
using Parse.Api.Models;
using Parse.Api.Models.Internal;

namespace Parse.Api
{
    /// <summary>
    /// Wrapper for the Parse REST API, found here: https://parse.com/docs/rest
    /// All methods throw an exception if Parse request fails, with message that includes Parse error.
    /// Example exception: "Parse API failed with status code 400 (Bad Request): {code:105,error:"invalid field name: b!ng"}
    /// </summary>
    public class ParseRestClient : IParseRestClient
    {
        private readonly HttpClient _client;

        public ParseRestClient(string appId, string restApiKey)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(restApiKey))
            {
                throw new ArgumentNullException();
            }

            var handler = new HttpClientHandler();
            _client = new HttpClient(handler)
            {
                BaseAddress = new Uri(ParseUrls.BASE),
            };

            _client.DefaultRequestHeaders.Add(ParseHeaders.APP_ID, appId);
            _client.DefaultRequestHeaders.Add(ParseHeaders.REST_API_KEY, restApiKey);
        }

        #region objects

        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="obj">The object to be created on the server</param>
        /// <returns>A fully populated ParseObject, including ObjectId</returns>
        public T CreateObject<T>(T obj) where T : ParseObject, new()
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var resource = string.Format(ParseUrls.CLASS, typeof (T).Name);
            var request = new HttpRequestMessage(HttpMethod.Post, resource);
            request.AddParseBody(obj);

            var response = ExecuteAndValidate<ParseObject>(request, HttpStatusCode.Created);
            obj.CreatedAt = response.CreatedAt;
            obj.UpdatedAt = response.CreatedAt; // UpdatedAt doesn't come back for create requests
            obj.ObjectId = response.ObjectId;
            return obj;
        }

        /// <summary>
        /// Updates a pre-existing ParseObject
        /// </summary>
        /// <param name="obj">The object being updated</param>
        public T Update<T>(T obj) where T : ParseObject, new()
        {
            if (obj == null || string.IsNullOrEmpty(obj.ObjectId))
            {
                throw new ArgumentException("ObjectId is required.");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, obj.ObjectId);
            var request = new HttpRequestMessage(HttpMethod.Put, resource);
            request.AddParseBody(obj);

            var response = ExecuteAndValidate<ParseObject>(request);
            obj.UpdatedAt = response.UpdatedAt; // only UpdatedAt comes back
            return obj;
        }

        /// <summary>
        /// Get one object identified by its ID from Parse
        /// </summary>
        /// <param name="objectId">The ObjectId of the object</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <returns>A dictionary with the object's attributes</returns>
        public T GetObject<T>(string objectId, bool includeReferences = false) where T : ParseObject, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var type = typeof (T);

            if (typeof (ParseUser).IsAssignableFrom(type))
            {
                throw new ArgumentException("Use GetUser() instead of GetObject() to query users.");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, type.Name, objectId);

            return GetObjectInternal<T>(resource, includeReferences, null);
        }

        private T GetObjectInternal<T>(string resource, bool includeReferences, string sessionToken) where T : ParseObject, new()
        {
            if (includeReferences)
            {
                var pointers = typeof(T).GetProperties()
                                         .Where(x => typeof(ParseObject).IsAssignableFrom(x.PropertyType))
                                         .Select(x => x.Name).ToArray();
                if (pointers.Length > 0)
                {
                    resource += "?include=" + string.Join(",", pointers);
                }
            }

            var request = new HttpRequestMessage(HttpMethod.Get, resource);

            if (sessionToken != null)
            {
                request.Headers.Add(ParseHeaders.SESSION_TOKEN, sessionToken);
            }

            return ExecuteAndValidate<T>(request);
        }

        /// <summary>
        /// Search for objects on Parse based on attributes
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order results. Prefacing with '-' will reverse results. Comma separate for multiple orderings.</param>
        /// <param name="limit">The maximum number of results to be returned</param>
        /// <param name="skip">The number of results to skip at the start</param>
        /// <returns>A list of result object, and the total count of results in case the results were limited</returns>
        public QueryResult<T> GetObjects<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseObject, new()
        {
            var type = typeof (T);

            if (typeof (ParseUser).IsAssignableFrom(type))
            {
                throw new ArgumentException("Use GetUsers() instead of GetObjects() to query users.");
            }

            var resource = string.Format(ParseUrls.CLASS, type.Name);

            return GetObjectsInternal<T>(resource, where, order, limit, skip);
        }

        private QueryResult<T> GetObjectsInternal<T>(string resource, object where, string order, int limit, int skip) where T : ParseObject, new()
        {
            resource += "?limit=" + limit + "&skip=" + skip + "&count=1";
            if (where != null)
            {
                // use Newtonsoft so Criteria are serialized correctly
                resource += "&where=" + Uri.EscapeUriString(JsonConvert.SerializeObject(where));
            }
            if (order != null)
            {
                resource += "&order=" + order;
            }

            var request = new HttpRequestMessage(HttpMethod.Get, resource);
            return ExecuteAndValidate<QueryResult<T>>(request);
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="obj">The object to be deleted</param>
        public void DeleteObject<T>(T obj) where T : ParseObject, new()
        {
            if (obj == null || string.IsNullOrEmpty(obj.ObjectId))
            {
                throw new ArgumentException("ObjectId is required.");
            }

            var type = typeof(T);

            if (typeof(ParseUser).IsAssignableFrom(type))
            {
                throw new ArgumentException("Use DeleteUser() instead of DeleteObject() to delete users.");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, type.Name, obj.ObjectId);
            var request = new HttpRequestMessage(HttpMethod.Delete, resource);
            ExecuteAndValidate(request);
        }

        #endregion

        #region relations

        /// <summary>
        /// Adds to an existing relation, or creates one if it doesn't exist
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to add to the relation</param>
        public void AddToRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new()
        {
            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, fromObj.ObjectId);
            var request = new HttpRequestMessage(HttpMethod.Put, resource);
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "AddRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            ExecuteAndValidate(request);
        }

        /// <summary>
        /// Removes from an existing relation
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to remove from the relation</param>
        public void RemoveFromRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new()
        {
            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof(T).Name, fromObj.ObjectId);
            var request = new HttpRequestMessage(HttpMethod.Put, resource);
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "RemoveRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            ExecuteAndValidate(request);
        }

        #endregion

        #region users

        /// <summary>
        /// Creates a new ParseUser and session
        /// </summary>
        /// <param name="user">The user to create, requires username and password</param>
        /// <returns>Fully populated created user and a session token</returns>
        public UserSession<T> SignUp<T>(T user) where T : ParseUser
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("username and password are required.");
            }

            var request = new HttpRequestMessage(HttpMethod.Post, ParseUrls.USER);
            request.AddParseBody(user);

            var response = ExecuteAndValidate<UserSessionResponse>(request, HttpStatusCode.Created);
            user.CreatedAt = response.CreatedAt;
            user.UpdatedAt = response.CreatedAt; // UpdatedAt doesn't come back on create requests
            user.ObjectId = response.ObjectId;
            return new UserSession<T>
            {
                User = user,
                SessionToken = response.SessionToken,
            };
        }

        /// <summary>
        /// Log in as a ParseUser to get a session
        /// </summary>
        /// <param name="user">The user to log in, requires username and password</param>
        /// <returns>Fully populated logged in user and a session token</returns>
        public UserSession<T> LogIn<T>(T user) where T : ParseUser, new()
        {
            if (user == null || string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(user.Password))
            {
                throw new ArgumentException("username and password are required.");
            }

            var resource = ParseUrls.LOGIN + "?username=" + user.Username + "&password=" + user.Password;
            var request = new HttpRequestMessage(HttpMethod.Get, resource);

            var response = ExecuteAndValidate(request);

            // TODO this is ugly
            var session = JsonConvert.DeserializeObject<UserSessionResponse>(response);
            var deserUser = JsonConvert.DeserializeObject<T>(response);
            
            return new UserSession<T>
            {
                User = deserUser,
                SessionToken = session.SessionToken,
            };
        }

        /// <summary>
        /// Get one user identified by it's Parse ID
        /// </summary>
        /// <param name="objectId">The ObjectId of the user</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <param name="sessionToken">more data comes back if the user is authenticated</param>
        /// <returns></returns>
        public T GetUser<T>(string objectId, string sessionToken = null, bool includeReferences = false) where T : ParseUser, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, objectId);

            return GetObjectInternal<T>(resource, includeReferences, sessionToken);
        }

        /// <summary>
        /// Search for users on Parse based on attributes
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order results. Prefacing with '-' will reverse results. Comma separate for multiple orderings.</param>
        /// <param name="limit">The maximum number of results to be returned</param>
        /// <param name="skip">The number of results to skip at the start</param>
        /// <returns>A list of result users, and the total count of results in case the results were limited</returns>
        public QueryResult<T> GetUsers<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseUser, new()
        {
            return GetObjectsInternal<T>(ParseUrls.USER, where, order, limit, skip);
        }

        /// <summary>
        /// Updates a pre-existing ParseUser
        /// </summary>
        /// <param name="user">The user to update</param>
        /// <param name="sessionToken">Session token given by SignUp or LogIn</param>
        public T UpdateUser<T>(T user, string sessionToken) where T : ParseUser, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, user.ObjectId);
            var request = new HttpRequestMessage(HttpMethod.Put, resource);
            request.AddParseBody(user);
            request.Headers.Add(ParseHeaders.SESSION_TOKEN, sessionToken);

            var response = ExecuteAndValidate<ParseObject>(request);
            user.UpdatedAt = response.UpdatedAt; // only UpdatedAt comes back
            return user;
        }

        /// <summary>
        /// Updates a pre-existing ParseUser
        /// </summary>
        /// <param name="user">The user to delete</param>
        /// <param name="sessionToken">Session token given by SignUp or LogIn</param>
        public void DeleteUser<T>(T user, string sessionToken) where T : ParseUser, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, user.ObjectId);
            var request = new HttpRequestMessage(HttpMethod.Delete, resource);
            request.Headers.Add(ParseHeaders.SESSION_TOKEN, sessionToken);
            
            ExecuteAndValidate(request);
        }

        #endregion

        #region cloud functions

        /// <summary>
        /// Executes a pre-existing cloud function, see here for details: https://www.parse.com/docs/cloud_code_guide
        /// </summary>
        /// <param name="name">The name of the cloud code function</param>
        /// <param name="data">Data to pass to the cloud code function</param>
        /// <returns>The result of the cloud code function</returns>
        public string CloudFunction(string name, object data = null)
        {
            var resource = string.Format(ParseUrls.FUNCTION, name);
            
            var request = new HttpRequestMessage(HttpMethod.Post, resource);
            request.AddBody(data ?? new {}); // need a blank body or the API borks
            
            var response = ExecuteAndValidate<CloudFunctionResponse>(request);
            return response.Result;
        }

        #endregion

        #region analytics

        /// <summary>
        /// Records an AppOpened event for Parse analytics
        /// </summary>
        /// <param name="dateUtc">The date the app was opened, or now if not specified</param>
        public void MarkAppOpened(DateTime? dateUtc = null)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, ParseUrls.APP_OPENED);
            if (dateUtc.HasValue)
            {
                request.AddBody(new {at = new ParseDate(dateUtc.Value)});
            }
            else
            {
                request.AddBody(new {}); // need a blank body or the API borks
            }
            ExecuteAndValidate(request);
        }

        #endregion

        #region helpers

        private string ExecuteAndValidate(HttpRequestMessage request, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            var response = _client.SendAsync(request).Result;
            var content = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode != expectedCode)
            {
                // when a request fails, body is JSON: {code:105,error:"invalid field name: b!ng"}
                throw new Exception(string.Format("Parse API failed with status code {0} ({1}): {2}", (int)response.StatusCode, response.StatusCode, content));
            }

            return content;
        }

        private T ExecuteAndValidate<T>(HttpRequestMessage request, HttpStatusCode expectedCode = HttpStatusCode.OK) where T : new()
        {
            var responseContent = ExecuteAndValidate(request, expectedCode);
            return JsonConvert.DeserializeObject<T>(responseContent, new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> {new ParseBytesConverter(), new ParseDateConverter()},
            });
        }

        private class UserSessionResponse : ParseObject
        {
            public string SessionToken { get; set; }
        }

        private class CloudFunctionResponse
        {
            public string Result { get; set; }
        }

        #endregion
    }

    internal static class HttpWebRequestExtensions
    {
        public static void AddParseBody(this HttpRequestMessage request, ParseObject body)
        {
            // var propsToIgnore = new List<string> { "CreatedAt", "UpdatedAt", "ObjectId", "AuthData", "EmailVerified" };

            var dict = new Dictionary<string, object>();

            foreach (var prop in body.GetType().GetProperties()) //.Where(x => !propsToIgnore.Contains(x.Name)))
            {
                var value = prop.GetValue(body, null);

                if (prop.PropertyType == typeof(DateTime))
                {
                    value = new ParseDate((DateTime)value);
                }
                else if (prop.PropertyType == typeof(byte[]))
                {
                    value = new ParseBytes((byte[])value);
                }
                else if (typeof(ParseObject).IsAssignableFrom(prop.PropertyType))
                {
                    if (value != null)
                    {
                        value = new ParsePointer((ParseObject)value);
                    }
                }
                else if (prop.PropertyType.IsGenericType && value is IList && typeof(ParseObject).IsAssignableFrom(prop.PropertyType.GetGenericArguments()[0]))
                {
                    // explicity skip relations, need to be dealt with manually
                    continue;

                    // var pointers = ((IList) value).Cast<ParseObject>().Select(x => new ParsePointer(x)).ToList();
                    // value = pointers.Count == 0 ? null : new {__op = "AddRelation", objects = pointers};
                }

                var attrs = prop.GetCustomAttributes(true);
                JsonIgnoreForSerializationAttribute jsonIgnore = null;
                JsonPropertyAttribute jsonProp = null;
                foreach (var attr in attrs)
                {
                    var tmp1 = attr as JsonPropertyAttribute;
                    if (tmp1 != null)
                    {
                        jsonProp = tmp1;
                    }
                    var tmp2 = attr as JsonIgnoreForSerializationAttribute;
                    if (tmp2 != null)
                    {
                        jsonIgnore = tmp2;
                    }
                }
                if (jsonIgnore != null)
                {
                    continue;
                }

                if (jsonProp != null && !string.IsNullOrEmpty(jsonProp.PropertyName))
                {
                    dict[jsonProp.PropertyName] = value;
                }
                else
                {
                    dict[prop.Name] = value;
                }
            }

            request.AddBody(dict);
        }

        public static void AddBody(this HttpRequestMessage request, object body)
        {
            var serializedBody = JsonConvert.SerializeObject(body);
            request.Content = new StringContent(serializedBody, null, "application/json");
        }
    }

    internal static class ParseHeaders
    {
        public const string APP_ID = "X-Parse-Application-Id";
        public const string REST_API_KEY = "X-Parse-REST-API-Key";
        public const string SESSION_TOKEN = "X-Parse-Session-Token";
    }

    internal static class ParseUrls
    {
        public const string BASE = "https://api.parse.com/1/";

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
}
