using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace Parse.Api
{
    public interface IParseRestClient
    {
        /// <summary>
        /// Creates a new ParseObject
        /// </summary>
        /// <param name="obj">The object to be created on the server</param>
        /// <returns>A fully populated ParseObject, including ObjectId</returns>
        T CreateObject<T>(T obj) where T : ParseObject, new();

        /// <summary>
        /// Updates a pre-existing ParseObject
        /// </summary>
        /// <param name="obj">The object being updated</param>
        T Update<T>(T obj) where T : ParseObject, new();

        /// <summary>
        /// Get one object identified by its ID from Parse
        /// </summary>
        /// <param name="objectId">The ObjectId of the object</param>
        /// <param name="includeReferences">Whether or not to fetch objects pointed to</param>
        /// <returns>A dictionary with the object's attributes</returns>
        T GetObject<T>(string objectId, bool includeReferences = false) where T : ParseObject, new();

        /// <summary>
        /// Search for objects on Parse based on attributes. 
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order the results. Prefacing with '-' will reverse the results.</param>
        /// <param name="limit">The maximum number of results to be returned (Default 100)</param>
        /// <param name="skip">The number of results to skip at the start (Default 0)</param>
        /// <returns>An array of Dictionaries containing the objects</returns>
        QueryResult<T> GetObjects<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseObject, new();

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="obj">The object to be deleted</param>
        void DeleteObject<T>(T obj) where T : ParseObject, new();

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="objectId">The object id to be deleted</param>
        void DeleteObject<T>(string objectId) where T : ParseObject, new();

        /// <summary>
        /// Adds to an existing relation, or creates one if it doesn't exist.
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to add to the relation</param>
        void AddToRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new();

        /// <summary>
        /// Removes from an existing relation.
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to remove from the relation</param>
        void RemoveFromRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new();

        UserSession<T> SignUp<T>(T user) where T : UserBase;
        UserSession<T> LogIn<T>(T user) where T : UserBase, new();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectId"></param>
        /// <param name="sesionToken">more data comes back if the user is authenticated</param>
        /// <returns></returns>
        T GetUser<T>(string objectId, string sesionToken = null) where T : UserBase, new();

        T UpdateUser<T>(T user, string sessionToken) where T : UserBase, new();
        void DeleteUser<T>(T user, string sessionToken) where T : UserBase, new();
        void CloudFunction(string name, object data = null);
        void MarkAppOpened(DateTime? dateUtc = null);
    }

    public class ParseRestClient : IParseRestClient
    {
        private readonly HttpClientHandler _handler;
        private readonly HttpClient _client;

        private readonly string _appId;
        private readonly string _restApiKey;

        public ParseRestClient(string appId, string restApiKey)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(restApiKey))
            {
                throw new ArgumentNullException();
            }

            _appId = appId;
            _restApiKey = restApiKey;
            _handler = new HttpClientHandler();
            _client = new HttpClient(_handler)
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
            var request = CreateRequestMessage(resource, "POST");
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
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, obj.ObjectId);
            var request = CreateRequestMessage(resource, "PUT");
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

            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, objectId);

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

            var request = CreateRequest(resource, "GET");
            return ExecuteAndValidate<T>(request);
        }

        /// <summary>
        /// Search for objects on Parse based on attributes. 
        /// </summary>
        /// <param name="where">See https://www.parse.com/docs/rest#data-querying for more details</param>
        /// <param name="order">The name of the attribute used to order the results. Prefacing with '-' will reverse the results.</param>
        /// <param name="limit">The maximum number of results to be returned (Default 100)</param>
        /// <param name="skip">The number of results to skip at the start (Default 0)</param>
        /// <returns>An array of Dictionaries containing the objects</returns>
        public QueryResult<T> GetObjects<T>(object where = null, string order = null, int limit = 100, int skip = 0) where T : ParseObject, new()
        {
            var resource = string.Format(ParseUrls.CLASS, typeof (T).Name);
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

            var request = CreateRequest(resource, "GET");
            return ExecuteAndValidate<QueryResult<T>>(request);
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="obj">The object to be deleted</param>
        public void DeleteObject<T>(T obj) where T : ParseObject, new()
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            DeleteObject<T>(obj.ObjectId);
        }

        /// <summary>
        /// Deletes an object from Parse
        /// </summary>
        /// <param name="objectId">The object id to be deleted</param>
        public void DeleteObject<T>(string objectId) where T : ParseObject, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, objectId);
            var request = CreateRequest(resource, "DELETE");
            ExecuteAndValidate(request);
        }

        #endregion

        #region relations

        /// <summary>
        /// Adds to an existing relation, or creates one if it doesn't exist.
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to add to the relation</param>
        public void AddToRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new()
        {
            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof (T).Name, fromObj.ObjectId);
            var request = CreateRequest(resource, "PUT");
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "AddRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            ExecuteAndValidate(request);
        }

        /// <summary>
        /// Removes from an existing relation.
        /// </summary>
        /// <param name="fromObj">The object with the relation</param>
        /// <param name="relationName">The name of the relation</param>
        /// <param name="toObjs">The ParseObjects to remove from the relation</param>
        public void RemoveFromRelation<T>(T fromObj, string relationName, IEnumerable<ParseObject> toObjs) where T : ParseObject, new()
        {
            var resource = string.Format(ParseUrls.CLASS_OBJECT, typeof(T).Name, fromObj.ObjectId);
            var request = CreateRequest(resource, "PUT");
            request.AddBody(new Dictionary<string, object>
            {
                {relationName, new {__op = "RemoveRelation", objects = toObjs.Select(x => new ParsePointer(x)).ToList()}}
            });
            ExecuteAndValidate(request);
        }

        #endregion

        #region users

        public UserSession<T> SignUp<T>(T user) where T : UserBase
        {
            if (user == null || string.IsNullOrEmpty(user.username) || string.IsNullOrEmpty(user.password))
            {
                throw new ArgumentException("username and password are required.");
            }

            var request = CreateRequest(ParseUrls.USER, "POST");
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

        public UserSession<T> LogIn<T>(T user) where T : UserBase, new()
        {
            if (user == null || string.IsNullOrEmpty(user.username) || string.IsNullOrEmpty(user.password))
            {
                throw new ArgumentException("username and password are required.");
            }

            var resource = ParseUrls.LOGIN + "?username=" + user.username + "&password=" + user.password;
            var request = CreateRequest(resource, "GET");

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
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectId"></param>
        /// <param name="sesionToken">more data comes back if the user is authenticated</param>
        /// <returns></returns>
        public T GetUser<T>(string objectId, string sesionToken = null) where T : UserBase, new()
        {
            if (string.IsNullOrEmpty(objectId))
            {
                throw new ArgumentNullException("objectId");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, objectId);
            var request = CreateRequest(resource, "GET");
            if (sesionToken != null)
            {
                request.Headers[ParseHeaders.SESSION_TOKEN] = sesionToken;
            }

            return ExecuteAndValidate<T>(request);
        }

        public T UpdateUser<T>(T user, string sessionToken) where T : UserBase, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, user.ObjectId);
            var request = CreateRequest(resource, "PUT");
            request.AddParseBody(user);
            request.Headers[ParseHeaders.SESSION_TOKEN] = sessionToken;

            var response = ExecuteAndValidate<ParseObject>(request);
            user.UpdatedAt = response.UpdatedAt; // only UpdatedAt comes back
            return user;
        }

        // TODO add method
        // public QueryResult<T> GetUsers<T>() where T : UserBase, new()

        public void DeleteUser<T>(T user, string sessionToken) where T : UserBase, new()
        {
            if (user == null || string.IsNullOrEmpty(user.ObjectId) || string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentException("ObjectId and SessionToken are required.");
            }

            var resource = string.Format(ParseUrls.USER_OBJECT, user.ObjectId);
            var request = CreateRequest(resource, "DELETE");
            request.Headers[ParseHeaders.SESSION_TOKEN] = sessionToken;
            
            ExecuteAndValidate(request);
        }

        private class UserSessionResponse : ParseObject
        {
            public string SessionToken { get; set; }
        }

        #endregion

        #region cloud functions

        public void CloudFunction(string name, object data = null)
        {
            var resource = string.Format(ParseUrls.FUNCTION, name);
            var request = CreateRequest(resource, "POST");
            if (data != null)
            {
                request.AddBody(data);
            }
            ExecuteAndValidate<object>(request);
        }

        #endregion

        #region analytics

        public void MarkAppOpened(DateTime? dateUtc = null)
        {
            var request = CreateRequest(ParseUrls.APP_OPENED, "POST");
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

        private HttpRequestMessage CreateRequestMessage(string resource, string method)
        {
            return new HttpRequestMessage(new HttpMethod(method), resource);
        }

        private T ExecuteAndValidate<T>(HttpRequestMessage request, HttpStatusCode expectedCode = HttpStatusCode.OK) where T : new()
        {
            var response = _client.SendAsync(request).Result;
            var content = response.Content.ReadAsStringAsync().Result;

            if (response.StatusCode != expectedCode)
            {
                // when a request fails, body is JSON: {code:105,error:"invalid field name: b!ng"}
                throw new Exception(string.Format("Parse API failed with status code {0} ({1}): {2}", (int)response.StatusCode, response.StatusCode, content));
            }

            return JsonConvert.DeserializeObject<T>(content);
        }

        /// <summary>
        /// Creates RestRequest for Parse REST API resource.
        /// </summary>
        private HttpWebRequest CreateRequest(string resource, string method)
        {
            var request = WebRequest.CreateHttp(ParseUrls.BASE + resource);
            request.Method = method;
            request.ContentType = "application/json";
            request.Headers = new WebHeaderCollection();
            request.Headers[ParseHeaders.APP_ID] = _appId;
            request.Headers[ParseHeaders.REST_API_KEY] = _restApiKey;
            return request;
        }

        private class Response
        {
            public string Content { get; set; }
            public HttpStatusCode StatusCode { get; set; }
            public Exception Exception { get; set; }
        }

        private Response GetResponse(HttpWebRequest request)
        {
            var response = new Response();

            var responseDone = new ManualResetEvent(false);

            try
            {
                request.BeginGetResponse(ar =>
                {
                    var theRequest = (HttpWebRequest)ar.AsyncState;
                    using (var theResponse = (HttpWebResponse) theRequest.EndGetResponse(ar))
                    {
                        response.StatusCode = theResponse.StatusCode;

                        try
                        {
                            using (var responseStream = theResponse.GetResponseStream())
                            using (var sr = new StreamReader(responseStream))
                            {
                                response.Content = sr.ReadToEnd();
                            }
                        }
                        catch (Exception e)
                        {
                            response.Exception = e;
                        }
                    }

                    responseDone.Set();
                }, request);
            }
            catch (Exception e)
            {
                if (response.Exception == null)
                {
                    response.Exception = e;
                }
            }

            responseDone.WaitOne();

            return response;
        }

        /// <summary>
        /// Returns true if response status is valid and there is no exception.
        /// </summary>
        private bool IsValidResponse(Response response, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            return response.Exception == null &&
                   response.StatusCode == expectedCode;
        }

        /// <summary>
        /// Executes request, validates response, returns content.
        /// </summary>
        private string ExecuteAndValidate(HttpWebRequest request, HttpStatusCode expectedCode = HttpStatusCode.OK)
        {
            var response = GetResponse(request);

            // make sure request went through ok
            if (!IsValidResponse(response, expectedCode))
            {
                // when a request fails, body is JSON: {code:105,error:"invalid field name: b!ng"}
                throw new Exception("Parse API failed: " + response.Content, response.Exception);
            }

            return response.Content;
        }

        /// <summary>
        /// Executes request, validates response, returns deserialized data.
        /// </summary>
        private T ExecuteAndValidate<T>(HttpWebRequest request, HttpStatusCode expectedCode = HttpStatusCode.OK) where T : new()
        {
            var response = ExecuteAndValidate(request, expectedCode);
            return JsonConvert.DeserializeObject<T>(response);
        }

        #endregion
    }

    internal static class HttpWebRequestExtensions
    {
        public static void AddParseBody(this HttpWebRequest request, ParseObject body)
        {
            var propsToIgnore = new List<string> {"CreatedAt", "UpdatedAt", "ObjectId", "authData", "emailVerified"};

            var dict = new Dictionary<string, object>();

            foreach (var prop in body.GetType().GetProperties().Where(x => !propsToIgnore.Contains(x.Name)))
            {
                var value = prop.GetValue(body, null);

                if (prop.PropertyType == typeof (DateTime))
                {
                    value = new ParseDate((DateTime)value);
                }
                else if (prop.PropertyType == typeof (byte[]))
                {
                    value = new ParseBytes((byte[]) value);
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

                dict[prop.Name] = value;
            }

            request.AddBody(dict);
        }

        public static void AddBody(this HttpWebRequest request, object body)
        {
            var serializedBody = JsonConvert.SerializeObject(body);
            var postData = Encoding.UTF8.GetBytes(serializedBody);

            // synchronously write request
            var requestDone = new ManualResetEvent(false);
            request.BeginGetRequestStream(ar =>
            {
                var theRequest = (HttpWebRequest)ar.AsyncState;
                using (var requestStream = theRequest.EndGetRequestStream(ar))
                {
                    requestStream.Write(postData, 0, postData.Length);
                }
                requestDone.Set();
            }, request);
            requestDone.WaitOne();
        }

        public static void AddParseBody(this HttpRequestMessage request, ParseObject body)
        {
            var propsToIgnore = new List<string> { "CreatedAt", "UpdatedAt", "ObjectId", "authData", "emailVerified" };

            var dict = new Dictionary<string, object>();

            foreach (var prop in body.GetType().GetProperties().Where(x => !propsToIgnore.Contains(x.Name)))
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

                dict[prop.Name] = value;
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
